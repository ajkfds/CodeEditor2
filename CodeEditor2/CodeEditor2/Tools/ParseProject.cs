using Avalonia.Controls;
using Avalonia.Threading;
using CodeEditor2.CodeEditor;
using CodeEditor2.Data;
using CodeEditor2.NavigatePanel;
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
//        private volatile bool abort = false;

        public async Task Run(NavigatePanel.ProjectNode projectNode)
        {
            if (projectNode.Project == null) throw new Exception();

            projectNode.Project.Update(); // must be launch on UI thread

            // data update
            await projectNode.HierarchicalVisibleUpdateAsync();
            items = projectNode.Project.FindItems(
                (x) => (x is Data.TextFile),
                (x) => (false)
                );

            ProgressWindow progress = new ProgressWindow();
            progress.Title = "Loading " + projectNode.Text;
            progress.ProgressMaxValue = items.Count;

            progress.LoadedAction = async (p) => {
                await runParse(p);
                p.Close();
            };
            await progress.ShowDialog(Global.mainWindow);

        }


        private async Task runParse(ProgressWindow progress)
        {
            // parse items
            int i = 0;
            int workerThreads = 8;

            System.Collections.Concurrent.BlockingCollection<Data.TextFile> fileQueue = new System.Collections.Concurrent.BlockingCollection<Data.TextFile>();

            List<ParseProjectUnit> tasks = new List<ParseProjectUnit>();
            for (int t = 0; t < workerThreads; t++)
            {
                tasks.Add(new ParseProjectUnit("ParseProject" + t.ToString()));
                tasks[t].Run(
                    fileQueue,
                    (
                        (f) =>
                        {
                            Dispatcher.UIThread.Post(
                                new Action(() =>
                                {
                                    progress.ProgressValue = i;
                                    progress.Message = f.Name;
                                    i++;
                                })
                                );
                        }
                    )
                );
            }

            foreach (Data.Item item in items)
            {
                Data.TextFile textFile = (Data.TextFile)item;
                if (textFile == null) continue;
                fileQueue.Add(textFile);
            }
            fileQueue.CompleteAdding();

            while (!fileQueue.IsCompleted)
            {
                await Task.Delay(10);
            }

            while (true)
            {
                int completeTasks = 0;
                foreach (ParseProjectUnit task in tasks)
                {
                    if (task.Complete) completeTasks++;
                
                }
                if (completeTasks == tasks.Count) break;
            }
        }
    }
}
