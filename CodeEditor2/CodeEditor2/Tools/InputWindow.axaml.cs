using Avalonia.Controls;
using System;

namespace CodeEditor2.Tools
{
    public partial class InputWindow : Window
    {
        public InputWindow()
        {
            InitializeComponent();

            // for designer
            initialize("", "", "");
        }

        public InputWindow(string title, string caption)
        {
            InitializeComponent();

            initialize(title, caption, "");
        }

        public InputWindow(string title, string caption, string defaultText)
        {
            InitializeComponent();

            initialize(title, caption, defaultText);
        }

        private void initialize(string title, string caption,string defaultText)
        {
            this.Title = title;

            TextBlock0.Text = caption;
            TextBox1.SelectAll();

            TextBox1.KeyDown += TextBox1_KeyDown;
            CancelButton.Click += CancelButton_Click;
            OKButton.Click += OKButton_Click;

            this.Activated += InputWindow_Activated;
        }

        private void InputWindow_Activated(object? sender, System.EventArgs e)
        {
            TextBox1.Focus();
        }

        private void TextBox1_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if (e.Key == Avalonia.Input.Key.Enter)
            {
                e.Handled = true;
                accept();
            }
        }

        public bool Cancel = true;
        public string InputText = "";

        private void OKButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            accept();
        }
        private void accept()
        {
            Cancel = false;
            string? text = TextBox1.Text;
            if (text == null)
            {
                InputText = "";
            }
            else
            {
                InputText = text;
            }
            Close();
        }


        private void CancelButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Close();
        }

    }
}
