using HarfBuzzSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using AjkAvaloniaLibs.Controls;

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
