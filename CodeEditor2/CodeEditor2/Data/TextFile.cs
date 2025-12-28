using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.FileTypes;
using CodeEditor2.NavigatePanel;
using Svg;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CodeEditor2.Data
{
    public class TextFile : File, ITextFile
    {
        public TextFile() : base() { }
        public static TextFile Create(string relativePath, Project project)
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
            TextFile fileItem = new TextFile() {
                Project = project,
                RelativePath = relativePath,
                Name = name
            };

            return fileItem;
        }
        public virtual void ModifyEditorContextMenu(ContextMenu contextMenu)
        {

        }

        public virtual string Key {
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


        public virtual async Task AcceptParsedDocumentAsync(CodeEditor2.CodeEditor.ParsedDocument newParsedDocument)
        {
            CodeEditor2.CodeEditor.ParsedDocument? oldParsedDocument = ParsedDocument;
            ParsedDocument = null;
            if (oldParsedDocument != null) oldParsedDocument.Dispose();

            ParsedDocument = newParsedDocument;
            await UpdateAsync();
        }
        public virtual void Close()
        {
            if (Dirty) return;
            if (CodeDocument == null) return;
            CodeDocument.Dispose();
        }

        public virtual bool Dirty
        {
            get
            {
                if (CodeDocument == null) return false;
                return CodeDocument.IsDirty;
            }
        }

        public virtual void LoadFormFile()
        {
            LoadDocumentFromFile();
        }


        protected CodeEditor.CodeDocument? document = null;

        public virtual CodeEditor.CodeDocument? CodeDocument
        {
            get
            {
                if (document == null)
                {
                    LoadDocumentFromFile();
                }
                else
                {
                    CashedStatus = null;
                }
                if (document == null) throw new Exception();
                return document;
            }
            protected set
            {
                document = value;
            }
        }

        public virtual void Save()
        {
            if (CodeDocument == null) return;
            try
            {
                using (System.IO.StreamWriter sw = new System.IO.StreamWriter(AbsolutePath))
                {
                    sw.Write(CodeDocument.CreateString());
                }
                CodeDocument.Clean();

                var info = new FileInfo(AbsolutePath);
                CashedStatus = new FileStatus(info.Length, info.LastWriteTimeUtc);
            }
            catch
            {
                Controller.AppendLog("Failed to save : "+RelativePath, Avalonia.Media.Colors.Red);
            }
        }

        public virtual DateTime? LoadedFileLastWriteTime
        {
            get
            {
                if (CashedStatus == null) return null;
                return CashedStatus.LastWriteTimeUtc;
            }
        }

        protected virtual void LoadDocumentFromFile()
        {
            try
            {
                if (document == null)
                {
                    document = new CodeEditor.CodeDocument(this);
                }

                var info = new FileInfo(AbsolutePath);
                CashedStatus = new FileStatus(info.Length, info.LastWriteTimeUtc);

                string text = ReadStableText(AbsolutePath);
                lock (document)
                {
                    document.TextDocument.Replace(0, document.TextDocument.TextLength, text);
                }
                document.ClearHistory();
                document.Clean();
            }
            catch
            {
                document = null;
            }
        }

        protected string ReadStableText(string path)
        {
            const int maxRetry = 3;
            const int delayMs = 50;

            for(int i = 0; i < maxRetry; i++)
            {
                var infoBefore = new FileInfo(path);
                long lengthBefore = infoBefore.Exists ? infoBefore.Length : -1;
                DateTime writeBefore = infoBefore.Exists ? infoBefore.LastWriteTimeUtc : DateTime.MinValue;

                using var fs = new FileStream(
                    path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.SequentialScan);
                using var sr = new StreamReader(fs, Encoding.UTF8, true);

                string text = sr.ReadToEnd();

                var infoAfter = new FileInfo(path);
                long lengthAfter = infoAfter.Exists ? infoAfter.Length : -1;
                DateTime writeAfter = infoAfter.Exists ? infoAfter.LastWriteTimeUtc : DateTime.MinValue;

                if (lengthBefore == lengthAfter && writeBefore == writeAfter)
                {
                    CashedStatus = new FileStatus(infoAfter.Length, infoAfter.LastWriteTimeUtc);
                    return text;
                }

                Thread.Sleep(delayMs);
            }

            using var fs2 = new FileStream(
                path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.SequentialScan);
            using var sr2 = new StreamReader(fs2, Encoding.UTF8, true);

            return sr2.ReadToEnd();
        }
        //public string GetMd5Hash()
        //{
        //    //if (document == null) return "";
        //    //byte[] data = System.Text.Encoding.UTF8.GetBytes(document.CreateString());

        //    //System.Security.Cryptography.MD5CryptoServiceProvider md5 =
        //    //    new System.Security.Cryptography.MD5CryptoServiceProvider();

        //    //byte[] bs = md5.ComputeHash(data);
        //    //md5.Clear();

        //    //System.Text.StringBuilder result = new System.Text.StringBuilder();
        //    //foreach (byte b in bs)
        //    //{
        //    //    result.Append(b.ToString("x2"));
        //    //}

        //    return result.ToString();
        //}


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

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        public override async Task OnChangedExternallyAsync()
        {
            if (_semaphore.CurrentCount == 0) return;

            await _semaphore.WaitAsync();
            try
            {
                if (Dirty)
                {
                    Tools.YesNoWindow checkUpdate = new Tools.YesNoWindow("Update Check", RelativePath + " changed externally. Can I dispose local change ?");
                    await checkUpdate.ShowDialog(Controller.GetMainWindow());
                    if (checkUpdate.Yes)
                    {
                        document = null;
                        LoadDocumentFromFile();
                        if (Controller.NavigatePanel.GetSelectedFile() == this)
                        {
                            await Controller.CodeEditor.SetTextFileAsync(this);
                        }
                        Controller.CodeEditor.Refresh();
                    }
                    else
                    {

                    }
                }
                else
                {
                    document = null;
                    LoadDocumentFromFile();
                    if (Controller.NavigatePanel.GetSelectedFile() == this)
                    {
                        await Controller.CodeEditor.SetTextFileAsync(this);
                    }
                    Controller.CodeEditor.Refresh();
                }
            }
            finally
            {
                _semaphore.Release();
            }

        }
        public override DocumentParser? CreateDocumentParser(DocumentParser.ParseModeEnum parseMode,System.Threading.CancellationToken? token)
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
            return null;
        }


        public virtual void TextEntered(TextInputEventArgs e)
        {

        }

        public virtual void TextEntering(TextInputEventArgs e)
        {

        }

        //public virtual void BeforeKeyPressed(KeyPressEventArgs e)
        //{

        //}

        //public virtual void AfterKeyPressed(System.Windows.Forms.KeyPressEventArgs e)
        //{
        //}

        // parse this text file hierarchy
        public async Task ParseHierarchyAsync(Action<ITextFile> action)
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
            if (textFile.CodeDocument == null) return;
            if (parsedIds.Contains(textFile.ID)) return;

            action(textFile);
            parsedIds.Add(textFile.ID);

            if (!textFile.ReparseRequested)
            {
                await textFile.UpdateAsync();
                textFile.CodeDocument.LockThreadToUI();
            }
            else
            {
                DocumentParser? parser = textFile.CreateDocumentParser(DocumentParser.ParseModeEnum.BackgroundParse,null);
                if (parser != null)
                {
                    await parser.ParseAsync();
                    if (parser.ParsedDocument == null)
                    {
                        return;
                    }
                    await textFile.AcceptParsedDocumentAsync(parser.ParsedDocument);
                    await textFile.UpdateAsync();
                }
            }

            // parse all child nodes
            List<Data.Item> items = new List<Data.Item>();
            lock (textFile.Items)
            {
                foreach (Data.Item subItem in textFile.Items.Values)
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
                DocumentParser? parser = item.CreateDocumentParser(DocumentParser.ParseModeEnum.BackgroundParse,null);
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
        //public async Task<string?> GetXxHash()
        //{
        //    if (CodeDocument == null) return null;

        //    string text = CodeDocument.CreateString();
        //    await Task.Run(() => { return System.IO.Hasg });
        //}

        public static readonly SemaphoreSlim CasheSemaphore = new SemaphoreSlim(1, 1);
        public virtual string CasheId
        {
            get
            {
                //byte[] data = Encoding.UTF8.GetBytes(AbsolutePath);
                //byte[] hashBytes = XxHash64.Hash(data);
                //string hex = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                string hex = RelativePath.Replace(@"\", "_").Replace("/", "_").Replace(":","_").Replace(".", "_")+".json";
                return hex;
            }
        }
        public virtual ParsedDocument? GetCashedParsedDocument()
        {
            if (!Global.ActivateCashe) return null;

            string path = Project.RootPath + System.IO.Path.DirectorySeparatorChar + ".cashe";
            if (!System.IO.Path.Exists(path)) System.IO.Directory.CreateDirectory(path);
            System.Diagnostics.Debug.Print("entry json " + path);

            path = path + System.IO.Path.DirectorySeparatorChar + CasheId;
            if (!System.IO.File.Exists(path)) return null;

            /*
// 1. シリアライズ時と同じ設定を用意する
// (実際には、この options は使い回せるように static なプロパティなどに保持しておくのが効率的です)
var options = new JsonSerializerOptions
{
    TypeInfoResolver = new DynamicHierarchyResolver(typeof(SyntaxNode), derivedMap)
};

// 2. JSON文字列（またはストリーム）から復元
// 基底クラスを指定して呼び出すと、適切な派生クラスが返ってくる
string json = "...(キャッシュファイルの内容)...";
SyntaxNode? restoredNode = JsonSerializer.Deserialize<SyntaxNode>(json, options);

// 3. 型判定をして利用
if (restoredNode is IdentifierNode idNode)
{
    Console.WriteLine($"Identifier found: {idNode.Name}");
}
else if (restoredNode is LiteralNode litNode)
{
    Console.WriteLine($"Literal value: {litNode.Value}");
}             
             */

            //var settings = new Newtonsoft.Json.JsonSerializerSettings
            //{
            //    TypeNameHandling = TypeNameHandling.Auto,
            //    Formatting = Formatting.Indented,
            //    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            //    ContractResolver = new DefaultContractResolver
            //    {
            //        IgnoreSerializableInterface = true,
            //        IgnoreSerializableAttribute = true
            //    }
            //};
            //var serializer = Newtonsoft.Json.JsonSerializer.Create(settings);

            //ParsedDocument? parsedDocument;
            //try
            //{
            //    //using (var reader = new StreamReader(path))
            //    //using (var jsonReader = new JsonTextReader(reader))
            //    //{
            //    //    parsedDocument = serializer.Deserialize<ParsedDocument>(jsonReader);
            //    //}
            //}
            //catch (Exception exception)
            //{
            //    CodeEditor2.Controller.AppendLog("exp " + exception.Message);
            //    return null;
            //}
            //return parsedDocument;
            return null;
        }

        public virtual async Task<bool> CreateCashe()
        {
            if (!CodeEditor2.Global.ActivateCashe) return true;

            if (ParsedDocument == null) return false;
            
            ParsedDocument casheObject = ParsedDocument;
            string path = Project.RootPath + System.IO.Path.DirectorySeparatorChar + ".cashe";
            if (!System.IO.Path.Exists(path)) System.IO.Directory.CreateDirectory(path);
            System.Diagnostics.Debug.Print("entry json " + path);

            // 派生クラスのマッピングを動的に作成（リフレクションやプラグインロード時に構築）
            var derivedMap = new Dictionary<string, Type>
            {
//                { "id", typeof(IdentifierNode) },
//                { "lit", typeof(LiteralNode) }
            };

            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = new DynamicHierarchyResolver(typeof(ParsedDocument), derivedMap),
                WriteIndented = true
            };

            try
            {
                string json = JsonSerializer.Serialize<ParsedDocument>(casheObject, options);
                using (var writer = new StreamWriter(path))
                {
                    await writer.WriteAsync(json);
                }
            }
            catch (Exception exception)
            {
                CodeEditor2.Controller.AppendLog("exp " + exception.Message);
            }



            //// シリアライズ実行


            /////////////////////////////////////////////////////////////////////////////


            //if (ParsedDocument == null) return false;

            //await TextFile.CasheSemaphore.WaitAsync();


            //var settings = new Newtonsoft.Json.JsonSerializerSettings
            //{
            //    TypeNameHandling = TypeNameHandling.Auto,
            //    Formatting = Formatting.Indented,
            //    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            //    ContractResolver = new DefaultContractResolver
            //    {
            //        IgnoreSerializableInterface = true,
            //        IgnoreSerializableAttribute = true
            //    }
            //};
            //var serializer = Newtonsoft.Json.JsonSerializer.Create(settings);

            //try
            //{
            //    System.Diagnostics.Debug.Print("start json " + path);
            //    using (var writer = new StreamWriter(path))
            //    using (var jsonWriter = new JsonTextWriter(writer))
            //    {
            //        serializer.Serialize(jsonWriter, casheObject);
            //    }
            //    System.Diagnostics.Debug.Print("complete json " + path);
            //}
            //catch (Exception exception)
            //{
            //    CodeEditor2.Controller.AppendLog("exp " + exception.Message);
            //}
            //finally
            //{
            //    TextFile.CasheSemaphore.Release();
            //}

            return true;
        }

    }
}
