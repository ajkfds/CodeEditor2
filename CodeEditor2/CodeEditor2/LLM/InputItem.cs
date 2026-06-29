using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit;
using CodeEditor2.Tools;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CodeEditor2.LLM
{
    public class InputItem : ChatItem
    {
        public InputItem()
        {
            Content = StackPanel;
            TextEditor.Margin = new Thickness(10, 5, 10, 5);

            StackPanel.Children.Add(TextEditor);

            HorizontalGridConstructor hgrid = new HorizontalGridConstructor();
            hgrid.AppendContol(ModelSelector, null);
            hgrid.AppendContol(ModeSelector, null);
            hgrid.AppendContolFill(ButtonBar);

            StackPanel.Children.Add(hgrid.Grid);

            {
                ButtonBar.Children.Add(ClearButton);
                ButtonBar.Children.Add(LoadButton);
                ButtonBar.Children.Add(SaveButton);
                ButtonBar.Children.Add(AbortButton);
                ButtonBar.Children.Add(SendButton);
            }

            ModelSelector.ItemsSource = ModelItems;
            ModeSelector.ItemsSource = ModeItems;
            ModeSelector.SelectedIndex = 0;

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

            TextEditor.KeyDown += (sender, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Left || e.Key == Avalonia.Input.Key.Right)
                {
                    e.Handled = true;
                }
            };

            // Ctrl+SpaceでAutoCompleteを起動
            TextEditor.KeyDown += TextEditor_KeyDown;
        }

        /// <summary>
        /// Event raised when Ctrl+Space is pressed to trigger auto-complete
        /// </summary>
        public event EventHandler? AutoCompleteRequested;

        /// <summary>
        /// Handle key down events for Ctrl+Space auto-complete
        /// </summary>
        private void TextEditor_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space && e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                e.Handled = true;
                AutoCompleteRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        public static readonly StyledProperty<IBrush?> SelectionBrushProperty =
            AvaloniaProperty.Register<TextEditor, IBrush?>(nameof(SelectionBrush));
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

        public TextEditor TextEditor = new TextEditor()
        {
            Margin = new Thickness(10, 5, 10, 5),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            FontFamily = new Avalonia.Media.FontFamily("Yu Gothic UI, Meiryo, MS Gothic, sans-serif"),
            FontSize = 12,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        public StackPanel BottomPanel = new StackPanel()
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Margin = new Thickness(10, 5, 10, 5),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };

        public ObservableCollection<ModelItem> ModelItems { get; } = new ObservableCollection<ModelItem>();

        public ComboBox ModelSelector = new ComboBox()
        {
            MinWidth = 200,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            PlaceholderText = "Select Model"
        };

        public ObservableCollection<ModeItem> ModeItems { get; } = new ObservableCollection<ModeItem>
        {
            new ModeItem { Id = "plan", Name = "Plan" },
            new ModeItem { Id = "implement", Name = "Implement" }
        };

        public ComboBox ModeSelector = new ComboBox()
        {
            MinWidth = 120,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
        };

        public event EventHandler<ModelItem?>? ModelChanged;
        public event EventHandler<ModeItem?>? ModeChanged;

        public StackPanel ButtonBar = new StackPanel()
        {
            Background = new Avalonia.Media.SolidColorBrush(new Avalonia.Media.Color(255, 20, 20, 20)),
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        public Button SendButton = new Button()
        {
            Content = "Send",
            Margin = new Thickness(0, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        public Button SaveButton = new Button()
        {
            Content = "Save",
            Margin = new Thickness(0, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        public Button AbortButton = new Button()
        {
            Content = "Abort",
            Margin = new Thickness(0, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        public Button LoadButton = new Button()
        {
            Content = "Load",
            Margin = new Thickness(0, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        public Button ClearButton = new Button()
        {
            Content = "Clear",
            Margin = new Thickness(0, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };

        public Button TestButton = new Button()
        {
            Content = "Test",
            Margin = new Thickness(0, 0, 0, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
        };
    }

    public class ModelItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public object? Tag { get; set; }
        public override string ToString() => Name;
    }

    public class ModeItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public override string ToString() => Name;
    }
}
