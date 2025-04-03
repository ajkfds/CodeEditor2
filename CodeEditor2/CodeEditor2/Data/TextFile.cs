using Avalonia.Input;
using AvaloniaEdit.Document;
using CodeEditor2.CodeEditor;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.Parser;
using CodeEditor2.CodeEditor.PopupHint;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.FileTypes;
using CodeEditor2.NavigatePanel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public bool ParseValid
        {
            get
            {
                CodeEditor2.CodeEditor.CodeDocument doc = CodeDocument;
                CodeEditor2.CodeEditor.ParsedDocument? parsedDocument = ParsedDocument;
                if (doc == null) return false;
                if (parsedDocument == null) return false;
                if (doc.Version == parsedDocument.Version) return true;
                return false;
            }
        }

        public virtual void AcceptParsedDocument(CodeEditor2.CodeEditor.ParsedDocument newParsedDocument)
        {
            CodeEditor2.CodeEditor.ParsedDocument? oldParsedDocument = ParsedDocument;
            ParsedDocument = null;
            if (oldParsedDocument != null) oldParsedDocument.Dispose();

            ParsedDocument = newParsedDocument;
            Update();
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
            loadDocumentFromFile();
        }


        protected CodeEditor.CodeDocument? document = null;

        public virtual CodeEditor.CodeDocument CodeDocument
        {
            get
            {
                if (document == null)
                {
                    loadDocumentFromFile();
                }
                else
                {
                    loadedFileLastWriteTime = null;
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

            using (System.IO.StreamWriter sw = new System.IO.StreamWriter(AbsolutePath))
            {
                sw.Write(CodeDocument.CreateString());
            }
            CodeDocument.Clean();
            loadedFileLastWriteTime = System.IO.File.GetLastWriteTime(AbsolutePath);
        }

        public virtual DateTime? LoadedFileLastWriteTime
        {
            get
            {
                return loadedFileLastWriteTime;
            }
        }

        protected DateTime? loadedFileLastWriteTime;
        private void loadDocumentFromFile()
        {
            try
            {
                if (document == null)
                {
                    document = new CodeEditor.CodeDocument(this);
                }
                using (System.IO.StreamReader sr = new System.IO.StreamReader(AbsolutePath))
                {
                    loadedFileLastWriteTime = System.IO.File.GetLastWriteTime(AbsolutePath);

                    string text = sr.ReadToEnd();
                    document.TextDocument.Replace(0, document.TextDocument.TextLength, text);
                    //document.Replace(0, document.Length, 0, text);
                    //document.ClearHistory();
                    document.Clean();
                }
            }
            catch
            {
                document = null;
            }
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


        public override DocumentParser? CreateDocumentParser(DocumentParser.ParseModeEnum parseMode)
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
        public void ParseHierarchy(Action<ITextFile> action)
        {
            //parse complete id list
            List<string> parsedIds = new List<string>();

            parseHierarchy(this, parsedIds, action);
            Update();

            if (NavigatePanelNode != null)
            {
                NavigatePanelNode.HierarchicalVisibleUpdate();
            }
            System.Diagnostics.Debug.Print("### TextFile.ParseHierarchy compelet " + parsedIds.Count.ToString() + "module parsed");
        }

        private void parseHierarchy(Data.Item item, List<string> parsedIds, Action<ITextFile> action)
        {
            if (item == null) return;
            Data.ITextFile? textFile = item as Data.TextFile;
            if (textFile == null) return;
            if (textFile.CodeDocument == null) return;
            if (parsedIds.Contains(textFile.ID)) return;

//            System.Diagnostics.Debug.Print("# Try ParseHier "+textFile.ID);
            action(textFile);
            parsedIds.Add(textFile.ID);

            if (textFile.ParseValid & !textFile.ReparseRequested)
            {
                System.Diagnostics.Debug.Print("### TextFileparseHierarchy parse skip : " + textFile.ID);
                textFile.Update();
                textFile.CodeDocument.LockThreadToUI();
            }
            else
            {
                DocumentParser? parser = textFile.CreateDocumentParser(DocumentParser.ParseModeEnum.BackgroundParse);
                if (parser != null)
                {
                    parser.Parse();
                    if (parser.ParsedDocument == null)
                    {
                        System.Diagnostics.Debug.Print("### TextFileparseHierarchy not parsed : " + textFile.ID + "," + parsedIds.Count.ToString() + "module parsed");
                        return;
                    }
                    textFile.AcceptParsedDocument(parser.ParsedDocument);
                    System.Diagnostics.Debug.Print("### TextFileparseHierarchy parsed : " + textFile.ID+ ","+parsedIds.Count.ToString() + "module parsed");
                    textFile.Update();
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
                parseHierarchy(subitem, parsedIds, action);
            }

            if (textFile.ReparseRequested)
            {
                DocumentParser? parser = item.CreateDocumentParser(DocumentParser.ParseModeEnum.BackgroundParse);
                if (parser != null)
                {
                    parser.Parse();
                    if (parser.ParsedDocument == null) return;
                    textFile.AcceptParsedDocument(parser.ParsedDocument);

                    // textFile.Items.ParsedDocuments Disposed already
                    System.Diagnostics.Debug.Print("# Re-ParseHier.Accept " + textFile.ID);
                    textFile.Update();
                }
            }
        }
    }
}
