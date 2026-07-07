using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace CodeEditor2.LLM
{
    public class MarkdownTextItem : ChatItem
    {
        public enum MessageType
        {
            command,
            responce,
            functionCallReturn
        }

        public MarkdownTextItem(string text, MessageType messageType)
        {
            Content = grid;
            int column = 0;

            {
                switch (messageType)
                {
                    case MessageType.responce:
                        image.Source = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap("CodeEditor2/Assets/Icons/ai.svg", Avalonia.Media.Colors.YellowGreen);
                        break;
                    case MessageType.command:
                        image.Source = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap("CodeEditor2/Assets/Icons/head.svg", Avalonia.Media.Colors.SlateBlue);
                        break;
                    case MessageType.functionCallReturn:
                        image.Source = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap("CodeEditor2/Assets/Icons/hummer.svg", Avalonia.Media.Colors.Gray);
                        break;
                }
                int? leftSize = 50;
                {
                    if (leftSize == null)
                    {
                        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
                    }
                    else
                    {
                        grid.ColumnDefinitions.Add(new ColumnDefinition((int)leftSize, GridUnitType.Auto));
                    }
                    Grid.SetColumn(image, column);
                    grid.Children.Add(image);
                    column++;
                }
            }

            // Initialize hamburger menu
            MenuFlyout hamburgerFlyout = new MenuFlyout();

            hamburgerFlyout.Items.Add(new MenuItem
            {
                Header = "Branch chat from here",
                MinWidth = 200
            });
            hamburgerFlyout.Items.Add(new MenuItem
            {
                Header = "Delete this message",
                MinWidth = 200
            });
            hamburgerFlyout.Items.Add(new MenuItem
            {
                Header = "Restart from here",
                MinWidth = 200
            });

            MenuItem copyAllTextMenuItem = new MenuItem
            {
                Header = "Copy all text",
                MinWidth = 200
            };
            copyAllTextMenuItem.Click += CopyAllTextMenuItem_Click;
            hamburgerFlyout.Items.Add(copyAllTextMenuItem);

            // Set up hamburger button with "≡" mark
            hamburgerButton.Content = "≡";
            hamburgerButton.Flyout = hamburgerFlyout;

            collapseAndMenuPanel.Children.Add(spinnerImage);
            collapseAndMenuPanel.Children.Add(CollapseExpandButton);
            collapseAndMenuPanel.Children.Add(hamburgerButton);

            {
                markdown.Text = text;
                markdown.Margin = new Thickness(10, 5, 10, 5);
//                textBox.TextWrapping = Avalonia.Media.TextWrapping.Wrap;
//                textBox.InnerRightContent = collapseAndMenuPanel;

                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                Grid.SetColumn(markdown, column);
                Grid.SetColumn(collapseAndMenuPanel, column);

                grid.Children.Add(markdown);
                grid.Children.Add(collapseAndMenuPanel);
                column++;
            }


            markdown.PointerEntered += (sender, e) =>
            {
                Background = new Avalonia.Media.SolidColorBrush(new Avalonia.Media.Color(50, 0, 0, 100));
            };
            markdown.PointerExited += (sender, e) =>
            {
                Background = Avalonia.Media.Brushes.Transparent;
            };

            ContextMenu contextMenu = new ContextMenu();
            {
                {
                    MenuItem menuItem = new MenuItem()
                    {
                        Header = "Copy All"
                    };
                    menuItem.Click += (sender, e) =>
                    {
                        var top = TopLevel.GetTopLevel(this);
                        top?.Clipboard?.SetTextAsync(markdown.Text);
                    };
                    contextMenu.Items.Add(menuItem);
                }
                {
                    MenuItem menuItem = new MenuItem()
                    {
                        Header = "Copy"
                    };
                    menuItem.Click += (sender, e) =>
                    {
                        var top = TopLevel.GetTopLevel(this);
                        top?.Clipboard?.SetTextAsync(markdown.SelectedText);
                    };
                    contextMenu.Items.Add(menuItem);
                }
            }

            markdown.ContextMenu = contextMenu;
            markdown.BorderBrush = new Avalonia.Media.SolidColorBrush(new Avalonia.Media.Color(255, 10, 10, 10));

            CollapseExpandButton.Click += CollapseExpandButton_Click;
        }

        private void CopyAllTextMenuItem_Click1(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void CopyAllTextMenuItem_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            try
            {
                var top = TopLevel.GetTopLevel(this);
                top?.Clipboard?.SetTextAsync(markdown.Text);
            }
            catch (Exception ex)
            {
                CodeEditor2.Controller.AppendLog("#Exception " + ex.Message, Avalonia.Media.Colors.Red);
            }
        }

        private bool collapsed = false;
        public bool Collapsed
        {
            get
            {
                return collapsed;
            }
            set
            {
                collapsed = value;
                updateCollapse();
            }
        }
        private void CollapseExpandButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Collapsed = !Collapsed;
        }

        private void updateCollapse()
        {
            if (collapsed)
            {
                CollapseExpandButton.Content = "▲";
                markdown.Height = FontSize * 3;
            }
            else
            {
                CollapseExpandButton.Content = "▼";
                markdown.Height = double.NaN;
            }
        }

        Image image = new Image()
        {
            Width = 25,
            Height = 25
        };

        private Grid grid = new Grid()
        {
            Margin = new Thickness(0, 0, 0, 0)
        };

        private AjkAvaloniaLibs.Controls.LiveMarkdownControl markdown = new AjkAvaloniaLibs.Controls.LiveMarkdownControl()
        {
            Margin = new Thickness(10, 5, 10, 5),
            MinHeight = 30,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            FontFamily = new Avalonia.Media.FontFamily("Cascadia Mono,Consolas,Menlo,Monospace,DejaVu Sans Mono,Liberation Mono,Noto Sans Mono,Source Code Pro"),
//            FontFamily = new Avalonia.Media.FontFamily("Yu Gothic UI, Meiryo, MS Gothic, sans-serif"),
        };

        private StackPanel collapseAndMenuPanel = new StackPanel()
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            Margin = new Thickness(0, 0, 0, 0),
            Spacing = 5
        };

        public Avalonia.Controls.Primitives.ToggleButton CollapseExpandButton = new Avalonia.Controls.Primitives.ToggleButton()
        {
            Content = "▼",
            Margin = new Thickness(0, 0, 0, 0),
            Padding = new Thickness(5, 2, 5, 2),
            MinWidth = 30,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };

        private Menu hamburgerMenu = new Menu();

        private Button hamburgerButton = new Button()
        {
            Content = "≡",
            Margin = new Thickness(0, 0, 0, 0),
            Padding = new Thickness(5, 2, 5, 2),
            MinWidth = 30,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
        };

        private Image spinnerImage = new Image()
        {
            Width = 16,
            Height = 16,
            Margin = new Thickness(0, 0, 5, 0),
            IsVisible = false
        };


        private int spinnerCount = 0;
        /// <summary>
        /// Show or hide the spinner
        /// </summary>
        public async void ShowSpinner(bool show)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (show)
                {
                    spinnerImage.IsVisible = true;
                    spinnerImage.Source = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap("CodeEditor2/Assets/Icons/spinner"+ spinnerCount.ToString() + ".svg", Avalonia.Media.Colors.Cyan);
                    spinnerCount = (spinnerCount + 1) % 3; // Cycle through 8 spinner images
                }
                else
                {
                    spinnerImage.IsVisible = false;
                }
            });
        }

        public Avalonia.Media.Color TextColor
        {
            set
            {
                markdown.Foreground = new Avalonia.Media.SolidColorBrush(value);
            }
        }
        public async Task SetText(string text)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                markdown.Text = text;
            });
        }

        public string Text
        {
            get
            {
                if (markdown.Text == null) return "";
                return markdown.Text;
            }
        }
        public async Task AppendText(string text)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                markdown.Text += text;
            });
        }
    }
}
