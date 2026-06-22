using AjkAvaloniaLibs.Controls;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.NavigatePanel;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Hashing;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeEditor2.Data
{
    /// <summary>
    /// Represents a text file in the project with code editing and parsing capabilities.
    /// </summary>
    public class TextFile : File, ITextFile
    {
        /// <summary>
        /// Lock for thread-safe access to TextFile properties
        /// </summary>
        protected readonly ReaderWriterLockSlim textFileLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        // do not call await in locked status

        private CodeEditor.ParsedDocument? _parsedDocument = null;


        /// <summary>
        /// Gets or sets the parsed document containing parsed code information.
        /// </summary>
        public virtual CodeEditor.ParsedDocument? ParsedDocument
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return _parsedDocument;
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
                    _parsedDocument = value;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// Gets or sets the stored vertical scroll position for this text file.
        /// </summary>
        private double _storedVerticalScrollPosition = 0;

        /// <summary>
        /// Gets or sets the stored vertical scroll position for this text file.
        /// </summary>
        public double StoredVerticalScrollPosition
        {
            get
            {
                if (textFileLock.IsReadLockHeld) System.Diagnostics.Debugger.Break();
                textFileLock.EnterReadLock();
                try
                {
                    return _storedVerticalScrollPosition;
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
                    _storedVerticalScrollPosition = value;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
            }
        }


        /// <summary>
        /// Indicates whether a reparse has been requested for this text file.
        /// </summary>
        protected bool _reparseRequested = false;

        /// <summary>
        /// Gets or sets whether a reparse has been requested for this text file.
        /// </summary>
        public virtual bool ReparseRequested
        {
            get
            {
                if (textFileLock.IsReadLockHeld) System.Diagnostics.Debugger.Break();
                textFileLock.EnterReadLock();
                try
                {
                    return _reparseRequested;
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
                    _reparseRequested = value;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
            }
        }

        private CodeEditor.CodeDocument? _document = null;
        /// <summary>
        /// Gets the code document associated with this text file.
        /// </summary>
        public virtual CodeDocument? CodeDocument
        {
            get
            {
                if (textFileLock.IsReadLockHeld) System.Diagnostics.Debugger.Break();
                textFileLock.EnterReadLock();
                try
                {
                    return _document;
                }
                finally
                {
                    textFileLock.ExitReadLock();
                }
            }
            protected set
            {
                textFileLock.EnterWriteLock();
                try
                {
                    _document = value;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }

            }
        }

        /// <summary>
        /// The draw style for rendering code in this text file.
        /// </summary>
        private CodeDrawStyle? _drawStyle = null;

        /// <summary>
        /// Gets or sets the draw style for rendering code in this text file.
        /// </summary>
        public virtual CodeDrawStyle DrawStyle
        {
            get
            {
                textFileLock.EnterReadLock();
                try
                {
                    return _drawStyle ?? Global.DefaultDrawStyle;
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
                    _drawStyle = value;
                }
                finally
                {
                    textFileLock.ExitWriteLock();
                }
            }
        }

        //----------------------------------------------------------
        protected TextFile() : base() { }

        /// <summary>
        /// Creates a new TextFile instance asynchronously.
        /// </summary>
        /// <param name="relativePath">The relative path of the file within the project.</param>
        /// <param name="project">The project that contains this file.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the created TextFile.</returns>
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
            await fileItem.FileCheckAsync();
            if (fileItem.CodeDocument == null) System.Diagnostics.Debugger.Break();
            return fileItem;
        }

        /// <summary>
        /// Modifies the context menu of the code editor for this text file.
        /// </summary>
        /// <param name="contextMenu">The context menu to modify.</param>
        public virtual void ModifyEditorContextMenu(ContextMenu contextMenu)
        {

        }


        /// <summary>
        /// Gets the unique key for this text file.
        /// </summary>
        public virtual string Key
        {
            get
            {
                return RelativePath;
            }
        }

        /// <summary>
        /// Converts this instance to a TextFile.
        /// </summary>
        /// <returns>This TextFile instance.</returns>
        public TextFile ToTextFile()
        {
            return this;
        }
        public override void Dispose()
        {
            CodeDocument?.Dispose();
            CodeDocument = null;
            base.Dispose();
        }



        /// <summary>
        /// Gets whether the code document is cached (loaded).
        /// </summary>
        public bool IsCodeDocumentCashed
        {
            get
            {
                return (CodeDocument != null) ;
            }
        }


        /// <summary>
        /// Posts a parse operation for this text file.
        /// </summary>
        public async void PostParse()
        {
            try
            {
                System.Diagnostics.Debug.Print("### postParse" + RelativePath);
                ParseWorker parseWorker = new();
                await Task.Run(async () => { await parseWorker.Parse(this); });
            }
            catch(Exception exception)
            {
                CodeEditor2.Controller.AppendLog("failedToPostParse:"+exception.Message, Avalonia.Media.Colors.Red);
            }
        }

        /// <summary>
        /// Accepts a new parsed document and replaces the existing one.
        /// </summary>
        /// <param name="newParsedDocument">The new parsed document to accept.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public virtual async Task AcceptParsedDocumentAsync(CodeEditor.Parser.DocumentParser parser)
        {
            CodeEditor2.CodeEditor.ParsedDocument? newParsedDocument = parser.ParsedDocument;
            if (newParsedDocument == null) return;

            CodeEditor2.CodeEditor.ParsedDocument? oldParsedDocument;
            oldParsedDocument = ParsedDocument;
            ParsedDocument = newParsedDocument;
            oldParsedDocument?.Dispose();

            TextFile? currentTextFile = await Controller.CodeEditor.GetTextFileAsync();
            if(currentTextFile == this)
            {
                currentTextFile.CodeDocument?.CopyColorMarkFrom(parser.Document);
            }

            // update navigate node, message and refresh editor
            await UpdateAsync();
        }

        /// <summary>
        /// Closes the text file and releases the code document if not dirty.
        /// </summary>
        public virtual void Close()
        {
            bool isDirty;
            CodeEditor.CodeDocument? doc = CodeDocument;
            isDirty = doc == null ? false : doc.IsDirty;
 
            if (isDirty) return;
            if (doc == null) return;
            doc.Dispose();
        }

        /// <summary>
        /// Gets whether the text file has unsaved changes.
        /// </summary>
        public virtual bool Dirty
        {
            get
            {
                CodeDocument? doc = CodeDocument;
                if (doc == null) return false;
                return doc.IsDirty;
            }
        }
        /// <summary>
        /// Gets the code document asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains the code document.</returns>
        public virtual Task<CodeEditor.CodeDocument> GetCodeDocumentAsync()
        {
            PostStatusCheck();
            CodeDocument? doc = CodeDocument;
            if (doc == null) throw new Exception();
            return Task.FromResult(doc);
        }



        public override void Remove()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() => { Remove(); });
                return;
            }

            if (Controller.NavigatePanel.GetSelectedFile() == this)
            {
                Controller.NavigatePanel.SelectNodePost(Project.NavigatePanelNode);
            }

            if (Parent != null)
            {
                Parent.Items.TryRemove(Name);
                Parent.NavigatePanelNode.UpdateVisual();
            }
        }

        protected string currentFileHash = "";
        protected string currentFileText = "";
        protected ulong currentFileVersion = 0;
        /// <summary>
        /// Saves the text file to disk asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous save operation.</returns>
        public async virtual Task SaveAsync()
        {
            await _fileSemaphore.WaitAsync();
            try
            {
                WeakReference<ListViewItem> itemRef = await Controller.AppendLogAndGetItem("Save " + RelativePath + "...", Avalonia.Media.Colors.Green);

                CodeEditor.CodeDocument? doc = CodeDocument;

                if (doc == null) return;

                string filePath = AbsolutePath;

                // Normalize line endings to \n in UI thread
                if (Dispatcher.UIThread.CheckAccess())
                {
                    NormalizeLineEndings(doc);
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(() => NormalizeLineEndings(doc));
                }

                string saveText = doc.CreateString();
                ulong savedVersion = doc.Version;

                await Task.Run(
                    async () =>
                    {
                        await DataAccess.SaveFileAsync(Project, RelativePath, saveText);
                    }
                );

                if (savedVersion == doc.Version) doc.Clean();
                currentFileHash = GetHash(saveText);
                currentFileVersion = savedVersion;
            }
            finally
            {
                _fileSemaphore.Release();
            }
        }


        protected virtual void CreateCodeDocument()
        {
            // lockは呼び出し元で取得している前提
            CodeDocument = new CodeEditor.CodeDocument(this);
        }


        public void CopyCodeDocumentFrom(TextFile text)
        {
            CodeDocument = text.CodeDocument;
            //await Dispatcher.UIThread.InvokeAsync(() =>
            //{
                //CodeDocument? doc = CodeDocument;
                //CodeDocument? sourceDoc = text.CodeDocument;
                //if (doc == null || sourceDoc == null) return;
                //loadFileHash = text.loadFileHash;
                //doc.TextDocument.Replace(0, doc.TextDocument.TextLength, sourceDoc.TextDocument.Text);
                //doc.Clean();
            //});
        }

        // ファイルの状態を確認する。CodeDocumentが古くなってしまっている場合には更新する。
        private readonly SemaphoreSlim _fileSemaphore = new(1, 1);
        protected virtual async Task FileCheckAsync()
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

                    if (CodeDocument == null)
                    {
                        initialLoad = true;
                        CreateCodeDocument();
                        if (CodeDocument == null) throw new Exception();
                    }
                    else
                    {
                        if (CodeDocument.Version != currentFileVersion) dirty = true;
                    }

                    if (initialLoad)
                    {
                        string? cashedText = await DataAccess.TryGetChasheAsync(Project, RelativePath);
                        if (cashedText != null)
                        {
                            PostStatusCheck();
                        }

                        string text = await DataAccess.GetFileTextAsync(Project, RelativePath);
                        // 保存時に正規化しているため、読み込み時にも正規化してハッシュ計算を一致させる
                        text = text.Replace("\r\n", "\n").Replace("\r", "\n");

                        await Dispatcher.UIThread.InvokeAsync(async () =>
                        {
                            var doc = CodeDocument;
                            if (doc != null)
                            {
                                doc.TextDocument.Replace(0, doc.TextDocument.TextLength, text);
                                doc.Clean();
                            }
                            currentFileHash = GetHash(text);
                            currentFileText = text;
                            await FileChangedAsync();
                        });

                    }
                    else
                    {
                        await FileCheckBackgroundAsync(dirty);
                    }

                }
                catch (FileNotFoundException)
                {
                    IsDeleted = true;
                    Remove();
                }
                catch (Exception ex)
                {
                    CodeEditor2.Controller.AppendLog("Failed to load file: " + AbsolutePath + " Error: " + ex.Message, Avalonia.Media.Colors.Red);
                    if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                }
            }
            finally
            {
                _fileSemaphore.Release();
            }

        }

        private async Task FileCheckBackgroundAsync(bool dirty)
        {
            // run on background
            if (Dispatcher.UIThread.CheckAccess())
            { // ui thread
                await Task.Run(async () => await FileCheckBackgroundAsync(dirty));
                return;
            }

            string text = await DataAccess.GetFileTextAsync(Project, RelativePath);
            // 保存時に正規化しているため、読み込み時にも正規化してハッシュ計算を一致させる
            text = text.Replace("\r\n", "\n").Replace("\r", "\n");
            string newHash = GetHash(text);
            if (newHash == currentFileHash) return;

            if (currentFileVersion != CodeDocument.Version & currentFileHash != "")
            { // dirty & 
                Controller.AppendLog("Conflict Detected "+RelativePath);
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    Tools.YesNoWindow checkUpdate = new Tools.YesNoWindow("Update Check\n", RelativePath + " changed externally.\nCan I dispose local change and accept external file?");
                    await CodeEditor2.Controller.ShowDialog(checkUpdate);
                    if (!checkUpdate.Yes) // keep current file
                    {
                        currentFileHash = newHash;
                        currentFileVersion = CodeDocument.Version;
                        currentFileText = text;
                        return;
                    }
                });
            }
            if (newHash == currentFileHash) return;

            // load current file
            if (Dispatcher.UIThread.CheckAccess())
            {
                CodeDocument doc = CodeDocument;

                if (doc != null)
                {
                    doc.TextDocument.Replace(0, doc.TextDocument.TextLength, text);
                    doc.Clean();
                }
                currentFileHash = newHash;
                currentFileText = text;
                await FileChangedAsync();
            }
            else
            { // background
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    var doc = CodeDocument;
                    if (doc != null)
                    {
                        doc.TextDocument.Replace(0, doc.TextDocument.TextLength, text);
                        doc.Clean();
                    }
                    currentFileHash = newHash;
                    await FileChangedAsync();
                });
            }

        }

        // ファイルが修正されたとき
        public override Task FileChangedAsync()
        {
            // エディタで選択されている場合には再描画
            if (Dispatcher.UIThread.CheckAccess() && Controller.NavigatePanel.GetSelectedFile() == this)
            {
                Controller.CodeEditor.PostRefresh();
            }

            NavigatePanelNode?.PostUpdate();

            return Task.CompletedTask;
        }





        /// <summary>
        /// Computes the hash of the given text using XxHash64.
        /// </summary>
        /// <param name="text">The text to hash.</param>
        /// <returns>The hash as a lowercase hex string.</returns>
        public static string GetHash(string text)
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            byte[] hashBytes = XxHash64.Hash(data);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// Normalizes line endings in the document to \n (LF).
        /// </summary>
        /// <param name="doc">The code document to normalize.</param>
        private void NormalizeLineEndings(CodeDocument doc)
        {
            string currentText = doc.CreateString();
            string normalizedText = currentText.Replace("\r\n", "\n").Replace("\r", "\n");
            if (currentText != normalizedText)
            {
                doc.TextDocument.Replace(0, doc.TextDocument.TextLength, normalizedText);
            }
        }

        protected override NavigatePanelNode CreateNode()
        {
            return new TextFileNode(this);
        }

        /// <summary>
        /// Updates the text file asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous update operation.</returns>
        public override async Task UpdateAsync()
        {
            await base.UpdateAsync();
            PostStatusCheck();

            Dispatcher.UIThread.Post(() =>
            {
                NavigatePanelNode?.UpdateVisual();
                if (CodeEditor2.Controller.NavigatePanel.GetSelectedFile() == this)
                {
                    CodeEditor2.Controller.CodeEditor.PostRefresh();
                    if (ParsedDocument != null) CodeEditor2.Controller.MessageView.Update(ParsedDocument);
                }
            });
        }


        /// <summary>
        /// Posts a status check for the text file.
        /// </summary>
        public override void PostStatusCheck()
        {
            _ = Task.Run(async () => { await FileCheckAsync(); });
        }


        /// <summary>
        /// Creates a document parser for the specified parse mode.
        /// </summary>
        /// <param name="parseMode">The parse mode.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>A DocumentParser instance, or null if not supported.</returns>
        public override DocumentParser? CreateDocumentParser(DocumentParser.ParseModeEnum parseMode, System.Threading.CancellationToken? token)
        {
            return null;
        }

        /// <summary>
        /// Gets a popup item at the specified index.
        /// </summary>
        /// <param name="Version">The document version.</param>
        /// <param name="index">The index in the document.</param>
        /// <returns>A PopupItem if available, or null.</returns>
        public virtual PopupItem? GetPopupItem(ulong Version, int index)
        {
            return null;
        }

        /// <summary>
        /// Gets auto-complete items at the specified index.
        /// </summary>
        /// <param name="index">The index in the document.</param>
        /// <param name="candidateWord">The candidate word for auto-completion.</param>
        /// <returns>A list of AutocompleteItem if available, or null.</returns>
        public virtual List<AutocompleteItem>? GetAutoCompleteItems(int index, out string? candidateWord)
        {
            candidateWord = "";
            return null;
        }

        /// <summary>
        /// Gets tool items at the specified index.
        /// </summary>
        /// <param name="index">The index in the document.</param>
        /// <returns>A list of ToolItem if available, or null.</returns>
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

        /// <summary>
        /// Gets or sets the custom tool item provider.
        /// </summary>
        public static Action<List<ToolItem>>? CustomizeTooltem;

        /// <summary>
        /// Called when text is entered in the editor.
        /// </summary>
        /// <param name="e">The text input event args.</param>
        public virtual void TextEntered(TextInputEventArgs e)
        {

        }

        /// <summary>
        /// Called when text is entering the editor.
        /// </summary>
        /// <param name="e">The text input event args.</param>
        public virtual void TextEntering(TextInputEventArgs e)
        {

        }

        /// <summary>
        /// Parses the text file hierarchy asynchronously.
        /// </summary>
        /// <param name="action">The action to perform on each text file in the hierarchy.</param>
        /// <returns>A task that represents the asynchronous parse operation.</returns>
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

        protected readonly SemaphoreSlim itemUpdateSemaphore = new SemaphoreSlim(1, 1);
        private async Task parseHierarchyAsync(Data.Item item, List<string> parsedIds, Action<ITextFile> action)
        {
            if (item == null) return;
            Data.ITextFile? textFile = item as Data.TextFile;
            if (textFile == null) return;

            var doc = CodeDocument;

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
                    await textFile.AcceptParsedDocumentAsync(parser);
                }
            }

            // parse all child nodes
            List<Data.Item> items = new List<Data.Item>();

            await itemUpdateSemaphore.WaitAsync();

            try
            {
                foreach (Data.Item subItem in textFile.Items)
                {
                    items.Add(subItem);
                }
            }finally
            {
                itemUpdateSemaphore.Release();
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
                    await textFile.AcceptParsedDocumentAsync(parser);

                    // textFile.Items.ParsedDocuments Disposed already
                    System.Diagnostics.Debug.Print("# Re-ParseHier.Accept " + textFile.ID);
                    await textFile.UpdateAsync();
                }
            }
        }


    }
}
