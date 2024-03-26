using CodeEditor2.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor
{
    public class BackroungParser
    {
        public BackroungParser()
        {
        }

        public void Run()
        {
            thread = new System.Threading.Thread(new System.Threading.ThreadStart(run));
            thread.Name = "BackGroundParser";
            thread.Start();
        }

        public void Terminate()
        {
//            abortFlag = true;
            //            thread.Abort();
        }
//        private volatile bool abortFlag = false;
        System.Threading.Thread thread;

        public void EntryParse(TextFile textFile)
        {
            lock (toBackgroundStock)
            {
                toBackgroundStock.Add(textFile);
            }
        }

        private volatile bool parsing = false;
        private void run()
        {
            while (!Global.Abort)
            {
                DocumentParser parser = null;
                lock (toBackgroundStock)
                {
                    if (toBackgroundStock.Count != 0)
                    {
                        TextFile textFile = toBackgroundStock.Last();
                        parser = textFile.CreateDocumentParser(DocumentParser.ParseModeEnum.EditParse);
//                        parser = toBackgroundStock.Last();
                        toBackgroundStock.Clear();
                    }
                }
                if (parser != null)
                {
                    parsing = true;

                    {
                        Global.ParseSemaphore.WaitOne();
                        
                        parser.Parse();

                        Global.ParseSemaphore.Release();
                    }

                    lock (fromBackgroundStock)
                    {
                        fromBackgroundStock.Add(parser);
                    }
                    parsing = false;
                }
                System.Threading.Thread.Sleep(1);
            }
        }

        public int RemainingStocks
        {
            get
            {
                lock (toBackgroundStock)
                {
                    //                    if (parsing) return toBackgroundStock.Count + 1;
                    if (parsing) return toBackgroundStock.Count;
                    return toBackgroundStock.Count;
                }
            }
        }

        public DocumentParser GetResult()
        {
            lock (fromBackgroundStock)
            {
                if (fromBackgroundStock.Count == 0) return null;
                DocumentParser parser = fromBackgroundStock.Last();
                fromBackgroundStock.Clear();
                return parser;
            }
        }

        private List<TextFile> toBackgroundStock = new List<TextFile>();
//        private List<DocumentParser> toBackgroundStock = new List<DocumentParser>();
        private List<DocumentParser> fromBackgroundStock = new List<DocumentParser>();

    }
}
