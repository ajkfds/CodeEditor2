using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using DynamicData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
                    }else{
                        grid.ColumnDefinitions.Add(new ColumnDefinition((int)leftSize, GridUnitType.Auto));
                    }
                    Grid.SetColumn(image, column);
                    grid.Children.Add(image);
                    column++;
                }
            }

            {
                textBox.Text = text;
                textBox.Margin = new Thickness(10, 5, 10, 5);
                textBox.TextWrapping = Avalonia.Media.TextWrapping.Wrap;
                textBox.InnerRightContent = CollapseExpandButton;

                grid.ColumnDefinitions.Add(new ColumnDefinition( GridLength.Star));
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


        public Button CollapseExpandButton = new Button()
        {
            Content = "v",
            Margin = new Thickness(0, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
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
