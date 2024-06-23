using Avalonia.Controls;

namespace CodeEditor2.Tools
{
    public partial class YesNoWindow : Window
    {
        public YesNoWindow()
        {
            InitializeComponent();

            initialize("","");
        }

        public YesNoWindow(string title, string caption)
        {
            InitializeComponent();

            initialize(title, caption);
        }
        private void initialize(string title, string caption)
        {
            this.Title = title;

            TextBlock0.Text = caption;

            NoButton.Click += NoButton_Click;
            YesButton.Click += YesButton_Click;

            this.Activated += InputWindow_Activated;
        }

        private void InputWindow_Activated(object? sender, System.EventArgs e)
        {
            YesButton.Focus();
        }


        public bool Yes = false;
        public string InputText = "";

        private void YesButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Yes = true;
            Close();
        }

        private void NoButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }

    }
}
