using Avalonia.Controls;
using Avalonia.Controls.Documents;

namespace CodeEditor2.MessageView
{
    public class MessageNode
    {
        public MessageNode()
        {
            Text = "";
        }

        public MessageNode(string text)
        {
            Text = text;
        }

        public TextBlock textBlock = new TextBlock();
        public ListBoxItem ListBoxItem()
        {
            ListBoxItem item = new ListBoxItem();
            item.Margin = new Avalonia.Thickness(0);
            item.Padding = new Avalonia.Thickness(0);
            item.Content = textBlock;
            item.Tapped += Item_Tapped;

            textBlock.Margin = new Avalonia.Thickness(0, 0, 0, 0);
            textBlock.Padding = new Avalonia.Thickness(0);

            item.PropertyChanged += Item_PropertyChanged;

            return item;
        }

        private Avalonia.Controls.Image? _iconImage;
        private void Item_PropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
        {
            Update();
        }

        private void Item_Tapped(object? sender, Avalonia.Input.TappedEventArgs e)
        {
            this.OnSelected();
        }

        private string text = "";
        public string Text
        {
            set
            {
                text = value;
                Update();
            }
            get
            {
                return text;
            }
        }

        public virtual void Update()
        {
            if (textBlock.Inlines == null) return;
            textBlock.Inlines.Clear();
            Avalonia.Media.IImage? iimage = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2/Assets/Icons/exclamation_triangle.svg",
                    Avalonia.Media.Color.FromArgb(100, 255, 150, 150)
                    );
            Avalonia.Controls.Image image = new Avalonia.Controls.Image();
            image.Source = iimage;
            image.Width = textBlock.FontSize;
            image.Height = textBlock.FontSize;
            image.Margin = new Avalonia.Thickness(0, 0, 4, 0);
            image.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            _iconImage = image;

            {
                InlineUIContainer uiContainer = new InlineUIContainer();
                uiContainer.BaselineAlignment = Avalonia.Media.BaselineAlignment.Center;// .Baseline;
                uiContainer.Child = image;
                textBlock.Inlines.Add(uiContainer);
            }

            Avalonia.Controls.Documents.Run run = new Avalonia.Controls.Documents.Run(text);
            textBlock.Inlines.Add(run);
        }
        public virtual void OnSelected()
        {
        }
    }
}
