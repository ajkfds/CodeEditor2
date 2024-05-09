using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using CodeEditor2.CodeEditor;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeEditor2.CodeEditor
{
    public class CodeDrawStyle
    {
        public CodeDrawStyle()
        {
            colors = new Color[16]
            {
                Color.FromRgb(212,212,212), // 0
                Color.FromRgb(  0,  0,255), // 1
                Color.FromRgb(  0,255,  0), // 2
                Color.FromRgb(  0,255,255), // 3
                Color.FromRgb(255,  0,  0), // 4
                Color.FromRgb(255,  0,255), // 5
                Color.FromRgb(255,255,  0), // 6
                Color.FromRgb(255,255,255), // 7

                Color.FromRgb(100,100,100), // 8
                Color.FromRgb(  0,  0,100), // 9
                Color.FromRgb(  0,100,  0), // 10
                Color.FromRgb(  0,100,100), // 11
                Color.FromRgb(100,  0,  0), // 12
                Color.FromRgb(100,  0,100), // 13
                Color.FromRgb(100,100,  0), // 14
                Color.FromRgb( 50, 50, 50)  // 15
            };


            markStyle = new MarkInfo[]
            {
                // 0
                new MarkInfo
                {
                    Color = Color.FromRgb(212, 212, 212),
                    Style = MarkInfo.MarkStyleEnum.WaveLine,
                },
                // 1
                new MarkInfo
                {
                    Color = Color.FromRgb(0, 0, 255),
                    Style = MarkInfo.MarkStyleEnum.WaveLine,
                },
                // 2
                new MarkInfo
                {
                    Color = Color.FromRgb(0, 255, 0),
                    Style = MarkInfo.MarkStyleEnum.WaveLine,
                },
                // 3
                new MarkInfo
                {
                    Color = Color.FromRgb(0, 255, 0),
                    Style = MarkInfo.MarkStyleEnum.WaveLine,
                },
                // 4
                new MarkInfo
                {
                    Color = Color.FromRgb(0, 255, 0),
                    Style = MarkInfo.MarkStyleEnum.WaveLine,
                },
                // 5
                new MarkInfo
                {
                    Color = Color.FromRgb(0, 255, 0),
                    Style = MarkInfo.MarkStyleEnum.WaveLine,
                },
                // 6
                new MarkInfo
                {
                    Color = Color.FromRgb(0, 255, 0),
                    Style = MarkInfo.MarkStyleEnum.WaveLine,
                },
                // 7
                new MarkInfo
                {
                    Color = Color.FromRgb(0, 255, 0),
                    Style = MarkInfo.MarkStyleEnum.WaveLine,
                },
            };
        }

        public class MarkInfo
        {
            public Color Color;
            public double DecorationWidth = 4;
            public double DecorationHeight = 1;
            public double Thickness = 1;
            public MarkStyleEnum Style;
            public int Offset;
            public int LastOffset;
            public enum MarkStyleEnum
            {
                UnderLine,
                WaveLine,
                DotLine,
                DashLine
            }
        }

        protected Color[] colors;

        public virtual Color[] ColorPallet
        {
            get
            {
                return colors;
            }
        }

        protected MarkInfo[] markStyle;
        public virtual MarkInfo[] MarkStyle
        {
            get
            {
                return markStyle;
            }
        }
    }
}