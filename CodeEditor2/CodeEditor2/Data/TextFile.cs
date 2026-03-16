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
        /// <summary>
        /// Lock for thread-safe access to TextFile properties
        /// </summary>
        protected readonly ReaderWriterLockSlim textFileLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

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

        protected CodeEditor.CodeDocument? document = null;
        public virtual void ModifyEditorContextMenu(ContextMenu contextMenu)
        {

        }

        public void CheckUnlocked()
        {
            if (textFileLock.IsReadLockHeld) System.Diagnostics.Debugger.Break();
            if (textFileLock.IsWriteLockHeld) System.Diagnostics.Debugger.Break();
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
            textFileLock.EnterWriteLock();
            try
            {
                if (document != null) document.Dispose();
                document = null;
            }
            finally
            {
                textFileLock.ExitWriteLock();
            }
            base.Dispose();
        }

        private double storedVerticalScrollPosition = 0;
        public double StoredVerticalScrollPosition
        {
            get
            {
                if (textFileLock.IsReadLockHeld) System.Diagnostics.Debugger.Break();
                textFileLock.EnterReadLock();
                try
                {
                    return storedVerticalScrollPosition;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
            set
            {
                textFileLock.EnterWriteLock();
                try
                {
                    storedVerticalScrollPosition = value;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
            }
        }

        protected bool reparseRequested = false;
        public virtual bool ReparseRequested
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return reparseRequested;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
            set
            {
                textFileLock.EnterWriteLock();
                try
                {
                    reparseRequested = value;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
            }
        }

        public bool IsCodeDocumentCashed
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return document != null;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
        }

        protected CodeEditor.ParsedDocument? parsedDocument = null;
        public virtual CodeEditor.ParsedDocument? ParsedDocument
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return parsedDocument;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
            set
            {
                textFileLock.EnterWriteLock();
                try
                {
                    parsedDocument = value;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
            }
        }

        public void PostParse()
        {
            ParseWorker parseWorker = new ParseWorker();
            Task.Run(async () => { await parseWorker.Parse(this); });
        }
        public virtual Task AcceptParsedDocumentAsync(CodeEditor2.CodeEditor.ParsedDocument newParsedDocument)
        {
            textFileLock.EnterWriteLock();
            CodeEditor2.CodeEditor.ParsedDocument? oldParsedDocument;
            try
            {
                oldParsedDocument = parsedDocument;
                parsedDocument = null;
            }
            finally
            {
                textFileLock.ExitWriteLock();
            }

            if (oldParsedDocument != null) oldParsedDocument.Dispose();

            textFileLock.EnterWriteLock();
            try
            {
                parsedDocument = newParsedDocument;
            }
            finally
            {
                textFileLock.ExitWriteLock();
            }

            PostUIUpdate();
            return Task.CompletedTask;
        }
        public virtual void Close()
        {
            textFileLock.EnterReadLock();
            bool isDirty;
            CodeEditor.CodeDocument? doc;
            try
            {
                isDirty = document == null ? false : document.IsDirty;
                doc = document;
            }
            finally
            {
                textFileLock.ExitReadLock();
            }

            if (isDirty) return;
            if (doc == null) return;
            doc.Dispose();
        }

        public virtual bool Dirty
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    if (document == null) return false;
                    return document.IsDirty;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
        }
        public virtual Task<CodeEditor.CodeDocument> GetCodeDocumentAsync()
        {
            postFileCheck();
            textFileLock.EnterReadLock();
            try
            {
                if (document == null) throw new Exception();
                return Task.FromResult(document);
            }
            finally
            {
                textFileLock.ExitReadLock();
            }
        }

        public virtual CodeDocument CodeDocument
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return document;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
        }


        public override void Remove()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => { Remove(); });
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

                textFileLock.EnterReadLock();
                CodeEditor.CodeDocument? doc;
                try
                {
                    doc = document;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }

                if (doc == null) return;

                string filePath = AbsolutePath;
                string saveText = doc.CreateString();
                ulong savedVersion = doc.Version;

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
                if (savedVersion == doc.Version) doc.Clean();
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
            // lockは呼び出し元で取得している前提
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
                    if (textFileLock.IsReadLockHeld) System.Diagnostics.Debugger.Break();
                    textFileLock.EnterWriteLock();
                    try
                    {
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
                    }
                    finally
                    {
                        textFileLock.ExitWriteLock();
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
                        textFileLock.EnterReadLock();
                        var doc = document;
                        textFileLock.ExitReadLock();

                        if (doc != null)
                        {
                            doc.TextDocument.Replace(0, doc.TextDocument.TextLength, text);
                            doc.Clean();
                        }
                        loadFileHash = newHash;
                        if (initialLoad)
                        {
                            textFileLock.EnterReadLock();
                            var doc2 = document;
                            textFileLock.ExitReadLock();
                            if (doc2 != null) doc2.ClearHistory();
                        }
                        await FileChangedAsync();
                    }
                    else
                    {
                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            textFileLock.EnterReadLock();
                            var doc = document;
                            textFileLock.ExitReadLock();

                            if (doc != null)
                            {
                                doc.TextDocument.Replace(0, doc.TextDocument.TextLength, text);
                                doc.Clean();
                            }
                            loadFileHash = newHash;
                            if (initialLoad)
                            {
                                textFileLock.EnterReadLock();
                                var doc2 = document;
                                textFileLock.ExitReadLock();
                                if (doc2 != null) doc2.ClearHistory();
                            }
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
            if (Controller.NavigatePanel.GetSelectedFile() == this && Dispatcher.UIThread.CheckAccess()) 
            {
                Controller.CodeEditor.PostRefresh();
            }
            
            // NavigatePanelNodeへのアクセスはUIスレッド 安全ではないため、Dispatcherを使用
            Dispatcher.UIThread.Post(() =>
            {
                if (NavigatePanelNode != null) NavigatePanelNode.PostUpdate();
            });
            
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
                        textFileLock.EnterReadLock();
                        var parsed = parsedDocument;
                        textFileLock.ExitReadLock();

                        if (parsed != null) Controller.MessageView.Update(parsed);
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

        private CodeDrawStyle? drawStyle = null;
        public virtual CodeDrawStyle DrawStyle
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return drawStyle ?? Global.DefaultDrawStyle;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
            set
            {
                textFileLock.EnterWriteLock();
                try
                {
                    drawStyle = value;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
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

            textFileLock.EnterReadLock();
            var doc = document;
            textFileLock.ExitReadLock();

            if (doc == null) return;
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
