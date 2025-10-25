using Avalonia.Controls;
using Avalonia.Controls.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.Tools
{
    public class VerticalGridConstructor
    {
        public VerticalGridConstructor() 
        {
            this.grid = new Grid() { Margin = new Avalonia.Thickness(4) };
        }

        Grid grid;
        int row = 0;
        public Grid Grid {  get { return this.grid; } }

        public void AppendText(string text)
        {
            AppendText(text, false);
        }
        public void AppendText(string text,bool bold)
        {
            TextBlock textBlock = new TextBlock();
            if(bold)
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
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            Grid.SetRow(control, row);
            grid.Children.Add(control);
            row++;
        }
        public void AppendContol(Avalonia.Controls.Control control,int? size)
        {
            if(size == null)
            {
                grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            }
            else
            {
                grid.RowDefinitions.Add(new RowDefinition((int)size, GridUnitType.Auto));
            }
            Grid.SetRow(control, row);
            grid.Children.Add(control);
            row++;
        }

    }
}
