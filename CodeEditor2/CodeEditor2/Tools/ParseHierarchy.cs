using Avalonia.Controls;
using Avalonia.Threading;
using CodeEditor2.CodeEditor;
using CodeEditor2.Data;
using CodeEditor2.NavigatePanel;
using Microsoft.CodeAnalysis.CSharp;
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
    public class ParseHierarchy
    {
        // All Parse executed in UI Thread

        public static List<ParsedDocument> unlockdPaeedsedDocument = new List<ParsedDocument>();

        public static async Task Run(NavigatePanel.NavigatePanelNode rootNode)
        {

            Dispatcher.UIThread.Post(() => {
                Global.ProgressWindow.Title = "Reparse " + rootNode.Text;
                Global.ProgressWindow.ProgressMaxValue = 100;
                Global.ProgressWindow.ShowDialog(Global.mainWindow);
            });

            {
                Global.LockParse();

                parseHier(rootNode.Item);

                Global.ReleaseParseLock();
            }
            rootNode.Update();



            Dispatcher.UIThread.Post(() => {
                Global.ProgressWindow.Hide();
            });
        }

        private static void parseHier(Data.Item item)
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
