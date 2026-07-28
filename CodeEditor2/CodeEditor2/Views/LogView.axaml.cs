using AjkAvaloniaLibs.Controls;
using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace CodeEditor2.Views
{
    public partial class LogView : UserControl
    {
        public LogView()
        {
            InitializeComponent();

            Global.logView = this;
            if (Global.ReducedRendering)
            {
                Avalonia.Media.RenderOptions.SetTextRenderingMode(ListView, Avalonia.Media.TextRenderingMode.Alias);
                var customFont = new Avalonia.Media.FontFamily(Global.ReducedRenderingCodeFontFamily);
                ListView.FontFamily = customFont;
            }
            AttachedToVisualTree += (s, e) =>
            {
                if (Global.ReducedRendering)
                {
                    double scale = this.VisualRoot?.RenderScaling ?? 1.0;
                    ListView.FontSize = Math.Ceiling((float)Global.ReducedRenderingFontSize / scale);
                }
            };
            this.AddHandler(PointerWheelChangedEvent, (o, i) =>
            {
                if (i.KeyModifiers != KeyModifiers.Control) return;
                if (i.Delta.Y > 0)
                {
                    ListView.FontSize = (int)ListView.FontSize + 1;
                }
                else
                {
                    ListView.FontSize = ListView.FontSize > 1 ? (int)ListView.FontSize - 1 : 1;
                }
            }, RoutingStrategies.Bubble, true);
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

        public void AppendLog(string message, Avalonia.Media.Color color)
        {
            List<string> messages = message.Replace("\r", "").Split('\n', System.StringSplitOptions.RemoveEmptyEntries).ToList();

            foreach (string m in messages)
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
