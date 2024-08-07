using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor.TextDecollation
{
    public class LineInformation
    {
        public List<Color> Colors = new List<Color>();
        //public List<Effect> Effects = new List<Effect>();

        public class Color
        {
            public Color(int offset, int length, Avalonia.Media.Color color)
            {
                Offset = offset;
                Length = length;
                DrawColor = color;
            }
            public int Offset;
            public int Length;
            public Avalonia.Media.Color DrawColor;
        }

        //public class Effect
        //{
        //    public Effect(int offset, int length, Avalonia.Media.Color color, CodeDocumentColorTransformer.MarkStyleEnum markStyle)
        //    {
        //        this.Offset = offset;
        //        this.Length = length;
        //        this.DrawColor = color;
        //        this.MarkStyle = markStyle;
        //    }
        //    public int Offset;
        //    public int Length;
        //    public Avalonia.Media.Color DrawColor;
        //    public CodeDocumentColorTransformer.MarkStyleEnum MarkStyle;
        //}
    }
}
