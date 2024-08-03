using Avalonia.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor
{
    public class PopupMenuItem : TextBlock
    {
        public PopupMenuItem(string text)
        {
            Text = text;
            Height = 14;
            FontSize = 8;
            Padding = new Avalonia.Thickness(0, 0, 2, 2);
            Margin = new Avalonia.Thickness(0, 0, 0, 0);
        }
        public Action Selected;
        public virtual void OnSelected()
        {
            if (Selected != null) Selected();
        }
    }
}
