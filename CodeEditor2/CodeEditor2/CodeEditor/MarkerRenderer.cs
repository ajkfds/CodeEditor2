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
        internal TextSegmentCollection<TextSegment> marks { get; } = new TextSegmentCollection<TextSegment>();

        public void ClearMark()
        {
            marks.Clear();
        }
        public void SetMarks(List<CodeDrawStyle.MarkInfo> marks)
        {
            this.marks.Clear();
            foreach(var mark in marks)
            {
                this.marks.Add(Mark.CloneFrom(mark));
            }
        }

        public class Mark : AvaloniaEdit.Document.TextSegment
        {
            public Color Color;
            public double DecorationWidth = 4;
            public double DecorationHeight = 1;
            public double Thickness = 1;
            public CodeDrawStyle.MarkInfo.MarkStyleEnum Style;

            public static Mark CloneFrom(CodeDrawStyle.MarkInfo markInfo)
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
                    case CodeDrawStyle.MarkInfo.MarkStyleEnum.DotLine:
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
