using Avalonia.Controls;
using static CodeEditor2.Controller;
using System;
using Avalonia.Threading;
using System.Reflection.Emit;
using CodeEditor2.Data;
using CodeEditor2.CodeEditor;

namespace CodeEditor2.Tools
{
    public partial class ParseHierarchyForm : Window
    {
        public ParseHierarchyForm(NavigatePanel.NavigatePanelNode rootNode)
        {
            InitializeComponent();
            //            Text = projectNode.Project.Name;
            this.rootNode = rootNode;
//            this.Icon = ajkControls.Global.Icon;
            this.ShowInTaskbar = false;

            Loaded += ParseHierarchyForm_Loaded;
            Closing += ParseHierarchyForm_Closing;
            Opened += ParseHierarchyForm_Opened;
        }


        private void ParseHierarchyForm_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            timer.Interval = new TimeSpan(0, 0, 0, 10);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            ProgressBar0.InvalidateVisual();
        }

        DispatcherTimer timer = new DispatcherTimer();

        private NavigatePanel.NavigatePanelNode rootNode = null;
        private volatile bool close = false;


        private void ParseHierarchyForm_Closing(object? sender, WindowClosingEventArgs e)
        {
            if (!close) e.Cancel = true;
            rootNode = null;
        }

        System.Threading.Thread thread = null;
        private void ParseHierarchyForm_Opened(object? sender, EventArgs e)
        {
            if (thread != null) return;
            thread = new System.Threading.Thread(() => { worker(); });
            thread.Name = "ParseHierarchyForm";
            thread.Start();
//            timer.Enabled = true;
        }


        private void worker()
        {
            parseHier(rootNode.Item);
            rootNode.Update();

            close = true;
            Dispatcher.UIThread.Invoke(new Action(() => { Close(); }));
        }

        private void parseHier(Data.Item item)
        {
            if (item == null) return;
            Data.ITextFile textFile = item as Data.TextFile;
            if (textFile == null) return;

            textFile.ParseHierarchy((tFile) => {
                Dispatcher.UIThread.Invoke(new Action(() => { Message.Text = tFile.ID; }));
            });
            textFile.ParsedDocument.UnlockDocumentThread();

        }
    }
}
