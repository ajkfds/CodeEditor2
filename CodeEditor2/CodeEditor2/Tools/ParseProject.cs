using Avalonia.Controls;
using Avalonia.Threading;
using CodeEditor2.CodeEditor;
using CodeEditor2.Data;
using CodeEditor2.NavigatePanel;
using Securify.ShellLink.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Quic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using static CodeEditor2.CodeEditor.ParsedDocument;

namespace CodeEditor2.Tools
{
    internal class ParseProject
    {

        // to pase thread
        //private Tools.ProgressWindow progressWindow;
//        private volatile bool abort = false;

        public async Task Run(NavigatePanel.ProjectNode projectNode)
        {
            if (!Dispatcher.UIThread.CheckAccess()) throw new Exception();

            if (projectNode.Project == null) throw new Exception();

            projectNode.Project.Update(); // must be launch on UI thread

            // data update
            await projectNode.HierarchicalVisibleUpdateAsync();

            ProgressWindow progress = new ProgressWindow();
            progress.Title = "Loading " + projectNode.Text;

            progress.LoadedAction = async (progressWindow) => {
                await runParse(progressWindow,projectNode.Project);
                progressWindow.Close();
            };
            await progress.ShowDialog(Global.mainWindow);
        }


        private async Task runParse(ProgressWindow progressWindow, Project project)
        {
            // parse items
            int workerThreads = Environment.ProcessorCount;

            var channel = Channel.CreateUnbounded<Data.TextFile>();
            ChannelWriter<Data.TextFile> writer = channel.Writer;
            ChannelReader<Data.TextFile> reader = channel.Reader;

            List<Task> tasks = new List<Task>();
            for (int t = 0; t < workerThreads; t++)
            {
                ParseProjectUnit parseProjectUnit = new ParseProjectUnit(t.ToString());
                Task task = parseProjectUnit.Run(
                        reader,
                        progressWindow
                        );
                tasks.Add(task);
            }

            List<Item> items = project.FindItems(
                (x) => (x is Data.TextFile),
                (x) => (false),
                (x) => {
                    Data.TextFile textFile = (Data.TextFile)x;
                    if (textFile != null)
                    {
                        writer.WriteAsync(textFile);
                        progressWindow.ProgressMaxValue++;
                    }
                }
                );
            writer.Complete();

            await Task.WhenAll(tasks);
        }
    }
}
