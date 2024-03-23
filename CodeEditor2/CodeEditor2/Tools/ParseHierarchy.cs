using Avalonia.Controls;
using Avalonia.Threading;
using CodeEditor2.CodeEditor;
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

        public async static void Run(NavigatePanel.NavigatePanelNode rootNode)
        {
            Tools.ProgressWindow progressWindow = new Tools.ProgressWindow(rootNode.Name, "Loading...", 100);
            progressWindow.Show();
            progressWindow.Topmost = true;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            parseHier(rootNode.Item,progressWindow);
            rootNode.Update();

            progressWindow.Close();
        }

        private static void parseHier(Data.Item item, ProgressWindow progressWindow)
        {
            if (item == null) return;
            Data.ITextFile textFile = item as Data.TextFile;
            if (textFile == null) return;

            textFile.ParseHierarchy((tFile) => {
                Dispatcher.UIThread.Invoke(new Action(() => { progressWindow.Message = tFile.ID; }));
            });
        }


    }
}
