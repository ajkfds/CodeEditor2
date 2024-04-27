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
    internal class HighlightRenderer : IBackgroundRenderer
    {
        public TextSegmentCollection<TextSegment> CurrentResults { get; } = new TextSegmentCollection<TextSegment>();

        public KnownLayer Layer => KnownLayer.Background;

        public HighlightRenderer(IBrush brush)
        {
            _markerBrush = brush;
        }

        private IBrush _markerBrush;

        public IBrush MarkerBrush
        {
            get => _markerBrush;
            set
            {
                _markerBrush = value;
            }
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (textView == null)
                throw new ArgumentNullException(nameof(textView));
            if (drawingContext == null)
                throw new ArgumentNullException(nameof(drawingContext));

            if (CurrentResults == null || !textView.VisualLinesValid)
                return;

            var visualLines = textView.VisualLines;
            if (visualLines.Count == 0)
                return;

            var viewStart = visualLines.First().FirstDocumentLine.Offset;
            var viewEnd = visualLines.Last().LastDocumentLine.EndOffset;

            foreach (var result in CurrentResults.FindOverlappingSegments(viewStart, viewEnd - viewStart))
            {
                var geoBuilder = new BackgroundGeometryBuilder
                {
                    AlignToWholePixels = true,
                    CornerRadius = 0
                };
                geoBuilder.AddSegment(textView, result);
                var geometry = geoBuilder.CreateGeometry();
                if (geometry != null)
                {
                    drawingContext.DrawGeometry(_markerBrush, null, geometry);
                    //drawingContext.DrawGeometry(null, pen, geometry);
                }
            }
        }


    }
}
