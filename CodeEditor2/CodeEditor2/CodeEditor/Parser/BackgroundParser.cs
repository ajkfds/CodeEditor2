using CodeEditor2.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor.Parser
{
    public class BackgroundParser
    {
        public BackgroundParser()
        {
        }

        public void Run()
        {
            thread = new Thread(new ThreadStart(run));
            thread.Name = "BackGroundParser";
            thread.Start();
        }

        public void Terminate()
        {
            //            abortFlag = true;
            //            thread.Abort();
        }
        //        private volatile bool abortFlag = false;
        Thread? thread;

        public void EntryParse(TextFile textFile)
        {
            lock (toBackgroundStock)
            {
                System.Diagnostics.Debug.Print("# BackgroundParser.EntryParse.Add toBackGround:" + textFile.ID);
                toBackgroundStock.Add(textFile);
            }
        }

        private volatile bool parsing = false;
        private void run()
        {
            while (!Global.Abort)
            {
                DocumentParser? parser = null;

                {
                    TextFile? textFile = null;
                    lock (toBackgroundStock)
                    {
                        if (toBackgroundStock.Count != 0)
                        {
                            textFile = toBackgroundStock.Last();
                            toBackgroundStock.Clear();
                        }
                    }
                    parser = textFile?.CreateDocumentParser(DocumentParser.ParseModeEnum.EditParse);
                }


                if (parser != null)
                {
                    parsing = true;

                    {
                        Global.LockParse();

                        parser.Parse();

                        Global.ReleaseParseLock();
                    }

                    lock (fromBackgroundStock)
                    {
                        fromBackgroundStock.Add(parser);
                    }
                    parsing = false;
                }
                Thread.Sleep(1);
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
