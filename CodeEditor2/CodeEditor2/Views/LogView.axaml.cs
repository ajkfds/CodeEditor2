using Avalonia.Controls;

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
            ListView.Items.Add(item);
            if (ListView.Items.Count > maxLogs)
            {
                ListView.Items.RemoveAt(0);
            }
        }

        public void AppendLog(string message,Avalonia.Media.Color color)
        {
            AjkAvaloniaLibs.Contorls.ListViewItem item = new AjkAvaloniaLibs.Contorls.ListViewItem(message, color);
            ListView.Items.Add(item);
            if (ListView.Items.Count > maxLogs)
            {
                ListView.Items.RemoveAt(0);
            }
        }
    }
}
