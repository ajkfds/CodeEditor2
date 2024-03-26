using HarfBuzzSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using AjkAvaloniaLibs.Contorls;

namespace CodeEditor2.CodeEditor
{
    public class PopupItem : ColorLabel
    {
        public PopupItem() { }

        public PopupItem(string text, Color color)
        {
            this.AppendText(text, color);
        }
    }

}
