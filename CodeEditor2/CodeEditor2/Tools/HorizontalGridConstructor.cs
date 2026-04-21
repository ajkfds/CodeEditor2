using Avalonia.Controls;
using Avalonia.Controls.Documents;

namespace CodeEditor2.Tools
{
    public class HorizontalGridConstructor
    {
        public HorizontalGridConstructor()
        {
            this.grid = new Grid() { Margin = new Avalonia.Thickness(4) };
        }

        Grid grid;
        int column = 0;
        public Grid Grid { get { return this.grid; } }

        public void AppendText(string text)
        {
            AppendText(text, false);
        }
        public void AppendText(string text, bool bold)
        {
            TextBlock textBlock = new TextBlock();
            if (bold)
            {
                Bold boldText = new Bold();
                boldText.Inlines.Add(new Run(text));
                textBlock.Inlines?.Add(boldText);
            }
            else
            {
                textBlock.Text = text;
            }
            AppendContol(textBlock, null);
        }

        public void AppendContolFill(Avalonia.Controls.Control control)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            Grid.SetColumn(control, column);
            grid.Children.Add(control);
            column++;
        }
        public void AppendContol(Avalonia.Controls.Control control, int? size)
        {
            if (size == null)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            }
            else
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition((int)size, GridUnitType.Auto));
            }
            Grid.SetColumn(control, column);
            grid.Children.Add(control);
            column++;
        }

    }
}
