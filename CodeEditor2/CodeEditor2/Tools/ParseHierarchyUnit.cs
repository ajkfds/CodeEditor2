using Avalonia.Threading;
using CodeEditor2.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.Tools
{
    internal class ParseHierarchyUnit
    {
        public ParseHierarchyUnit(string name,ProgressWindow progress)
        {
            this.name = name;
            this.progress = progress;
        }

        string name;
        ProgressWindow progress;
        public void Run(Data.Item item, Action<Data.TextFile> startParse)
        {
            this.item = item;
            this.startParse = startParse;

            if (thread != null) return;
            thread = new System.Threading.Thread(() => { worker(); });
            thread.Name = name;
            thread.Start();
        }

        System.Threading.Thread? thread = null;
        public volatile bool Complete = false;

        private Data.Item? item;
        Action<Data.TextFile>? startParse;

        private void worker()
        {
            if (item == null) return;
            Data.ITextFile? textFile = item as Data.TextFile;
            if (textFile == null) return;

            //textFile.ParseHierarchy((tFile) => {
            //    textFile.ParseHierarchy((tFile) =>
            //    {
            //        Dispatcher.UIThread.Invoke(new Action(() => { Global.ProgressWindow.Message = tFile.ID; }));
            //    });
            //});
            textFile.ParseHierarchy((tFile) =>
            {
                Dispatcher.UIThread.Invoke(new Action(() => { progress.Message = tFile.ID; }));
            });
            Complete = true;
        }

    }
}
