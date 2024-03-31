using Avalonia.Controls;
using Avalonia.Threading;
using System;

namespace CodeEditor2.Views
{
    public partial class SmartMenuWindow : Window
    {
        public SmartMenuWindow()
        {
            InitializeComponent();

            Closing += SmartMenuWindow_Closing;

            Loaded += SmartMenuWindow_Loaded;
            TextBox0.LostFocus += TextBox0_LostFocus;
            TextBox0.KeyDown += TextBox0_KeyDown;

            TextBox0.SelectAll();
            TextBox0.Focus();

        }

        private void SmartMenuWindow_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // set focus must be launched after loaded evet
            TextBox0.Focus();
        }

        private void TextBox0_KeyDown(object? sender, Avalonia.Input.KeyEventArgs e)
        {
            if(e.Key == Avalonia.Input.Key.Return)
            {
                select();
            }
        }

        private void TextBox0_LostFocus(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            select();
        }

        private void select()
        {
            Hide();
        }
        private void cancel()
        {
            Hide();
        }

        private void SmartMenuWindow_Closing(object? sender, WindowClosingEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }
    }
}
