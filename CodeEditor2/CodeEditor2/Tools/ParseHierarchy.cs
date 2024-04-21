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

            Global.ProgressWindow.Title = "Reparse " + rootNode.Text;
            Global.ProgressWindow.ProgressMaxValue = 100;
            Global.ProgressWindow.ShowDialog(Global.mainWindow);

            {
                Global.StopParse = true;
                Global.LockParse();
                
                int i = 0;
                ParseHierarchyUnit unit = new ParseHierarchyUnit("ParseHier"+rootNode.Item.Name);
                unit.Run(rootNode.Item,
                        (
                            (f) =>
                            {
                                Dispatcher.UIThread.Post(
                                    new Action(() =>
                                    {
                                        Global.ProgressWindow.ProgressValue = i;
                                        Global.ProgressWindow.Message = f.Name;
                                        i++;
                                    })
                                    );
                            }
                        )
                );
                while (!unit.Complete)
                {
                    await Task.Delay(10);
                }

                Global.ReleaseParseLock();
                Global.StopParse = false;
            }
            rootNode.Update();

            Global.ProgressWindow.Hide();
        }

    }
}
