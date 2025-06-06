﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
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
            this.marks.Clear();
            lock (marks)
            {
                foreach (var mark in marks)
                {
                    this.marks.Add(Mark.CloneFrom(mark));
                }
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
                    updateMark(e.Offset , e.InsertionLength, e.RemovalLength, mark, removeTarget);
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
                Mark? mark = result as Mark;
                if (mark == null) continue;

                var geoBuilder = new BackGroundUnderlineGeometryBuilder();
                geoBuilder.AddSegment(textView, mark);
                var geometry = geoBuilder.CreateGeometry();
                SolidColorBrush brush = new SolidColorBrush(mark.Color);
                Pen pen;
                switch (mark.Style)
                {
                    case CodeDrawStyle.MarkDetail.MarkStyleEnum.DotLine:
                        pen = new Pen(brush, mark.Thickness, DashStyle.Dash);
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
