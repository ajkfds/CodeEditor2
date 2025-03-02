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

        public LineInformation Clone()
        {
            LineInformation lineInformation = new LineInformation();
            foreach (Color color in Colors) 
            {
                lineInformation.Colors.Add(new Color(color.Offset, color.Length, color.DrawColor));
            }
            return lineInformation;
        }
    }
}
