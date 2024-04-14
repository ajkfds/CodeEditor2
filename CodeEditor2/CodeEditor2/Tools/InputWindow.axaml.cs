using Avalonia.Controls;

namespace CodeEditor2.Tools
{
    public partial class InputWindow : Window
    {
        public InputWindow(string titile,string caption)
        {
            InitializeComponent();
            
            TextBlock0.Text = caption;

            CancelButton.Click += CancelButton_Click;
            OKButton.Click += OKButton_Click;
        }

        public bool Cancel = true;
        public string InputText = "";

        private void OKButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Cancel = false;
            InputText = TextBox1.Text;
            Close();
        }

        private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }

    }
}
