using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Utils;
using HarfBuzzSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor.CodeComplete
{
    public class AutocompleteItem
    {
        public AutocompleteItem(string text, byte colorIndex, Color color)
        {
            this.text = text;
            this.colorIndex = colorIndex;
            Color = color;
        }

        public void Clean()
        {
        }


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
                Apply(codeDocument);
            }
        }

        public CodeDocument? codeDocument;

        public void Assign(CodeDocument codeDocument)
        {
            this.codeDocument = codeDocument;
        }


        // create item view object to avoid Avalonia Exception
        // ICompletionData re-use will cause visual tree exception
        public AutoCompleteItemView CreateItemView()
        {
            AutoCompleteItemView itemView = new AutoCompleteItemView();
            itemView.textBlock = new TextBlock();
            itemView.textBlock.Text = Text;
            itemView.textBlock.FontSize = 10;
            itemView.textBlock.Foreground = new SolidColorBrush(Color);

            itemView.Text = Text;

            itemView.Completed = new Action<TextArea, ISegment, EventArgs>((x, y, z) => { Complete(x, y, z); });

            return itemView;
        }

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

        public class AutoCompleteItemView : ICompletionData
        {

            public object? Content => textBlock;// Text;

            public string Text { get; set; }

            public object Description => "";

            public double Priority { get; } = 0;

            public IImage? Image => null;
            public TextBlock? textBlock = null;
            public Action<TextArea, ISegment, EventArgs> Completed;

            public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
            {
                if (Completed != null) Completed(textArea, completionSegment, insertionRequestEventArgs);
            }
        }

    }
}
