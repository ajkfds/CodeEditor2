using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
namespace CodeEditor2.CodeEditor.TextDecollation
{
    // mark renderer for AvaloniaEdit
    // attached to TextEditor and handle mark rendering

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
            lock (this.marks)
            {
                // Diff check: if the incoming mark list is identical to the current
                // one (same count, same order, same content), skip the rebuild of
                // the TextSegmentCollection entirely. This avoids re-allocating all
                // Mark segments on every refresh when nothing actually changed.
                if (EqualsCurrentMarks(marks))
                {
                    return;
                }

                this.marks.Clear();
                foreach (var mark in marks)
                {
                    if (mark.LastOffset < mark.Offset) continue;
                    this.marks.Add(Mark.CloneFrom(mark));
                }
            }
        }

        // Compare the given MarkDetail list against the currently stored marks.
        // Both lists are expected to be in the same order (the source list
        // CodeDocument.Marks.marks keeps a stable order when unchanged).
        private bool EqualsCurrentMarks(List<CodeDrawStyle.MarkDetail> markDetails)
        {
            if (markDetails.Count != currentMarkCount) return false;

            int i = 0;
            foreach (var segment in this.marks)
            {
                Mark? mark = segment as Mark;
                if (mark == null) return false;
                CodeDrawStyle.MarkDetail detail = markDetails[i];
                if (mark.StartOffset != detail.Offset ||
                    mark.EndOffset != detail.LastOffset ||
                    mark.Color != detail.Color ||
                    mark.Style != detail.Style ||
                    mark.Thickness != detail.Thickness ||
                    mark.ZOrder != detail.ZOrder)
                {
                    return false;
                }
                i++;
            }
            return true;
        }

        private int currentMarkCount
        {
            get
            {
                int count = 0;
                foreach (var segment in this.marks) count++;
                return count;
            }
        }

        // update renderer mark information on editing text
        public void OnTextEdit(DocumentChangeEventArgs e)
        {
            if (marks == null) return;

            int change = e.InsertionLength - e.RemovalLength;
            List<TextSegment> removeTarget = new List<TextSegment>();

            lock (marks)
            {

                // marks sorted with offset order.
                // mark offset change will change order of IEnumerator and
                // foreach loop can failed to update all mark.
                // marks must be stored once and update it
                List<TextSegment> markList = new List<TextSegment>();
                foreach (var mark in marks)
                {
                    if (mark == null) continue;
                    markList.Add(mark);
                }

                foreach (var mark in markList)
                {
                    updateMark(e.Offset, e.InsertionLength, e.RemovalLength, mark, removeTarget);
                }

                foreach (var removeMark in removeTarget)
                {
                    marks.Remove(removeMark);
                }
            }
        }

        public void updateMark(int offset, int insertionLength, int removalLength, TextSegment color, List<TextSegment> removeTarget)
        {
            if (offset < color.StartOffset)
            {
                if (offset + removalLength < color.StartOffset)
                {
                    //                      color
                    //               start       last
                    //                 v           v
                    // .   .   .   .   .   .   .   .   .   .   .   .
                    //                 =============
                    //     |------->                 removal area
                    // remove
                    color.StartOffset -= removalLength;
                    // insert
                    color.StartOffset += insertionLength;
                }
                else if (offset + removalLength < color.StartOffset + color.Length)
                {
                    //                      color
                    //               start       last
                    //                 v           v
                    // .   .   .   .   .   .   .   .   .   .   .   .
                    //                 =============
                    //     |------------------>      removalarea
                    // remove
                    //                 <------> duplicate
                    int duplicate = offset + removalLength - color.StartOffset;

                    color.StartOffset = offset;
                    color.Length -= duplicate;
                    // insert
                    color.StartOffset += insertionLength;
                }
                else
                {
                    //                      color
                    //               start       last
                    //                 v           v
                    // .   .   .   .   .   .   .   .   .   .   .   .
                    //                 =============
                    //     |-------------------------------> // removalarea
                    removeTarget.Add(color);
                }
            }
            else if (offset == color.StartOffset)
            {
                if (offset + removalLength < color.StartOffset + color.Length)
                {
                    //                      color
                    //               start       last
                    //                 v           v
                    // .   .   .   .   .   .   .   .   .   .   .   .
                    //                 =============
                    //                 |------->     removal area
                    // remove
                    color.Length -= removalLength;
                    // insert
                    color.StartOffset += insertionLength;
                }
                else
                {
                    //                      color
                    //               start       last
                    //                 v           v
                    // .   .   .   .   .   .   .   .   .   .   .   .
                    //                 =============
                    //                 |-------------------> removal area
                    removeTarget.Add(color);
                }
            }
            else if (offset <= color.StartOffset + color.Length)
            {
                if (offset + removalLength < color.StartOffset + color.Length)
                {
                    //                      color
                    //               start       last
                    //                 v           v
                    // .   .   .   .   .   .   .   .   .   .   .   .
                    //                 =============
                    //                     |--->     removal area
                    color.Length = offset - color.StartOffset;

                    // remove
                    //color.Length -= removalLength;
                    // insert
                    //color.Length += insertionLength;
                }
                else
                {
                    //                      color
                    //               start       last
                    //                 v           v
                    // .   .   .   .   .   .   .   .   .   .   .   .
                    //                 =============
                    //                     |---------------> removal area
                    // remove
                    color.Length = offset - color.StartOffset;
                }
            }
            else
            {
                //                      color
                //               start       last
                //                 v           v
                // .   .   .   .   .   .   .   .   .   .   .   .
                //                 =============
                //                                 |--->
            }
        }
        public class Mark : TextSegment
        {
            public Color Color;
            public double DecorationWidth = 4;
            public double DecorationHeight = 1;
            public double Thickness = 1;
            public int ZOrder = 0;
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
                mark.ZOrder = markInfo.ZOrder;
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


            List<(SolidColorBrush,Pen,Geometry,int)> geometories = new List<(SolidColorBrush, Pen, Geometry,int)>();

            foreach (var result in marks.FindOverlappingSegments(viewStart, viewEnd - viewStart))
            {
                Mark? mark = result as Mark;
                if (mark == null) continue;

                var geoBuilder = new BackGroundUnderlineGeometryBuilder();
                geoBuilder.AddSegment(textView, mark);
                var geometry = geoBuilder.CreateGeometry();
                SolidColorBrush brush = GetBrush(mark.Color);
                Pen pen = GetPen(mark);
                geometories.Add((brush,pen, geometry,mark.ZOrder));
                //if (geometry != null)
                //{
                //    drawingContext.DrawGeometry(brush, pen, geometry);
                //}
            }
            geometories.Sort((a, b) => (a.Item4).CompareTo(b.Item4));


            foreach (var geo in geometories)
            {
                drawingContext.DrawGeometry(geo.Item1, geo.Item2, geo.Item3);
            }
        }

        // Cached brushes, keyed by color. Draw is invoked for every visible mark
        // on every render pass; allocating a new SolidColorBrush per mark caused
        // heavy allocation pressure.
        private static readonly Dictionary<Color, SolidColorBrush> brushCache = new Dictionary<Color, SolidColorBrush>();

        private static SolidColorBrush GetBrush(Color color)
        {
            lock (brushCache)
            {
                SolidColorBrush? brush;
                if (!brushCache.TryGetValue(color, out brush))
                {
                    brush = new SolidColorBrush(color);
                    brushCache[color] = brush;
                }
                return brush;
            }
        }

        // Cached pens, keyed by (color, thickness, style).
        private static readonly Dictionary<(Color, double, CodeDrawStyle.MarkDetail.MarkStyleEnum), Pen> penCache = new Dictionary<(Color, double, CodeDrawStyle.MarkDetail.MarkStyleEnum), Pen>();

        private static Pen GetPen(Mark mark)
        {
            var key = (mark.Color, mark.Thickness, mark.Style);
            Pen? pen;
            lock (penCache)
            {
                if (!penCache.TryGetValue(key, out pen))
                {
                    SolidColorBrush brush = GetBrush(mark.Color);
                    switch (mark.Style)
                    {
                        case CodeDrawStyle.MarkDetail.MarkStyleEnum.DotLine:
                            pen = new Pen(brush, mark.Thickness, DashStyle.Dash);
                            break;
                        default:
                            pen = new Pen(brush, mark.Thickness);
                            break;
                    }
                    penCache[key] = pen;
                }
            }
            return pen;
        }


    }
}
