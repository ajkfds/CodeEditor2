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

            int change = e.InsertionLength - e.RemovalLength;

            for (int i = 0; i < marks.Count; i++)
            {
                //     start    last
                //       +=======+

                // |---|                 a0
                // |---------|           a1
                // |-------------------| a2

                //           |---|       b0
                //           |---------| b1

                //                  |--| c0

                int start = marks[i].Offset;
                int last = marks[i].LastOffset;

                if (e.Offset <= start) // a0 | a1 | a2
                {
                    if (e.Offset + e.RemovalLength < start)
                    { // a0
                        marks[i].Offset += change;
                        marks[i].LastOffset += change;
                    }
                    else if (e.Offset + e.RemovalLength <= last)
                    { // a1
                        marks[i].LastOffset += change;
                    }
                    else
                    { // a2
                        marks[i].LastOffset += change;
                    }
                }
                else if (e.Offset <= marks[i].LastOffset) // b0 | b1
                {
                    if (e.Offset + e.RemovalLength <= last)
                    { // b0
                        marks[i].LastOffset += change;
                    }
                    else
                    { // b1
                        marks[i].LastOffset += e.Offset;
                    }
                }
                else
                { // c0
                    // none
                }
            }

        }


    }
}
