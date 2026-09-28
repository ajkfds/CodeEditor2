using AvaloniaEdit.Document;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;


namespace CodeEditor2.CodeEditor
{
    public class HIghLightHandler
    {
        public HIghLightHandler(CodeDocument codeDocument)
        {
            this.codeDocument = codeDocument;
        }
        CodeDocument codeDocument;


        private List<int> highlightStarts = new List<int>();
        private List<int> highlightLasts = new List<int>();

        public void OnTextEdit(DocumentChangeEventArgs e)
        {
            if (highlightStarts.Count == 0) return;

            int editStart = e.Offset;
            int remEnd = e.Offset + e.RemovalLength;
            int ins = e.InsertionLength;
            int change = ins - e.RemovalLength;

            for (int i = highlightStarts.Count - 1; i >= 0; i--)
            {
                int newStart = adjustOffset(highlightStarts[i], editStart, remEnd, change, ins);
                int newLast = adjustOffset(highlightLasts[i], editStart, remEnd, change, ins);

                if (newLast <= newStart)
                {
                    // highlight fully covered by the removal (a2) or collapsed -> remove
                    highlightStarts.RemoveAt(i);
                    highlightLasts.RemoveAt(i);
                }
                else
                {
                    highlightStarts[i] = newStart;
                    highlightLasts[i] = newLast;
                }
            }

            RebuildRendererResults();

            //            ReDrawHighlight();
        }

        //     start    last
        //       +=======+

        // |---|                 a0 : edit before highlight -> shift whole highlight
        // |---------|           a1 : removal eats start -> clamp start to insert point
        // |-------------------| a2 : removal covers highlight -> remove highlight

        //           |---|       b0 : edit inside highlight -> keep start, expand / shrink last
        //           |---------| b1 : removal reaches beyond highlight -> clamp last to insert point

        //                  |--| c0 : edit after highlight -> none

        private int adjustOffset(int offset, int editStart, int remEnd, int change, int ins)
        {
            if (offset >= remEnd) return offset + change;  // after removal range -> shift
            if (offset <= editStart) return offset;        // before removal range -> keep
            return editStart + ins;                        // inside removal range -> collapse to insert point
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
            // index == Count is also invalid (would throw IndexOutOfRange);
            // use >= instead of >.
            if (highlightIndex < 0 || highlightIndex >= highlightStarts.Count)
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
            if (highlightIndex < 0 || highlightIndex >= highlightStarts.Count) return;
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
            highlightStarts.Clear();
            highlightLasts.Clear();
            RebuildRendererResults();

            Controller.CodeEditor.PostRefresh();
        }

        public void AppendHighlight(int highlightStart, int highlightLast)
        {
            highlightStarts.Add(highlightStart);
            highlightLasts.Add(highlightLast);
            RebuildRendererResults();

            Controller.CodeEditor.PostRefresh();
        }

        public void ReDrawHighlight()
        {
            Controller.CodeEditor.PostRefresh();
        }

        // Rebuild the renderer's CurrentResults from the highlight lists.
        // _highlightRenderer belongs to the TextView and must only be touched on
        // the UI thread; OnTextEdit may run on a background document thread.
        private void RebuildRendererResults()
        {
            if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
            {
                rebuildRendererResults();
            }
            else
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(rebuildRendererResults);
            }
        }

        private void rebuildRendererResults()
        {
            Global.codeView._highlightRenderer.CurrentResults.Clear();
            for (int i = 0; i < highlightStarts.Count; i++)
            {
                AvaloniaEdit.Document.TextSegment segment = new AvaloniaEdit.Document.TextSegment();
                segment.StartOffset = highlightStarts[i];
                segment.Length = highlightLasts[i] - highlightStarts[i];
                Global.codeView._highlightRenderer.CurrentResults.Add(segment);
            }
        }
    }
}
