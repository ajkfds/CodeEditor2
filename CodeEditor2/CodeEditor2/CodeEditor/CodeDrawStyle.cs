using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;

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
        }


        protected Color[] colors;

        public virtual Color[] ColorPallet
        {
            get
            {
                return colors;
            }
        }

        public virtual Color[] MarkColor
        {
            get
            {
                return new Color[8]
                    {
                        Color.FromRgb(212,212,212), // 0
                        Color.FromRgb(  0,  0,255), // 1
                        Color.FromRgb(  0,255,  0), // 2
                        Color.FromRgb(  0,255,255), // 3
                        Color.FromRgb(255,  0,  0), // 4
                        Color.FromRgb(255,  0,255), // 5
                        Color.FromRgb(255,255,  0), // 6
                        Color.FromRgb(255,255,255), // 7
                    };
            }
        }

        public virtual CodeDocumentColorTransformer.MarkStyleEnum[] MarkStyle
        {
            get
            {
                return new CodeDocumentColorTransformer.MarkStyleEnum[8]
                    {
                        CodeDocumentColorTransformer.MarkStyleEnum.DashedLine0,    // 0
                        CodeDocumentColorTransformer.MarkStyleEnum.ThickUnderLine,
                        CodeDocumentColorTransformer.MarkStyleEnum.ThickUnderLine,
                        CodeDocumentColorTransformer.MarkStyleEnum.ThickUnderLine,
                        CodeDocumentColorTransformer.MarkStyleEnum.ThickUnderLine,
                        CodeDocumentColorTransformer.MarkStyleEnum.ThickUnderLine,
                        CodeDocumentColorTransformer.MarkStyleEnum.ThickUnderLine,
                        CodeDocumentColorTransformer.MarkStyleEnum.ThickUnderLine              // 7 for selection highlight
                    };
            }
        }
    }
}