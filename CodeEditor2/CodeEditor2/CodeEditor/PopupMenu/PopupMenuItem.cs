using System;

namespace CodeEditor2.CodeEditor.PopupMenu
{
    public class PopupMenuItem : AjkAvaloniaLibs.Controls.ListViewItem
    {
        public PopupMenuItem(string text) : base()
        {
            Text = text;
            //Height = 14;
            //FontSize = 8;
            //Padding = new Avalonia.Thickness(0, 0, 2, 2);
            //Margin = new Avalonia.Thickness(0, 0, 0, 0);
        }
        public Action? Selected;
        public virtual void OnSelected()
        {
            if (Selected != null) Selected();
        }
    }
}
