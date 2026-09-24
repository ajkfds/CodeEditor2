using AvaloniaEdit.Document;


//using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace CodeEditor2.CodeEditor
{
    public class MarkHandler
    {
        public MarkHandler(CodeDocument codeDocument)
        {
            this.codeDocument = codeDocument;
        }
        CodeDocument codeDocument;

        // Details : Details information Assigned to this CodeDocument
        public List<CodeDrawStyle.MarkDetail> marks = new List<CodeDrawStyle.MarkDetail>();
        public void SetMarkAt(int index, int length, byte value)
        {
            if (codeDocument.TextDocument == null) return;
            if (codeDocument.TextFile == null) return;
            if (index >= codeDocument.Length) return;

            int startLine = codeDocument.GetLineAt(index);
            int lastLine = codeDocument.GetLineAt(index + length);

            if (startLine == lastLine)
            {
                CodeDrawStyle.MarkDetail mark = createDetail(codeDocument.TextFile.DrawStyle.MarkStyle[value]);
                mark.Offset = index;
                mark.LastOffset = index + length;
                lock (marks)
                {
                    if (mark.LastOffset > mark.Offset) marks.Add(mark);
                }
                return;
            }

            { // startLine
                CodeDrawStyle.MarkDetail mark = createDetail(codeDocument.TextFile.DrawStyle.MarkStyle[value]);
                mark.Offset = index;
                mark.LastOffset = codeDocument.GetLineStartIndex(startLine) + codeDocument.GetLineLength(startLine);

                lock (marks)
                {
                    if (mark.LastOffset > mark.Offset) marks.Add(mark);
                }
            }
            for (int i = startLine + 1; i < lastLine; i++)
            {
                CodeDrawStyle.MarkDetail mark = createDetail(codeDocument.TextFile.DrawStyle.MarkStyle[value]);
                mark.Offset = codeDocument.GetLineStartIndex(i);
                mark.LastOffset = mark.Offset + codeDocument.GetLineLength(i);

                lock (marks)
                {
                    if (mark.LastOffset > mark.Offset) marks.Add(mark);
                }
            }
            { // last line
                CodeDrawStyle.MarkDetail mark = createDetail(codeDocument.TextFile.DrawStyle.MarkStyle[value]);
                mark.Offset = codeDocument.GetLineStartIndex(lastLine);
                mark.LastOffset = index + length;

                lock (marks)
                {
                    if (mark.LastOffset > mark.Offset) marks.Add(mark);
                }
            }
        }
        private CodeDrawStyle.MarkDetail createDetail(CodeDrawStyle.MarkDetail markStyle)
        {
            CodeDrawStyle.MarkDetail mark = new CodeDrawStyle.MarkDetail();
            mark.Color = markStyle.Color;
            mark.Thickness = markStyle.Thickness;
            mark.DecorationHeight = markStyle.DecorationHeight;
            mark.DecorationWidth = markStyle.DecorationWidth;
            mark.Style = markStyle.Style;
            mark.ZOrder = markStyle.ZOrder;
            return mark;
        }

        public byte GetMarkAt(int index)
        {
            //            return marks[index];
            return 0;
        }

        public virtual void SetMarkAt(int index, byte value)
        {
            SetMarkAt(index, 1, value);
        }

        public void OnTextEdit(DocumentChangeEventArgs e)
        {
            if (marks.Count == 0) return;

            int editStart = e.Offset;
            int remEnd = e.Offset + e.RemovalLength;
            int ins = e.InsertionLength;
            int change = ins - e.RemovalLength;

            lock (marks)
            {
                for (int i = marks.Count - 1; i >= 0; i--)
                {
                    int start = marks[i].Offset;
                    int last = marks[i].LastOffset;

                    int newStart = adjustOffset(start, editStart, remEnd, change, ins);
                    int newLast = adjustOffset(last, editStart, remEnd, change, ins);

                    if (newLast <= newStart)
                    {
                        // mark fully covered by the removal (a2) or collapsed -> remove
                        marks.RemoveAt(i);
                    }
                    else
                    {
                        marks[i].Offset = newStart;
                        marks[i].LastOffset = newLast;
                    }
                }
            }
        }

        //     start    last
        //       +=======+

        // |---|                 a0 : edit before mark -> shift whole mark
        // |---------|           a1 : removal eats start -> clamp start to insert point
        // |-------------------| a2 : removal covers mark -> remove mark

        //           |---|       b0 : edit inside mark -> keep start, expand / shrink last
        //           |---------| b1 : removal reaches beyond mark -> clamp last to insert point

        //                  |--| c0 : edit after mark -> none

        private static int adjustOffset(int offset, int editStart, int remEnd, int change, int ins)
        {
            if (offset >= remEnd) return offset + change;  // after removal range -> shift
            if (offset <= editStart) return offset;        // before removal range -> keep
            return editStart + ins;                        // inside removal range -> collapse to insert point
        }


    }
}
