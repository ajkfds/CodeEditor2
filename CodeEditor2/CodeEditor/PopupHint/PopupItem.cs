using AjkAvaloniaLibs.Controls;
using Avalonia.Media;

namespace CodeEditor2.CodeEditor.PopupHint
{
    public class PopupItem : ColorLabel
    {
        public PopupItem() { }

        public PopupItem(string text, Color color)
        {
            AppendText(text, color);
        }
    }

}
