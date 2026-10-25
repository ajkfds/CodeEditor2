using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using System;
using System.Diagnostics;
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD101:Avoid unsupported async delegates", Justification = "<保留中>")]
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
            copyAllTextMenuItem.Click += (sender, e) =>
            {
                _ = CopyAllTextAsync();
            };
            hamburgerFlyout.Items.Add(copyAllTextMenuItem);

            // Set up hamburger button with "≡" mark
            hamburgerButton.Content = "≡";
            hamburgerButton.Flyout = hamburgerFlyout;

            collapseAndMenuPanel.Children.Add(CollapseExpandButton);
            collapseAndMenuPanel.Children.Add(hamburgerButton);

            {
                markdown.Text = text;
                markdown.Margin = new Thickness(10, 5, 10, 5);
//                textBox.TextWrapping = Avalonia.Media.TextWrapping.Wrap;
//                textBox.InnerRightContent = collapseAndMenuPanel;

                // spinner is overlaid at the bottom-right of the message text
                Grid textGrid = new Grid();
                // Row 0: the message body (markdown) with spinner / menu overlays.
                // Row 1: optional informational footer line (e.g. token usage).
                // The footer is kept OUTSIDE of the markdown text so it is never
                // part of the message content itself.
                textGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                textGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

                textGrid.Children.Add(markdown);
                Grid.SetColumn(markdown, 0);
                Grid.SetRow(markdown, 0);

                Grid.SetColumn(spinnerImage, 0);
                Grid.SetRow(spinnerImage, 0);
                spinnerImage.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right;
                spinnerImage.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom;
                spinnerImage.Margin = new Thickness(0, 0, 15, 8);
                textGrid.Children.Add(spinnerImage);

                Grid.SetColumn(collapseAndMenuPanel, 0);
                Grid.SetRow(collapseAndMenuPanel, 0);
                textGrid.Children.Add(collapseAndMenuPanel);

                Grid.SetColumn(infoTextBlock, 0);
                Grid.SetRow(infoTextBlock, 1);
                textGrid.Children.Add(infoTextBlock);

                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
                Grid.SetColumn(textGrid, column);

                grid.Children.Add(textGrid);
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
                // Copy text: copies the current selection if any, otherwise the
                // whole message. The previous separate "Copy All" / "Copy"
                // entries are unified into this single item.
                MenuItem copyTextMenuItem = new MenuItem()
                {
                    Header = "Copy text"
                };
                copyTextMenuItem.Click += async (sender, e) =>
                {
                    try
                    {
                        var top = TopLevel.GetTopLevel(this);
                        if (top?.Clipboard is { } clipboard)
                        {
                            string text = markdown.SelectedText;
                            if (string.IsNullOrEmpty(text)) text = markdown.Text;
                            await clipboard.SetTextAsync(text);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Print($"clipboard error: {ex.Message}");
                    }
                };
                contextMenu.Items.Add(copyTextMenuItem);
            }
            {
                // Context menu item to toggle vertical size collapse / expand
                MenuItem collapseExpandMenuItem = new MenuItem()
                {
                    Header = collapsed ? "Expand" : "Collapse"
                };
                collapseExpandMenuItem.Click += (sender, e) =>
                {
                    Collapsed = !Collapsed;
                };
                contextMenu.Items.Add(collapseExpandMenuItem);
                // Keep the menu label in sync with the current collapse state
                contextMenu.Opening += (sender, e) =>
                {
                    collapseExpandMenuItem.Header = collapsed ? "Expand" : "Collapse";
                };
            }

            // Open the context menu from a tunneled ContextRequested handler on
            // this item as a whole (not only the inner markdown control). The
            // inner LiveMarkdown renderer handles right-clicks internally, so a
            // context menu set on it never opens; opening the menu here and
            // marking the event handled guarantees it shows on right-click
            // anywhere over the message.
            //
            // The ContextMenu property is intentionally NOT set (that would add
            // a second default opener and could show the menu twice).
            this.AddHandler(
                ContextRequestedEvent,
                (sender, e) =>
                {
                    contextMenu.Open(this);
                    e.Handled = true;
                },
                Avalonia.Interactivity.RoutingStrategies.Tunnel);

            markdown.BorderBrush = new Avalonia.Media.SolidColorBrush(new Avalonia.Media.Color(255, 10, 10, 10));

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

        /// <summary>
        /// Informational footer line shown below the message body (e.g. token
        /// usage). Kept outside the markdown text so it is not part of the
        /// message content. Hidden while empty.
        /// </summary>
        private TextBlock infoTextBlock = new TextBlock()
        {
            Margin = new Thickness(10, 0, 10, 5),
            FontSize = 11,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Gray),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            IsVisible = false
        };

        /// <summary>
        /// Optional informational line displayed at the end of the message, as a
        /// footer outside of the message body. The value is not included in
        /// <see cref="Text"/> and therefore never sent to the LLM.
        /// Setting null or an empty string hides the line.
        /// </summary>
        public string? InfoMessage
        {
            get => infoTextBlock.Text;
            set
            {
                infoTextBlock.Text = value;
                infoTextBlock.IsVisible = !string.IsNullOrEmpty(value);
            }
        }


        private int spinnerCount = 0;
        /// <summary>
        /// Show or hide the spinner
        /// </summary>
        public async void ShowSpinner(bool show)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ShowSpinnerDirect(show);
            });
        }

        /// <summary>
        /// Synchronously show or hide the spinner. Must be called on the UI thread.
        /// Unlike <see cref="ShowSpinner"/>, this does not enqueue work on the
        /// dispatcher, so the visual update is committed before the caller
        /// returns. This matters for ordering: a deferred spinner update on a
        /// tool-result item could otherwise be flushed after the next LLM turn's
        /// items had already been inserted, making the spinner appear out of
        /// order.
        /// </summary>
        public void ShowSpinnerDirect(bool show)
        {
            if (show)
            {
                spinnerImage.IsVisible = true;
                spinnerImage.Source = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap("CodeEditor2/Assets/Icons/spinner" + spinnerCount.ToString() + ".svg", Avalonia.Media.Colors.Cyan);
                spinnerCount = (spinnerCount + 1) % 3; // Cycle through spinner images
            }
            else
            {
                spinnerImage.IsVisible = false;
            }
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
                IsShowingPlaceholder = false;
            });
        }

        /// <summary>
        /// Synchronously set the message body. Must be called on the UI thread.
        /// Unlike <see cref="SetText"/>, this does not enqueue work on the
        /// dispatcher, so the new text is committed before the caller returns.
        /// Used when the accumulated body must be cleared before the next chat
        /// items are inserted (see ChatControl.SplitPendingResponse).
        /// </summary>
        public void SetTextDirect(string text)
        {
            markdown.Text = text;
            IsShowingPlaceholder = false;
        }

        /// <summary>
        /// True while the body still displays the progress placeholder text
        /// (e.g. "waiting ... 0.5s") written by the progress timer. The
        /// placeholder is not real message content: it must not be treated as a
        /// part of the response when the response is split by a tool call, and
        /// it is cleared as soon as real content is appended.
        /// </summary>
        public bool IsShowingPlaceholder { get; private set; }

        /// <summary>
        /// Measures how long this chat item has been displayed. The progress
        /// placeholder shows this value ("waiting ... 1.2s") so every message
        /// reports the wait time from the moment IT was inserted.
        /// <para>
        /// Measuring per item (instead of one stopwatch per LLM call) is what
        /// makes the response that follows a native (API) tool call show its own
        /// elapsed time: the tool call moves the stream into a new item
        /// (see ChatControl.SplitStreamedResponseBeforeToolCall), which is
        /// inserted later than the first response of the same turn and must
        /// therefore restart the count from its own display time.
        /// </para>
        /// </summary>
        private readonly Stopwatch displayStopwatch = Stopwatch.StartNew();

        /// <summary>
        /// Refresh the progress placeholder with the elapsed time since this item
        /// was inserted ("waiting ... 1.2s").
        /// <para>
        /// Does nothing once real message content has arrived, so a late timer
        /// tick can never overwrite streamed text. Safe to call from any thread
        /// (marshals to the UI thread).
        /// </para>
        /// </summary>
        /// <param name="prefix">Text placed before the elapsed time.</param>
        public async Task ShowProgressPlaceholder(string prefix)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ShowProgressPlaceholderDirect(prefix);
            });
        }

        /// <summary>
        /// <see cref="ShowProgressPlaceholder"/> without dispatching. Must be
        /// called on the UI thread; used when the placeholder has to be committed
        /// before the next chat items are inserted (a response item inserted by a
        /// tool-call split shows its wait time right away instead of on the next
        /// timer tick).
        /// </summary>
        public void ShowProgressPlaceholderDirect(string prefix)
        {
            // Real content already arrived: keep it.
            if (!IsShowingPlaceholder && Text.Length > 0) return;

            markdown.Text = prefix + (displayStopwatch.ElapsedMilliseconds / 1000f).ToString("F1") + "s";
            IsShowingPlaceholder = true;
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
                appendDirect(text);
            });
        }

        /// <summary>
        /// Synchronously append text to the message body. Must be called on the
        /// UI thread. Unlike <see cref="AppendText"/>, this does not enqueue work
        /// on the dispatcher, so the text is committed before the caller
        /// returns. Used when the append must be ordered before subsequent chat
        /// items are inserted (e.g. the tool result must appear above the next
        /// LLM response).
        /// </summary>
        public void AppendTextDirect(string text)
        {
            appendDirect(text);
        }

        /// <summary>
        /// Append streamed text to the body, discarding the progress placeholder
        /// first so "waiting ... 1.2s" never becomes a part of the message.
        /// Must run on the UI thread.
        /// </summary>
        private void appendDirect(string text)
        {
            // The placeholder reported the wait time of this item while nothing
            // had arrived yet. The first real chunk replaces it entirely.
            string body = IsShowingPlaceholder ? "" : markdown.Text ?? "";
            markdown.Text = body + text;
            IsShowingPlaceholder = false;
        }

        private async Task CopyAllTextAsync()
        {
            try
            {
                var top = TopLevel.GetTopLevel(this);
                if (top?.Clipboard is { } clipboard)
                {
                    await clipboard.SetTextAsync(markdown.Text);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"clipboard error: {ex.Message}");
            }
        }

        private async Task CopySelectedTextAsync()
        {
            try
            {
                var top = TopLevel.GetTopLevel(this);
                if (top?.Clipboard is { } clipboard)
                {
                    await clipboard.SetTextAsync(markdown.SelectedText);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"clipboard error: {ex.Message}");
            }
        }
    }
}
