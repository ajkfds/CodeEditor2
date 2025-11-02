using Avalonia.Controls;
using Avalonia.Controls.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.Tools
{
    public class VerticalStackPanelConstructor
    {
        public VerticalStackPanelConstructor()
        {
            this.stackPanel = new StackPanel() { Orientation = Avalonia.Layout.Orientation.Vertical };
        }

        StackPanel stackPanel;
        int row = 0;
        public StackPanel StackPanel { get { return this.stackPanel; } }

        public enum Style
        {
            Normal,
            Bold
        }
        public void AppendText(string text)
        {
            AppendText(text, null, Style.Normal);
        }
        public void AppendText(string text, Style style)
        {
            AppendText(text, null, style);
        }
        public void AppendText(string text, double? size)
        {
            AppendText(text, null, Style.Normal);
        }
        public void AppendText(string text, double? size,Style style)
        {
            TextBlock textBlock = new TextBlock();
            if (style == Style.Bold)
            {
                Bold boldText = new Bold();
                boldText.Inlines.Add(new Run(text));
                if(size !=null) boldText.FontSize = (double)size;
                textBlock.Inlines?.Add(boldText);
            }
            else
            {
                textBlock.Text = text;
                if (size != null) textBlock.FontSize = (double)size;
            }
            AppendContol(textBlock, null);
        }

        public void AppendContol(Avalonia.Controls.Control control, int? size)
        {
            stackPanel.Children.Add(control);
            row++;
        }
    }
}
