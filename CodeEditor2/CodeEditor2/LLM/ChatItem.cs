using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.LLM
{
    public class ChatItem : ListBoxItem
    {
    }

    public class TextItem : ChatItem
    {
        private TextBox textBox = new TextBox()
        {
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Margin = new Thickness(10, 5, 10, 5),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch,
            AcceptsReturn = true,
            IsReadOnly = true,
            MinHeight = 30
        };

        public TextItem(string text, Avalonia.Media.Color textColor) : this(text)
        {
            TextColor = textColor;
        }
        public TextItem(string text)
        {
            textBox.Text = text;
            Content = textBox;

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
        }


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

    public class InputItem : ChatItem
    {
        public InputItem()
        {
            Content = StackPanel;
            TextBox.Margin = new Thickness(10, 5, 10, 5);
            TextBox.TextWrapping = Avalonia.Media.TextWrapping.Wrap;

            StackPanel.Children.Add(TextBox);
            StackPanel.Children.Add(ButtonBar);
            {
//                ButtonBar.Children.Add(TestButton);
                ButtonBar.Children.Add(ClearButton);
                ButtonBar.Children.Add(LoadButton);
                ButtonBar.Children.Add(SaveButton);
                ButtonBar.Children.Add(AbortButton);
                ButtonBar.Children.Add(SendButton);
            }


            SendButton.PropertyChanged += (sender, args) =>
            {
                if (args.Property == Button.IsFocusedProperty)
                {
                    var isFocused = (bool)args.NewValue!;
                    if (isFocused)
                    {
                        SendButton.Background = new Avalonia.Media.SolidColorBrush(new Avalonia.Media.Color(255, 0, 120, 212));
                    }
                    else
                    {
                        SendButton.Background = new Avalonia.Media.SolidColorBrush(new Avalonia.Media.Color(255, 20, 20, 20));
                    }
                }
            };

            SaveButton.Click += async (o, e) =>
            {
                SendButton.Background = new Avalonia.Media.SolidColorBrush(new Avalonia.Media.Color(255, 0, 120, 212));
                await Task.Delay(100);
                SendButton.Background = new Avalonia.Media.SolidColorBrush(new Avalonia.Media.Color(255, 20, 20, 20));
            };

            TextBox.KeyDown += (sender, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Left || e.Key == Avalonia.Input.Key.Right)
                {
                    // テキストボックス内でのカーソル移動のみを許容し、
                    // 親のListBoxへのイベント伝播（フォーカス移動）を止める
                    e.Handled = true;
                }
            };
        }

        public static readonly StyledProperty<IBrush?> SelectionBrushProperty =
            AvaloniaProperty.Register<TextBox, IBrush?>(nameof(SelectionBrush));
        public IBrush? SelectionBrush
        {
            get => GetValue(SelectionBrushProperty);
            set => SetValue(SelectionBrushProperty, value);
        }

        public StackPanel StackPanel = new StackPanel()
        {
            Orientation = Avalonia.Layout.Orientation.Vertical,
            Margin = new Thickness(10, 5, 10, 5),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };

        public TextBox TextBox = new TextBox()
        {
            Margin = new Thickness(10, 5, 10, 5),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            AcceptsReturn = true
        };

        public StackPanel ButtonBar = new StackPanel()
        {
            Background = new Avalonia.Media.SolidColorBrush(new Avalonia.Media.Color(255, 20, 20, 20)),
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Margin = new Thickness(10, 5, 10, 5),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        public Button SendButton = new Button()
        {
            Content = "Send",
            Margin = new Thickness(0, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };

        public Button SaveButton = new Button()
        {
            Content = "Save",
            Margin = new Thickness(0, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };

        public Button AbortButton = new Button()
        {
            Content = "Abort",
            Margin = new Thickness(0, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };

        public Button LoadButton = new Button()
        {
            Content = "Load",
            Margin = new Thickness(0, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };

        public Button ClearButton = new Button()
        {
            Content = "Clear",
            Margin = new Thickness(0, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };

        public Button TestButton = new Button()
        {
            Content = "Test",
            Margin = new Thickness(0, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };

    }
}
