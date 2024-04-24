using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor
{
    public class LineInfomation
    {
        public List<Color> Colors = new List<Color>();
        public List<Effect> Effects = new List<Effect>();

        public class Color
        {
            public Color(int offeset,int length,Avalonia.Media.Color color)
            {
                this.Offset = offeset;
                this.Length = length;
                this.DrawColor = color;
            }
            public int Offset;
            public int Length;
            public Avalonia.Media.Color DrawColor;
        }

        public class Effect
        {
            public Effect(int offset, int length, Avalonia.Media.Color color, CodeDocumentColorTransformer.MarkStyleEnum markStyle)
            {
                this.Offset = offset;
                this.Length = length;
                this.DrawColor = color;
                this.MarkStyle = markStyle;
            }
            public int Offset;
            public int Length;
            public Avalonia.Media.Color DrawColor;
            public CodeDocumentColorTransformer.MarkStyleEnum MarkStyle;
        }
    }
}
