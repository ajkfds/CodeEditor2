using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.Utils;
using HarfBuzzSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor
{
    public class AutocompleteItem : AvaloniaEdit.CodeCompletion.ICompletionData
    {
        public AutocompleteItem(string text, byte colorIndex, Avalonia.Media.Color color)
        {
            this.text = text;
            this.colorIndex = colorIndex;
            this.Color = color;
        }

        public void Clean()
        {
            textBlock = null;
        }

        public IImage Image => null;
        private TextBlock textBlock = null;

        // Use this property if you want to show a fancy UIElement in the list.
        public object Content => textBlock;// Text;

        public object Description => "";

        public double Priority { get; } = 0;

        public void Complete(AvaloniaEdit.Editing.TextArea textArea, AvaloniaEdit.Document.ISegment completionSegment,
            EventArgs insertionRequestEventArgs)
        {
            if (codeDocument == null)
            {
                textArea.Document.Replace(completionSegment, Text);
            }
            else
            {
                Apply(codeDocument);
            }
        }

        public CodeDocument codeDocument;

        public void Assign(CodeDocument codeDocument)
        {
            this.codeDocument = codeDocument;
            textBlock = new TextBlock();
            textBlock.Text = Text;
            textBlock.FontSize = 10;
            textBlock.Foreground = new SolidColorBrush(Color);
        }

        //private ajkControls.Primitive.IconImage icon = null;
        //private ajkControls.Primitive.IconImage.ColorStyle iconColorStyle;

        private string text;
        public string Text { get { return text; } }

        public byte ColorIndex
        {
            get
            {
                return colorIndex;
            }
        }

        private byte colorIndex;
        private Color Color;

        public virtual void Apply(CodeDocument codeDocument)
        {
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
                codeDocument.CaretIndex = index + Text.Length;

                //if(Global.codeView.CodeDocument == codeDocument)
                //{
                //    Controller.CodeEditor.SetSelection(index, index + Text.Length - 1);
                //}
            }
            else
            {
                // delete after last .
                codeDocument.Replace(headIndex, length, ColorIndex, Text);
                codeDocument.CaretIndex = headIndex + Text.Length;
                //if (Global.codeView.CodeDocument == codeDocument)
                //{
                //    Controller.CodeEditor.SetSelection(headIndex, headIndex + Text.Length - 1);
                //}
            }
            Global.codeView.codeViewPopupMenu.AfterAutoCompleteHandled();
        }

    }
}
