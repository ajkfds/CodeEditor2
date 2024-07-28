using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
namespace CodeEditor2.CodeEditor
{
    public class MarkerRenderer : IBackgroundRenderer
    {
        public MarkerRenderer()
        {
        }
        private TextSegmentCollection<TextSegment> marks { get; } = new TextSegmentCollection<TextSegment>();

        public void ClearMark()
        {
            marks.Clear();
        }

        // copy mark information
        public void SetMarks(List<CodeDrawStyle.MarkDetail> marks)
        {
            this.marks.Clear();
            lock (marks)
            {
                foreach (var mark in marks)
                {
                    this.marks.Add(Mark.CloneFrom(mark));
                }
            }
        }

        public void OnTextEdit(DocumentChangeEventArgs e)
        {
            if (marks == null) return;

            int change = e.InsertionLength - e.RemovalLength;
            List<TextSegment> removeTarget = new List<TextSegment>();

            lock (marks)
            {
                foreach (var mark in marks)
                {
                    //     start    last
                    //       +=======+

                    // |---|                 a0
                    // |---------|           a1
                    // |-------------------| a2

                    //           |---|       b0
                    //           |---------| b1

                    //                  |--| c0

                    int start = mark.StartOffset;
                    int last = mark.EndOffset;

                    if (e.Offset <= start) // a0 | a1 | a2
                    {
                        if (e.Offset + e.RemovalLength <= start)
                        { // a0
                            mark.StartOffset += change;
                            //                            mark.EndOffset += change;
                        }
                        else if (e.Offset + e.RemovalLength <= last)
                        { // a1
                            mark.StartOffset = e.Offset;
                            if (mark.EndOffset + change <= mark.StartOffset)
                            {
                                removeTarget.Add(mark);
                            }
                            else
                            {
                                if (e.Offset + change <= mark.StartOffset)
                                {
                                    removeTarget.Add(mark);
                                }
                                else
                                {
                                    mark.EndOffset = e.Offset + change;
                                }
                            }
                        }
                        else
                        { // a2
                            if (mark.EndOffset + change > mark.StartOffset) mark.EndOffset += change;
                            else removeTarget.Add(mark);
                        }
                    }
                    else if (e.Offset <= mark.EndOffset + 1) // b0 | b1
                    {
                        if (e.Offset + e.RemovalLength <= last + 1)
                        { // b0
                            mark.EndOffset += change;
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

                foreach (var removeMark in removeTarget)
                {
                    marks.Remove(removeMark);
                }

            }
        }
        public class Mark : AvaloniaEdit.Document.TextSegment
        {
            public Color Color;
            public double DecorationWidth = 4;
            public double DecorationHeight = 1;
            public double Thickness = 1;
            public CodeDrawStyle.MarkDetail.MarkStyleEnum Style;

            public static Mark CloneFrom(CodeDrawStyle.MarkDetail markInfo)
            {
                Mark mark = new Mark();
                mark.Color = markInfo.Color;
                mark.DecorationWidth = markInfo.DecorationWidth;
                mark.DecorationHeight = markInfo.DecorationHeight;
                mark.Style = markInfo.Style;
                mark.StartOffset = markInfo.Offset;
                mark.EndOffset = markInfo.LastOffset;
                mark.Thickness = markInfo.Thickness;
                return mark;
            }
        }
        public KnownLayer Layer => KnownLayer.Background;



        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (textView == null)
                throw new ArgumentNullException(nameof(textView));
            if (drawingContext == null)
                throw new ArgumentNullException(nameof(drawingContext));

            if (marks == null || !textView.VisualLinesValid)
                return;

            var visualLines = textView.VisualLines;
            if (visualLines.Count == 0)
                return;

            var viewStart = visualLines.First().FirstDocumentLine.Offset;
            var viewEnd = visualLines.Last().LastDocumentLine.EndOffset;

            foreach (var result in marks.FindOverlappingSegments(viewStart, viewEnd - viewStart))
            {
                MarkerRenderer.Mark? mark = result as MarkerRenderer.Mark;
                if (mark == null) continue;

                var geoBuilder = new BackGroundUnderlineGeometryBuilder();
                geoBuilder.AddSegment(textView, mark);
                var geometry = geoBuilder.CreateGeometry();
                SolidColorBrush brush = new SolidColorBrush(mark.Color);
                Pen pen;
                switch (mark.Style)
                {
                    case CodeDrawStyle.MarkDetail.MarkStyleEnum.DotLine:
                        pen = new Pen(brush, mark.Thickness,DashStyle.Dash);
                        break;
                    default:
                        pen = new Pen(brush, mark.Thickness);
                        break;
                }
                if (geometry != null)
                {
                    drawingContext.DrawGeometry(brush, pen, geometry);
                }
            }
        }


    }
}
