using Avalonia.Controls;
using Avalonia.Controls.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

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

        ListBoxItem listboxItem;
        public TextBlock textBlock = new TextBlock();
        public ListBoxItem ListBoxItem()
        {
            ListBoxItem item = new ListBoxItem();
            textBlock.Height = 14;
            textBlock.MinHeight = 14;
            textBlock.FontSize = 10;
            textBlock.Margin = new Avalonia.Thickness(0, 0, 0, 0);

            item.Content = textBlock;
            item.Tapped += Item_Tapped;
            item.Height = 14;
            return item;
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
            image.Width = 12;
            image.Height = 12;
            image.Margin = new Avalonia.Thickness(0, 0, 4, 0);
            image.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center;
            {
                InlineUIContainer uiContainer = new InlineUIContainer();
                uiContainer.BaselineAlignment = Avalonia.Media.BaselineAlignment.Baseline;
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
