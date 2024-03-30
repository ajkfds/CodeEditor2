using Avalonia.Controls;
using Avalonia.Threading;
using CodeEditor2.CodeEditor;
using CodeEditor2.Data;
using CodeEditor2.NavigatePanel;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Quic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static CodeEditor2.CodeEditor.ParsedDocument;

namespace CodeEditor2.Tools
{
    internal class ParseProject
    {

        // to pase thread
        //private Tools.ProgressWindow progressWindow;
        List<Data.Item> items;
        private volatile bool abort = false;

        public void Run(NavigatePanel.ProjectNode projectNode)
        {
            projectNode.Project.Update(); // must be launch on UI thread

            // data update
            projectNode.HierarchicalVisibleUpdate();
            items = projectNode.Project.FindItems(
                (x) => (x is Data.TextFile),
                (x) => (false)
                );

            Global.progressWindow.ProgressMaxValue = items.Count;

            Global.LockParse();

            runParse();

            Global.ReleaseParseLock();

            //progressWindow.Close();
        }


        private void runParse()
        {
            // parse items
            int i = 0;
            int workerThreads = 1;

            System.Collections.Concurrent.BlockingCollection<Data.TextFile> fileQueue = new System.Collections.Concurrent.BlockingCollection<Data.TextFile>();

            List<TextParseTask> tasks = new List<TextParseTask>();
            for (int t = 0; t < workerThreads; t++)
            {
                tasks.Add(new TextParseTask("ParseProject" + t.ToString()));
                tasks[t].Run(
                    fileQueue,
                    (
                        (f) =>
                        {
                            Dispatcher.UIThread.Post(
                                new Action(() =>
                                {
                                    Global.progressWindow.ProgressValue = i;
                                    Global.progressWindow.Message = f.Name;
                                    i++;
                                })
                                );
                        }
                    )
                );
            }

            foreach (Data.Item item in items)
            {
                if (!(item is Data.TextFile)) continue;
                fileQueue.Add(item as Data.TextFile);
            }
            fileQueue.CompleteAdding();

            while (!fileQueue.IsCompleted)
            {
                System.Threading.Thread.Sleep(10);
            }

            while (true)
            {
                int completeTasks = 0;
                foreach (TextParseTask task in tasks)
                {
                    if (task.Complete) completeTasks++;
                }
                if (completeTasks == tasks.Count) break;
            }
            abort = true;
        }
    }
}
