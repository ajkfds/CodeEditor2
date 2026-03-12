using AjkAvaloniaLibs.Controls;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.FileTypes;
using CodeEditor2.NavigatePanel;
using CodeEditor2.Tools;
using Microsoft.Playwright;
using Svg;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace CodeEditor2.Data
{
    public class TextFile : File, ITextFile
    {
        protected TextFile() : base() { }
        public static async Task<TextFile> CreateAsync(string relativePath, Project project)
        {
            string name;
            if (relativePath.Contains(System.IO.Path.DirectorySeparatorChar))
            {
                name = relativePath.Substring(relativePath.LastIndexOf(System.IO.Path.DirectorySeparatorChar) + 1);
            }
            else
            {
                name = relativePath;
            }
            TextFile fileItem = new TextFile()
            {
                Project = project,
                RelativePath = relativePath,
                Name = name
            };
            await fileItem.FileCheck();
            if (fileItem.document == null) System.Diagnostics.Debugger.Break();
            return fileItem;
        }
        protected CodeEditor.CodeDocument document;
        public virtual void ModifyEditorContextMenu(ContextMenu contextMenu)
        {

        }

        public virtual string Key
        {
            get
            {
                return RelativePath;
            }
        }
        public TextFile ToTextFile()
        {
            return this;
        }
        public override void Dispose()
        {
            if (document != null) document.Dispose();
            //if (ParsedDocument != null) ParsedDocument.Dispose();
            document = null;
            //ParsedDocument = null;
            base.Dispose();
        }

        public double StoredVerticalScrollPosition { get; set; } = 0;

        public virtual bool ReparseRequested { get; set; } = false;

        public bool IsCodeDocumentCashed
        {
            get { if (document == null) return false; else return true; }
        }

        public virtual CodeEditor.ParsedDocument? ParsedDocument { get; set; }

        public void PostParse()
        {
            ParseWorker parseWorker = new ParseWorker();
            Task.Run(async () => { await parseWorker.Parse(this); });
        }
        public virtual Task AcceptParsedDocumentAsync(CodeEditor2.CodeEditor.ParsedDocument newParsedDocument)
        {
            CodeEditor2.CodeEditor.ParsedDocument? oldParsedDocument = ParsedDocument;
            ParsedDocument = null;
            if (oldParsedDocument != null) oldParsedDocument.Dispose();

            ParsedDocument = newParsedDocument;
            PostUIUpdate();
            return Task.CompletedTask;
        }
        public virtual void Close()
        {
            if (Dirty) return;
            if (document == null) return;
            document.Dispose();
        }

        public virtual bool Dirty
        {
            get
            {
                if (document == null) return false;
                return document.IsDirty;
            }
        }
        public virtual Task<CodeEditor.CodeDocument> GetCodeDocumentAsync()
        {
            postFileCheck();
            if (document == null) throw new Exception();
            return Task.FromResult(document);
        }
        public virtual CodeDocument CodeDocument
        {
            get
            {
                return document;
            }
        }

        public override void Remove()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Remove();
                return;
            }

            if(Controller.NavigatePanel.GetSelectedFile() == this)
            {
                Controller.NavigatePanel.SelectNode(Project.NavigatePanelNode);
            }

            if (Parent != null)
            {
                Parent.Items.TryRemove(Name);
                Parent.NavigatePanelNode.UpdateVisual();
            }
        }

        public async virtual Task SaveAsync()
        {
            await _fileSemaphore.WaitAsync();
            try
            {
                WeakReference<ListViewItem> itemRef = await Controller.AppendLogAndGetItem("Save " + RelativePath + "...", Avalonia.Media.Colors.Green);

                if (document == null) return;

                string filePath = AbsolutePath;
                string saveText = document.CreateString();
                ulong savedVersion = document.Version;

                string? newHash = null;
                await Task.Run(
                    async () => {
                        newHash = await SaveTextAndGetHash(saveText);
                    }
                );

                if (newHash == null)
                {
                    Controller.AppendLog("filed to save " + AbsolutePath, Avalonia.Media.Colors.Red);
                    return;
                }
                if (savedVersion == document.Version) document.Clean();
                loadFileHash = newHash;
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }

        public async Task<string?> SaveTextAndGetHash(string text)
        {
            try
            {
                await DataAccess.SaveFileAsync(Project, RelativePath, text);
                return GetHash(text);
            }
            catch (IOException)
            {
                return null;
            }
        }

        protected string loadFileHash = "";

        protected virtual void CreateCodeDocument()
        {
            document = new CodeEditor.CodeDocument(this);
        }


        //非同期待機
        private readonly SemaphoreSlim _fileSemaphore = new SemaphoreSlim(1, 1);
        protected virtual async Task FileCheck()
        {
            // 待ち時間 0 でトライ。入れなければ false が返る
            if (!await _fileSemaphore.WaitAsync(0))
            {
                // すでに実行中のため、何もせずリターン
                return;
            }

            try
            {
                await DataAccess.UpdateFieSystemInfoAsync(Project, RelativePath);
                if (IsDeleted)
                {
                    Remove();
                    return;
                }

                try
                {
                    bool initialLoad = false;
                    bool dirty = false;
                    if (document == null)
                    {
                        initialLoad = true;
                        CreateCodeDocument();
                        if (document == null) throw new Exception();
                        string? cashedText = await DataAccess.TryGetChasheAsync(Project, RelativePath);
                        if(cashedText != null)
                        {
                            PostStatusCheck();
                        }
                    }
                    else
                    {
                        dirty = document.IsDirty;
                    }


                    string text = await DataAccess.GetFileTextAsync(Project, RelativePath);
                    string newHash = newHash = GetHash(text);
                    if (newHash == loadFileHash)
                    {
                        return;
                    }

                    if (dirty & !initialLoad)
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            Tools.YesNoWindow checkUpdate = new Tools.YesNoWindow("Update Check", RelativePath + " changed externally. Can I dispose local change ?");
                            await checkUpdate.ShowDialog(Controller.GetMainWindow());
                            if (!checkUpdate.Yes) // keep current file
                            {
                                loadFileHash = newHash;
                                return;
                            }
                        });
                    }

                    if (Dispatcher.UIThread.CheckAccess() | initialLoad)
                    {
                        document.TextDocument.Replace(0, document.TextDocument.TextLength, text);
                        document.Clean();
                        loadFileHash = newHash;
                        if (initialLoad) document.ClearHistory();
                        await FileChangedAsync();
                    }
                    else
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            document.TextDocument.Replace(0, document.TextDocument.TextLength, text);
                            document.Clean();
                            loadFileHash = newHash;
                            if (initialLoad) document.ClearHistory();
                            await FileChangedAsync();
                        });
                    }
                }
                catch (FileNotFoundException)
                {
                    IsDeleted = true;
                    Remove();
                }
                catch (Exception ex)
                {
                    if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                }
            }
            finally
            {
                _fileSemaphore.Release();
            }

        }

        public override Task FileChangedAsync()
        {
            if (Controller.NavigatePanel.GetSelectedFile() == this && Dispatcher.UIThread.CheckAccess()) Controller.CodeEditor.PostRefresh();
            if (NavigatePanelNode != null) NavigatePanelNode.PostUpdate();
            return Task.CompletedTask;
        }

        public override void PostUIUpdate()
        {
            Dispatcher.UIThread.Post(
                async() =>
                {
                    if (await Controller.CodeEditor.GetTextFileAsync() == this)
                    {
                        Controller.CodeEditor.PostRefresh();
                        if (ParsedDocument != null) Controller.MessageView.Update(ParsedDocument);
                    }
                    if (NavigatePanelNode != null) NavigatePanelNode.UpdateVisual();
                });
        }



        public string GetHash(string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            byte[] hashBytes = XxHash64.Hash(data);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        public virtual CodeDrawStyle DrawStyle
        {
            get
            {
                return Global.DefaultDrawStyle;
            }
        }



        protected override NavigatePanelNode CreateNode()
        {
            return new TextFileNode(this);
        }

        public override async Task UpdateAsync()
        {
            await base.UpdateAsync();
            postFileCheck();
        }


        public override void PostStatusCheck()
        {
            postFileCheck();
        }
        protected void postFileCheck()
        {
            _ = Task.Run(async () => { await FileCheck(); });
        }

        public override DocumentParser? CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            return null;
        }

        public virtual PopupItem? GetPopupItem(ulong Version, int index)
        {
            return null;
        }

        public virtual List<AutocompleteItem>? GetAutoCompleteItems(int index, out string? candidateWord)
        {
            candidateWord = "";
            return null;
        }
        public virtual List<ToolItem>? GetToolItems(int index)
        {

            if (CustomizeTooltem == null)
            {
                return null;
            }
            else
            {
                List<ToolItem> toolItems = new List<ToolItem>();
                CustomizeTooltem?.Invoke(toolItems);
                return toolItems;
            }
        }

        public static Action<List<ToolItem>>? CustomizeTooltem;

        public virtual void TextEntered(TextInputEventArgs e)
        {

        }

        public virtual void TextEntering(TextInputEventArgs e)
        {

        }

        // parse this text file hierarchy
        public virtual async Task ParseHierarchyAsync(Action<ITextFile> action)
        {
            List<string> parsedIds = new List<string>();

            await parseHierarchyAsync(this, parsedIds, action);
            await UpdateAsync();

            if (NavigatePanelNode != null)
            {
                await NavigatePanelNode.HierarchicalVisibleUpdateAsync();
            }
        }

        private async Task parseHierarchyAsync(Data.Item item, List<string> parsedIds, Action<ITextFile> action)
        {
            if (item == null) return;
            Data.ITextFile? textFile = item as Data.TextFile;
            if (textFile == null) return;
            if (document == null) return;
            if (parsedIds.Contains(textFile.ID)) return;

            action(textFile);
            parsedIds.Add(textFile.ID);

            if (!textFile.ReparseRequested)
            {
                await textFile.UpdateAsync();
            }
            else
            {
                DocumentParser? parser = textFile.CreateDocumentParser(DocumentParser.ParseModeEnum.BackgroundParse, null);
                if (parser != null)
                {
                    await parser.ParseAsync();
                    if (parser.ParsedDocument == null)
                    {
                        return;
                    }
                    await textFile.AcceptParsedDocumentAsync(parser.ParsedDocument);
                    await textFile.UpdateAsync();
                    textFile.PostUIUpdate();
                }
            }

            // parse all child nodes
            List<Data.Item> items = new List<Data.Item>();
            lock (textFile.Items)
            {
                foreach (Data.Item subItem in textFile.Items)
                {
                    items.Add(subItem);
                }
            }

            foreach (Data.Item subitem in items)
            {
                await parseHierarchyAsync(subitem, parsedIds, action);
            }

            if (textFile.ReparseRequested)
            {
                DocumentParser? parser = item.CreateDocumentParser(DocumentParser.ParseModeEnum.BackgroundParse, null);
                if (parser != null)
                {
                    await parser.ParseAsync();
                    if (parser.ParsedDocument == null) return;
                    await textFile.AcceptParsedDocumentAsync(parser.ParsedDocument);

                    // textFile.Items.ParsedDocuments Disposed already
                    System.Diagnostics.Debug.Print("# Re-ParseHier.Accept " + textFile.ID);
                    await textFile.UpdateAsync();
                }
            }
        }


    }
}
