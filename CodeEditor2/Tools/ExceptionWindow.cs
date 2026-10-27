using System;
using System.Collections.Generic;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;
using System.Threading.Tasks;

namespace CodeEditor2.Tools
{
    internal class ExceptionWindow : Window
    {
        public ExceptionWindow(Exception ex)
        {
            this.Title = "Exception";
            this.Width = 800;
            this.Height = 600;
            
            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(20),
                Spacing = 10
            };
            Content = stackPanel;

            textBlock.Text = ex.Message + "\n" + ex.StackTrace;
            textBlock.TextWrapping = TextWrapping.Wrap;
            stackPanel.Children.Add(textBlock);


            var buttonStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 10
            };

            stackPanel.Children.Add(buttonStackPanel);
            {
                var copyButton = new Button
                {
                    Content = "Copy",
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                copyButton.Click += CopyButton_Click;

                buttonStackPanel.Children.Add(copyButton);

                var okButton = new Button
                {
                    Content = "OK",
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                okButton.Click += OkButton_Click;
                buttonStackPanel.Children.Add(okButton);
            }
        }
        private TextBlock textBlock = new TextBlock();
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<保留中>")]
        private async void CopyButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
                if (clipboard != null) await clipboard.SetTextAsync(textBlock.Text);
            }
            catch
            {
            }
        }

        private void OkButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
