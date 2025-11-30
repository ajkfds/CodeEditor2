using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using CodeEditor2.CodeEditor.TextDecollation;
using CodeEditor2.NavigatePanel;
using DynamicData;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReactiveUI;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Metadata;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static CodeEditor2.CodeEditor.TextDecollation.MarkerRenderer;
using static System.Net.Mime.MediaTypeNames;

namespace CodeEditor2.CodeEditor
{
    public class AnchorHandler
    {
        public AnchorHandler(CodeDocument codeDocument)
        {
            this.codeDocument = codeDocument;
        }
        CodeDocument codeDocument;


        private List<int> highlightStarts = new List<int>();
        private List<int> highlightLasts = new List<int>();

        public void OnTextEdit(DocumentChangeEventArgs e)
        {
            if (highlightStarts.Count == 0) return;

            int change = e.InsertionLength - e.RemovalLength;

            for (int i = 0; i < highlightStarts.Count; i++)
            {
                //     start    last
                //       +=======+

                // |---|                 a0
                // |---------|           a1
                // |-------------------| a2

                //           |---|       b0
                //           |---------| b1

                //                  |--| c0

                int start = highlightStarts[i];
                int last = highlightLasts[i];

                if (e.Offset <= start) // a0 | a1 | a2
                {
                    if (e.Offset + e.RemovalLength < start)
                    { // a0
                        highlightStarts[i] += change;
                        highlightLasts[i] += change;
                    }
                    else if (e.Offset + e.RemovalLength <= last)
                    { // a1
                        highlightStarts[i] = e.Offset;
                        highlightLasts[i] = e.Offset + change;
                    }
                    else
                    { // a2
                        highlightLasts[i] += change;
                    }
                }
                else if (e.Offset <= highlightLasts[i] + 1) // b0 | b1
                {
                    if (e.Offset + e.RemovalLength <= last + 1)
                    { // b0
                        highlightLasts[i] += change;
                    }
                    else
                    { // b1
                        // none
                    }
                }
                else
                { // c0
                    // none
                }
            }

            Global.codeView._highlightRenderer.CurrentResults.Clear();
            for (int i = 0; i < highlightStarts.Count; i++)
            {
                if (highlightStarts[i] > highlightLasts[i]) continue;
                AvaloniaEdit.Document.TextSegment segment = new AvaloniaEdit.Document.TextSegment();
                segment.StartOffset = highlightStarts[i];
                segment.Length = highlightLasts[i] - highlightStarts[i] + 1;
                Global.codeView._highlightRenderer.CurrentResults.Add(segment);
            }

            //            ReDrawHighlight();
        }
        public void MoveToNextHighlight(out bool moved)
        {
            moved = false;
            int i = GetHighlightIndex(codeDocument.CaretIndex);
            if (i == -1) return;
            i++;
            if (i >= highlightStarts.Count) return;

            SelectHighlight(i);
            moved = true;
        }

        public void GetHighlightPosition(int highlightIndex, out int highlightStart, out int highlightLast)
        {
            if (highlightIndex > highlightStarts.Count)
            {
                highlightStart = -1;
                highlightLast = -1;
                return;
            }
            highlightStart = highlightStarts[highlightIndex];
            highlightLast = highlightLasts[highlightIndex];
        }

        public void SelectHighlight(int highlightIndex)
        {
            CodeDocument document = codeDocument;
            CodeEditor2.Controller.CodeEditor.SetCaretPosition(highlightStarts[highlightIndex]);
            CodeEditor2.Controller.CodeEditor.SetSelection(highlightStarts[highlightIndex], highlightLasts[highlightIndex]);
        }

        public int GetHighlightIndex(int index)
        {
            if (highlightStarts.Count == 0) return -1;
            for (int i = 0; i < highlightStarts.Count; i++)
            {
                if (highlightStarts[i] <= index && index <= highlightLasts[i] + 1) return i;
            }
            return -1;
        }

        public void ClearHighlight()
        {
            CodeDocument document = codeDocument;

            if (highlightStarts.Count == 0) return;
            Global.codeView._highlightRenderer.CurrentResults.Clear();
            //for (int i = 0; i < highlightStarts.Count; i++)
            //{
            //    document.RemoveMarkAt(highlightStarts[i], highlighLasts[i] - highlightStarts[i] + 1, 7);
            //    //for (int index = highlightStarts[i]; index <= highlighLasts[i]; index++)
            //    //{
            //    //    if(index < document.Length) document.RemoveMarkAt(index, 7);
            //    //}
            //}
            highlightStarts.Clear();
            highlightLasts.Clear();

            Controller.CodeEditor.Refresh();
        }

        public void AppendHighlight(int highlightStart, int highlightLast)
        {
            AvaloniaEdit.Document.TextSegment segment = new AvaloniaEdit.Document.TextSegment();
            segment.StartOffset = highlightStart;
            segment.Length = highlightLast - highlightStart + 1;
            Global.codeView._highlightRenderer.CurrentResults.Add(segment);
            highlightStarts.Add(highlightStart);
            highlightLasts.Add(highlightLast);

            Controller.CodeEditor.Refresh();
        }

        public void ReDrawHighlight()
        {
            Controller.CodeEditor.Refresh();
        }
    }
}
