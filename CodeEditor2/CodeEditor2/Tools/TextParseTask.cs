using CodeEditor2.CodeEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.Tools
{
    internal class TextParseTask
    {
        public TextParseTask(string name)
        {
            this.name = name;
        }

        private string name;

        public void Run(System.Collections.Concurrent.BlockingCollection<Data.TextFile> files, Action<Data.TextFile> startParse)
        {
            this.fileQueue = files;
            this.startParse = startParse;

            if (thread != null) return;
            thread = new System.Threading.Thread(() => { worker(); });
            thread.Name = name;
            thread.Start();
        }

        System.Threading.Thread thread = null;
        public volatile bool Complete = false;

        private System.Collections.Concurrent.BlockingCollection<Data.TextFile> fileQueue;
        Action<Data.TextFile> startParse;

        private void worker()
        {
            foreach (Data.TextFile file in fileQueue.GetConsumingEnumerable())
            {
                parse(file);
            }
            Complete = true;
        }

        private void parse(Data.TextFile textFile)
        {
            if (textFile.RelativePath == @"fpga\hbirdkit\src\clkdivider.v")
            {
                string a = "";
            }

            CodeEditor.DocumentParser parser = textFile.CreateDocumentParser(CodeEditor.DocumentParser.ParseModeEnum.LoadParse);
            if (parser == null)
            {
                textFile.CodeDocument.LockThreadToUI();
                return;
            }
            parser.Document._tag = "TextParserTask:"+textFile.Name;

            if (textFile != null) startParse(textFile);
            parser.Parse();

            textFile.CodeDocument.CopyColorMarkFrom(parser.Document);

            if (textFile.ParsedDocument != null)
            {
                CodeEditor.ParsedDocument oldParsedDocument = textFile.ParsedDocument;
                textFile.ParsedDocument = null;
                oldParsedDocument.Dispose();
            }

            textFile.AcceptParsedDocument(parser.ParsedDocument);
            textFile.Close();

            //gc++;
            //if (gc > 100)
            //{
            //    System.GC.Collect();
            //    gc = 0;
            //    System.Diagnostics.Debug.Print("process memory " + (Environment.WorkingSet / 1024 / 1024).ToString() + "Mbyte");
            //}
        }
    }
}
