using Avalonia.Controls;
using Avalonia.Threading;
using CodeEditor2.CodeEditor;
using CodeEditor2.Data;
using CodeEditor2.NavigatePanel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static CodeEditor2.CodeEditor.ParsedDocument;

namespace CodeEditor2.Tools
{
    public class ParseHierarchy
    {
        // All Parse executed in UI Thread

//        public static List<ParsedDocument> unlockdPaeedsedDocument = new List<ParsedDocument>();

        public static async Task Run(NavigatePanel.NavigatePanelNode rootNode)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            System.Diagnostics.Debug.Print("parse hier sw " + sw.ElapsedMilliseconds.ToString());

            ProgressWindow progress = new ProgressWindow();
            progress.Title = "Reparse " + rootNode.Text;
            int progressMax = 20;
            progress.ProgressMaxValue = progressMax;
            progress.LoadedAction = async (p) => {
                int i = 0;
                Item? item = rootNode.Item;
                if (item == null) throw new Exception();
                ParseHierarchyUnit unit = new ParseHierarchyUnit("ParseHier" + item.Name,p);
                unit.Run(item,
                        (
                            (f) =>
                            {
                                Dispatcher.UIThread.Post(
                                    new Action(() =>
                                    {
                                        if (progressMax <= i) progressMax = progressMax * 2;
                                        p.ProgressMaxValue = progressMax;
                                        p.ProgressValue = i;
                                        p.Message = f.Name;
                                        i++;
                                    })
                                    );
                            }
                        )
                );
                while (!unit.Complete)
                {
                    await Task.Delay(1);
                }
                p.Close();
            };
            await progress.ShowDialog(Global.mainWindow);

            rootNode.Update();

            // move ownerWindow to top
            Global.mainWindow.Focus();
            Global.mainWindow.Topmost = true;
            await Task.Delay(1);
            Global.mainWindow.Topmost = false;
            Global.mainWindow.Focus();
        }

    }
}
