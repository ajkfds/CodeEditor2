using AjkAvaloniaLibs.Controls;
using Avalonia.Controls;
using Avalonia.Threading;
using CodeEditor2.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeEditor2.Views
{
    public partial class LogView : UserControl
    {
        public LogView()
        {
            InitializeComponent();

            Global.logView = this;
        }

        const int maxLogs = 100;

        public void AppendLog(string message)
        {
            List<string> messages = message.Replace("\r", "").Split('\n', System.StringSplitOptions.RemoveEmptyEntries).ToList();
            foreach (string m in messages)
            {
                AjkAvaloniaLibs.Controls.ListViewItem item = new AjkAvaloniaLibs.Controls.ListViewItem(m);
                Dispatcher.UIThread.Post(() => appendLog(item));
            }
            Dispatcher.UIThread.Post(() =>
            {
                ListView.Scroll(ListView.Items.Last());
            }, DispatcherPriority.Background);
        }

        public void AppendLog(string message,Avalonia.Media.Color color)
        {
            List<string> messages = message.Replace("\r", "").Split('\n',System.StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach(string m in messages)
            {
                AjkAvaloniaLibs.Controls.ListViewItem item = new AjkAvaloniaLibs.Controls.ListViewItem(m, color);
                Dispatcher.UIThread.Post(() => appendLog(item));
            }
            Dispatcher.UIThread.Post(() =>
            {
                ListView.Scroll(ListView.Items.Last());
            }, DispatcherPriority.Background);
        }

        public WeakReference<ListViewItem> AppendLogAndGetLastItem(string message, Avalonia.Media.Color color)
        {
            List<string> messages = message.Replace("\r", "").Split('\n', System.StringSplitOptions.RemoveEmptyEntries).ToList();
            ListViewItem? lastItem = null;

            foreach (string m in messages)
            {
                lastItem = new AjkAvaloniaLibs.Controls.ListViewItem(m, color);
                Dispatcher.UIThread.Post(() => appendLog(lastItem));
            }
            Dispatcher.UIThread.Post(() =>
            {
                ListView.Scroll(ListView.Items.Last());
            }, DispatcherPriority.Background);

            if (lastItem == null) throw new Exception();
            return new WeakReference<ListViewItem>(lastItem);
        }
        private void appendLog(ListViewItem item)
        {
            ListView.Items.Add(item);
            if (ListView.Items.Count > maxLogs)
            {
                ListView.Items.RemoveAt(0);
            }
        }
    }
}
