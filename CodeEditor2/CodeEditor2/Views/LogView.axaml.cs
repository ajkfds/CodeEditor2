using AjkAvaloniaLibs.Contorls;
using Avalonia.Controls;
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
            AjkAvaloniaLibs.Contorls.ListViewItem item = new AjkAvaloniaLibs.Contorls.ListViewItem(message);
            appendLog(item);
        }

        public void AppendLog(string message,Avalonia.Media.Color color)
        {
            AjkAvaloniaLibs.Contorls.ListViewItem item = new AjkAvaloniaLibs.Contorls.ListViewItem(message, color);
            appendLog(item);
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
