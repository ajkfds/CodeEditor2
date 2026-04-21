using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
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
            TextBox.Margin = new Thickness(10, 5, 10, 5);
            TextBox.TextWrapping = Avalonia.Media.TextWrapping.Wrap;

            StackPanel.Children.Add(TextBox);

            HorizontalGridConstructor hgrid = new HorizontalGridConstructor();
            hgrid.AppendContol(ModelSelector, null);
            hgrid.AppendContolFill(ButtonBar);

            StackPanel.Children.Add(hgrid.Grid);

            // Add model selector and button bar to the bottom panel

            {
                //                ButtonBar.Children.Add(TestButton);
                ButtonBar.Children.Add(ClearButton);
                ButtonBar.Children.Add(LoadButton);
                ButtonBar.Children.Add(SaveButton);
                ButtonBar.Children.Add(AbortButton);
                ButtonBar.Children.Add(SendButton);
            }

            // Set ItemsSource after ModelItems is initialized
            ModelSelector.ItemsSource = ModelItems;

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

        public StackPanel BottomPanel = new StackPanel()
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Margin = new Thickness(10, 5, 10, 5),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
        };

        /// <summary>
        /// Collection of available models for the model selector
        /// </summary>
        public ObservableCollection<ModelItem> ModelItems { get; } = new ObservableCollection<ModelItem>();

        public ComboBox ModelSelector = new ComboBox()
        {
            MinWidth = 200,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            PlaceholderText = "Select Model"
        };

        /// <summary>
        /// Event raised when the selected model changes
        /// </summary>
        public event EventHandler<ModelItem?>? ModelChanged;

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

    /// <summary>
    /// Represents an LLM model item for the model selector
    /// </summary>
    public class ModelItem
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public object? Tag { get; set; }

        public override string ToString() => Name;
    }
}
