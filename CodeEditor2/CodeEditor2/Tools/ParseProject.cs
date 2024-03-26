using Avalonia.Controls;
using Avalonia.Threading;
using CodeEditor2.CodeEditor;
using CodeEditor2.NavigatePanel;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static CodeEditor2.CodeEditor.ParsedDocument;

namespace CodeEditor2.Tools
{
    internal class ParseProject
    {
        public async static void Run(NavigatePanel.ProjectNode projectNode)
        {
            Tools.ProgressWindow progressWindow = new Tools.ProgressWindow(projectNode.Name, "Loading...", 100);
            progressWindow.Show();
            progressWindow.Topmost = true;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            projectNode.Project.Update();

            // data update
            projectNode.HierarchicalVisibleUpdate();
            List<Data.Item> items = projectNode.Project.FindItems(
                (x) => (x is Data.TextFile),
                (x) => (false)
                );
            progressWindow.ProgressMaxValue = items.Count;

            
            {
                Global.ParseSemaphore.WaitOne();

                // parse items
                int i = 0;
                int workerThreads = 1;

                System.Collections.Concurrent.BlockingCollection<Data.TextFile> fileQueue = new System.Collections.Concurrent.BlockingCollection<Data.TextFile>();

                List<TextParseTask> tasks = new List<TextParseTask>();
                for (int t = 0; t < workerThreads; t++)
                {
                    tasks.Add(new TextParseTask("ParseProject"+t.ToString()));
                    tasks[t].Run(
                        fileQueue,
                        (
                            (f) =>
                            {
                                Dispatcher.UIThread.Post(
                                    new Action(() =>
                                    {
                                        progressWindow.ProgressValue = i;
                                        progressWindow.Message = f.Name;
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
                    await Task.Delay(10);
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

                //    gc++;
                //    if (gc > 100)
                //    {
                //        System.GC.Collect();
                //        gc = 0;
                //        System.Diagnostics.Debug.Print("process memory " + (Environment.WorkingSet / 1024 / 1024).ToString() + "Mbyte");
                //    }
                //}
                Global.ParseSemaphore.Release();
            }


            progressWindow.Close();
        }
    }
}
