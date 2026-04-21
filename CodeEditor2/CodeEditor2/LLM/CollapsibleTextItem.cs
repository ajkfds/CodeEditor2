using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace CodeEditor2.LLM
{
    public class CollapsibleTextItem : ChatItem
    {
        public enum MessageType
        {
            command,
            responce,
            functionCallReturn
        }

        public CollapsibleTextItem(string text, MessageType messageType)
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

            collapseAndMenuPanel.Children.Add(CollapseExpandButton);
            collapseAndMenuPanel.Children.Add(hamburgerButton);

            {
                textBox.Text = text;
                textBox.Margin = new Thickness(10, 5, 10, 5);
                textBox.TextWrapping = Avalonia.Media.TextWrapping.Wrap;
                textBox.InnerRightContent = collapseAndMenuPanel;

                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                Grid.SetColumn(textBox, column);
                grid.Children.Add(textBox);
                column++;
            }


            textBox.PointerEntered += (sender, e) =>
            {
                Background = new Avalonia.Media.SolidColorBrush(new Avalonia.Media.Color(50, 0, 0, 100));
            };
            textBox.PointerExited += (sender, e) =>
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
                        top?.Clipboard?.SetTextAsync(textBox.Text);
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
                        top?.Clipboard?.SetTextAsync(textBox.SelectedText);
                    };
                    contextMenu.Items.Add(menuItem);
                }
            }

            textBox.ContextMenu = contextMenu;
            textBox.BorderBrush = new Avalonia.Media.SolidColorBrush(new Avalonia.Media.Color(255, 10, 10, 10));

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
                top?.Clipboard?.SetTextAsync(textBox.Text);
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
                textBox.Height = FontSize * 3;
            }
            else
            {
                CollapseExpandButton.Content = "▼";
                textBox.Height = double.NaN;
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

        private TextBox textBox = new TextBox()
        {
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Thickness(10, 5, 10, 5),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            AcceptsReturn = true,
            IsReadOnly = true,
            MinHeight = 30
        };


        private StackPanel collapseAndMenuPanel = new StackPanel()
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 0),
            Spacing = 5
        };

        public Avalonia.Controls.Primitives.ToggleButton CollapseExpandButton = new Avalonia.Controls.Primitives.ToggleButton()
        {
            Content = "▼",
            Margin = new Thickness(0, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };

        private Menu hamburgerMenu = new Menu();

        private Button hamburgerButton = new Button()
        {
            Content = "≡",
            Margin = new Thickness(5, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            MinWidth = 30,
            Padding = new Thickness(5, 2, 5, 2)
        };

        public Avalonia.Media.Color TextColor
        {
            set
            {
                textBox.Foreground = new Avalonia.Media.SolidColorBrush(value);
            }
        }
        public async Task SetText(string text)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                textBox.Text = text;
            });
        }

        public string Text
        {
            get
            {
                if (textBox.Text == null) return "";
                return textBox.Text;
            }
        }
        public async Task AppendText(string text)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                textBox.Text += text;
            });
        }
    }
}
