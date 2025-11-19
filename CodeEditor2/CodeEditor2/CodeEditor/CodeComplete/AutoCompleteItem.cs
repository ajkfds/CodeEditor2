using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Utils;
using CodeEditor2.CodeEditor.PopupMenu;
using HarfBuzzSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static AjkAvaloniaLibs.Libs.Icons;

namespace CodeEditor2.CodeEditor.CodeComplete
{
    public class AutocompleteItem : PopupMenu.ToolItem
    {
        public AutocompleteItem(string text, byte colorIndex, Color color) :base(text)
        {
            this.Text = text;
            this.colorIndex = colorIndex;
            Color = color;
        }
        public AutocompleteItem(string text, byte colorIndex, Color color,string svgPath) : base(text)
        {
            this.Text = text;
            this.colorIndex = colorIndex;
            Color = color;
            this.svgPath = svgPath; 
        }

        public void Clean()
        {
        }

        private string? svgPath = null;

        // Use this property if you want to show a fancy UIElement in the list.

        public void Complete(TextArea textArea, ISegment completionSegment,
            EventArgs insertionRequestEventArgs)
        {
            if (codeDocument == null)
            {
                textArea.Document.Replace(completionSegment, Text);
            }
            else
            {
                Apply();
            }
        }

        public CodeDocument? codeDocument;

        public void Assign(CodeDocument codeDocument)
        {
            this.codeDocument = codeDocument;
        }

        public byte ColorIndex
        {
            get
            {
                return colorIndex;
            }
        }

        private byte colorIndex;
        private Color Color;

        public override void Apply()
        {
            if (codeDocument == null) return;
            int prevIndex = codeDocument.CaretIndex;
            if (codeDocument.GetLineStartIndex(codeDocument.GetLineAt(prevIndex)) != prevIndex && prevIndex != 0)
            {
                prevIndex--;
            }
            int headIndex, length;
            codeDocument.GetWord(prevIndex, out headIndex, out length);
            if (codeDocument.GetCharAt(prevIndex) == '.')
            {
                int index = codeDocument.CaretIndex;
                codeDocument.Replace(index, 0, ColorIndex, Text);
                Controller.CodeEditor.SetCaretPosition(index + Text.Length);

                //if(Global.codeView.CodeDocument == codeDocument)
                //{
                //    Controller.CodeEditor.SetSelection(index, index + Text.Length - 1);
                //}
            }
            else
            {
                // delete after last .
                codeDocument.Replace(headIndex, length, ColorIndex, Text);
                Controller.CodeEditor.SetCaretPosition(headIndex + Text.Length);
            }
            Global.codeView.codeViewPopupMenu.AfterAutoCompleteHandled();
        }

        public override PopupMenuItem CreatePopupMenuItem()
        {
            PopupMenuItem popupMenuItem = new PopupMenuItem(Text);
            if(svgPath != null)
            {
                popupMenuItem.Image = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    svgPath,
                    Color
                    );
            }

            popupMenuItem.ForeColor = Color;
            popupMenuItem.Selected += new Action(() => { Apply(); });
            return popupMenuItem;
        }

    }
}
