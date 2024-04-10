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

        public static List<ParsedDocument> unlockdPaeedsedDocument = new List<ParsedDocument>();

        public static async Task Run(NavigatePanel.NavigatePanelNode rootNode)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            System.Diagnostics.Debug.Print("parse hier sw " + sw.ElapsedMilliseconds.ToString());

            Dispatcher.UIThread.Post(() => {
                Global.ProgressWindow.Title = "Reparse " + rootNode.Text;
                Global.ProgressWindow.ProgressMaxValue = 100;
                Global.ProgressWindow.ShowDialog(Global.mainWindow);
            });

            {
                Global.LockParse();

                await parseHier(rootNode.Item);

                Global.ReleaseParseLock();
            }
            System.Diagnostics.Debug.Print("parse hier sw2 " + sw.ElapsedMilliseconds.ToString());
            rootNode.Update();

            System.Diagnostics.Debug.Print("parse hier sw3 " + sw.ElapsedMilliseconds.ToString());


            Dispatcher.UIThread.Post(() => {
                Global.ProgressWindow.Hide();
            });
        }

        private static async Task parseHier(Data.Item item)
        {
            if (item == null) return;
            Data.ITextFile textFile = item as Data.TextFile;
            if (textFile == null) return;

            textFile.ParseHierarchy((tFile) => {
                textFile.ParseHierarchy((tFile) =>
                {
                    Dispatcher.UIThread.Invoke(new Action(() => { Global.ProgressWindow.Message = tFile.ID; }));
                });
            });
        }


    }
}
