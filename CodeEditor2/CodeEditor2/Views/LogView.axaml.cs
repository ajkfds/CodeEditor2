using AjkAvaloniaLibs.Controls;
using Avalonia.Controls;
using Avalonia.Threading;
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
            AjkAvaloniaLibs.Controls.ListViewItem item = new AjkAvaloniaLibs.Controls.ListViewItem(message);
            Dispatcher.UIThread.Post(() => appendLog(item));
            
        }

        public void AppendLog(string message,Avalonia.Media.Color color)
        {
            AjkAvaloniaLibs.Controls.ListViewItem item = new AjkAvaloniaLibs.Controls.ListViewItem(message, color);
            Dispatcher.UIThread.Post(() => appendLog(item));
        }

        private void appendLog(ListViewItem item)
        {
            ListView.Items.Add(item);
            if (ListView.Items.Count > maxLogs)
            {
                ListView.Items.RemoveAt(0);
            }
            ListView.Scroll(ListView.Items.Last());
        }
    }
}
