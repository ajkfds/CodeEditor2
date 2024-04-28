using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls.Primitives;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Utils;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaEdit.Rendering;
using AvaloniaEdit;
using ShimSkiaSharp;
using System.Net.Http.Headers;

namespace CodeEditor2.CodeEditor
{
    internal class BackGroundUnderlineGeometryBuilder
    {

        /// <summary>
        /// Gets/sets the border thickness.
        /// 
        /// This property only has an effect if <c>AlignToWholePixels</c> is enabled.
        /// When using the resulting geometry to paint a border, set this property to the border thickness.
        /// Otherwise, leave the property set to the default value <c>0</c>.
        /// </summary>
        public double BorderThickness { get; set; }

        public double HorizontalOffset { get; set; } = 2;

        /// <summary>
        /// Gets/Sets whether to extend the rectangles to full width at line end.
        /// </summary>
        public bool ExtendToFullWidthAtLineEnd { get; set; }

        /// <summary>
        /// Creates a new BackgroundGeometryBuilder instance.
        /// </summary>
        public BackGroundUnderlineGeometryBuilder()
        {
        }

        /// <summary>
        /// Adds the specified segment to the geometry.
        /// </summary>
        public void AddSegment(TextView textView, MarkerRenderer.Mark mark)
        {
            if (textView == null)
                throw new ArgumentNullException("textView");
            Size pixelSize = PixelSnapHelpers.GetPixelSize(textView);
            foreach (Rect r in GetRectsForSegment(textView, mark, ExtendToFullWidthAtLineEnd))
            {
                AddRectangle(pixelSize, r,mark);
            }
        }

        public double WaveWidth { get; set; } = 4;
        public double WaveHeight { get; set; } = 1;



        /// <summary>
        /// Adds a rectangle to the geometry.
        /// </summary>
        /// <remarks>
        /// This overload will align the coordinates according to
        /// <see cref="AlignToWholePixels"/>.
        /// Use the <see cref="AddRectangle(double,double,double,double)"/>-overload instead if the coordinates should not be aligned.
        /// </remarks>
        public void AddRectangle(TextView textView, Rect rectangle, MarkerRenderer.Mark mark)
        {
            AddRectangle(PixelSnapHelpers.GetPixelSize(textView), rectangle ,mark);
        }

        private void AddRectangle(Size pixelSize, Rect r, MarkerRenderer.Mark mark)
        {
            double halfBorder = 0.5 * BorderThickness;
            switch (mark.Style)
            {
                case CodeDrawStyle.MarkInfo.MarkStyleEnum.WaveLine:
                    AddWaveLine(
                        mark,
                        PixelSnapHelpers.Round(r.Left - halfBorder, pixelSize.Width) + halfBorder,
                        PixelSnapHelpers.Round(r.Bottom - halfBorder, pixelSize.Height) + halfBorder - HorizontalOffset,
                        PixelSnapHelpers.Round(r.Right + halfBorder, pixelSize.Width) - halfBorder,
                        PixelSnapHelpers.Round(r.Bottom + halfBorder, pixelSize.Height) - halfBorder - HorizontalOffset
                        );
                    break;
                case CodeDrawStyle.MarkInfo.MarkStyleEnum.UnderLine:
                    break;
            }
        }

        /// <summary>
        /// Calculates the list of rectangle where the segment in shown.
        /// This method usually returns one rectangle for each line inside the segment
        /// (but potentially more, e.g. when bidirectional text is involved).
        /// </summary>
        public static IEnumerable<Rect> GetRectsForSegment(TextView textView, ISegment segment, bool extendToFullWidthAtLineEnd = false)
        {
            if (textView == null)
                throw new ArgumentNullException("textView");
            if (segment == null)
                throw new ArgumentNullException("segment");
            return GetRectsForSegmentImpl(textView, segment, extendToFullWidthAtLineEnd);
        }

        private static IEnumerable<Rect> GetRectsForSegmentImpl(TextView textView, ISegment segment, bool extendToFullWidthAtLineEnd)
        {
            int segmentStart = segment.Offset;
            int segmentEnd = segment.Offset + segment.Length;

            segmentStart = segmentStart.CoerceValue(0, textView.Document.TextLength);
            segmentEnd = segmentEnd.CoerceValue(0, textView.Document.TextLength);

            TextViewPosition start;
            TextViewPosition end;

            if (segment is SelectionSegment)
            {
                SelectionSegment sel = (SelectionSegment)segment;
                start = new TextViewPosition(textView.Document.GetLocation(sel.StartOffset), sel.StartVisualColumn);
                end = new TextViewPosition(textView.Document.GetLocation(sel.EndOffset), sel.EndVisualColumn);
            }
            else
            {
                start = new TextViewPosition(textView.Document.GetLocation(segmentStart));
                end = new TextViewPosition(textView.Document.GetLocation(segmentEnd));
            }

            foreach (VisualLine vl in textView.VisualLines)
            {
                int vlStartOffset = vl.FirstDocumentLine.Offset;
                if (vlStartOffset > segmentEnd)
                    break;
                int vlEndOffset = vl.LastDocumentLine.Offset + vl.LastDocumentLine.Length;
                if (vlEndOffset < segmentStart)
                    continue;

                int segmentStartVc;
                if (segmentStart < vlStartOffset)
                    segmentStartVc = 0;
                else
                    segmentStartVc = vl.ValidateVisualColumn(start, extendToFullWidthAtLineEnd);

                int segmentEndVc;
                if (segmentEnd > vlEndOffset)
                    segmentEndVc = extendToFullWidthAtLineEnd ? int.MaxValue : vl.VisualLengthWithEndOfLineMarker;
                else
                    segmentEndVc = vl.ValidateVisualColumn(end, extendToFullWidthAtLineEnd);

                foreach (var rect in ProcessTextLines(textView, vl, segmentStartVc, segmentEndVc))
                    yield return rect;
            }
        }


        private static IEnumerable<Rect> ProcessTextLines(TextView textView, VisualLine visualLine, int segmentStartVc, int segmentEndVc)
        {
            TextLine lastTextLine = visualLine.TextLines.Last();
            Vector scrollOffset = textView.ScrollOffset;

            for (int i = 0; i < visualLine.TextLines.Count; i++)
            {
                TextLine line = visualLine.TextLines[i];
                double y = visualLine.GetTextLineVisualYPosition(line, VisualYPosition.TextTop);
                int visualStartCol = visualLine.GetTextLineVisualStartColumn(line);
                int visualEndCol = visualStartCol + line.Length;
                if (line == lastTextLine)
                    visualEndCol -= 1; // 1 position for the TextEndOfParagraph
                else
                    visualEndCol -= line.TrailingWhitespaceLength;

                if (segmentEndVc < visualStartCol)
                    break;
                if (lastTextLine != line && segmentStartVc > visualEndCol)
                    continue;
                int segmentStartVcInLine = Math.Max(segmentStartVc, visualStartCol);
                int segmentEndVcInLine = Math.Min(segmentEndVc, visualEndCol);
                y -= scrollOffset.Y;
                Rect lastRect = default;
                if (segmentStartVcInLine == segmentEndVcInLine)
                {
                    // GetTextBounds crashes for length=0, so we'll handle this case with GetDistanceFromCharacterHit
                    // We need to return a rectangle to ensure empty lines are still visible
                    double pos = visualLine.GetTextLineVisualXPosition(line, segmentStartVcInLine);
                    pos -= scrollOffset.X;
                    // The following special cases are necessary to get rid of empty rectangles at the end of a TextLine if "Show Spaces" is active.
                    // If not excluded once, the same rectangle is calculated (and added) twice (since the offset could be mapped to two visual positions; end/start of line), if there is no trailing whitespace.
                    // Skip this TextLine segment, if it is at the end of this line and this line is not the last line of the VisualLine and the selection continues and there is no trailing whitespace.
                    if (segmentEndVcInLine == visualEndCol && i < visualLine.TextLines.Count - 1 && segmentEndVc > segmentEndVcInLine && line.TrailingWhitespaceLength == 0)
                        continue;
                    if (segmentStartVcInLine == visualStartCol && i > 0 && segmentStartVc < segmentStartVcInLine && visualLine.TextLines[i - 1].TrailingWhitespaceLength == 0)
                        continue;
                    lastRect = new Rect(pos, y, textView.EmptyLineSelectionWidth, line.Height);
                }
                else
                {
                    if (segmentStartVcInLine <= visualEndCol)
                    {
                        foreach (var b in line.GetTextBounds(segmentStartVcInLine, segmentEndVcInLine - segmentStartVcInLine))
                        {
                            double left = b.Rectangle.Left - scrollOffset.X;
                            double right = b.Rectangle.Right - scrollOffset.X;
                            if (lastRect != default)
                                yield return lastRect;
                            // left>right is possible in RTL languages
                            lastRect = new Rect(Math.Min(left, right), y, Math.Abs(right - left), line.Height);
                        }
                    }
                }
                // If the segment ends in virtual space, extend the last rectangle with the rectangle the portion of the selection
                // after the line end.
                // Also, when word-wrap is enabled and the segment continues into the next line, extend lastRect up to the end of the line.
                if (segmentEndVc > visualEndCol)
                {
                    double left, right;
                    if (segmentStartVc > visualLine.VisualLengthWithEndOfLineMarker)
                    {
                        // segmentStartVC is in virtual space
                        left = visualLine.GetTextLineVisualXPosition(lastTextLine, segmentStartVc);
                    }
                    else
                    {
                        // Otherwise, we already processed the rects from segmentStartVC up to visualEndCol,
                        // so we only need to do the remainder starting at visualEndCol.
                        // For word-wrapped lines, visualEndCol doesn't include the whitespace hidden by the wrap,
                        // so we'll need to include it here.
                        // For the last line, visualEndCol already includes the whitespace.
                        left = (line == lastTextLine ? line.WidthIncludingTrailingWhitespace : line.Width);
                    }
                    if (line != lastTextLine || segmentEndVc == int.MaxValue)
                    {
                        // If word-wrap is enabled and the segment continues into the next line,
                        // or if the extendToFullWidthAtLineEnd option is used (segmentEndVC == int.MaxValue),
                        // we select the full width of the viewport.
                        right = Math.Max(((ILogicalScrollable)textView).Extent.Width, ((ILogicalScrollable)textView).Viewport.Width);
                    }
                    else
                    {
                        right = visualLine.GetTextLineVisualXPosition(lastTextLine, segmentEndVc);
                    }
                    Rect extendSelection = new Rect(Math.Min(left, right), y, Math.Abs(right - left), line.Height);
                    if (lastRect != default)
                    {
                        if (extendSelection.Intersects(lastRect))
                        {
                            lastRect.Union(extendSelection);
                            yield return lastRect;
                        }
                        else
                        {
                            // If the end of the line is in an RTL segment, keep lastRect and extendSelection separate.
                            yield return lastRect;
                            yield return extendSelection;
                        }
                    }
                    else
                        yield return extendSelection;
                }
                else
                    yield return lastRect;
            }
        }

        private List<Point> points = new List<Point>();

        public void AddWaveLine(MarkerRenderer.Mark mark,double left, double top, double right, double bottom)
        {
            double waveWidth = mark.DecorationWidth;
            double waveHeight = mark.DecorationHeight;

            {
                int waveCount = (int)((right - left) / waveWidth);
                double step = (right - left) / (double)waveCount;

                for (int i=0; i < waveCount; i++)
                {
                    points.Add(new Point(left + step*i , bottom - waveHeight));
                    points.Add(new Point(left + step * i+step/2.0, bottom + waveHeight));
                }
                points.Add(new Point(right, bottom - waveHeight));
            }
        }
        public void AddUnderLine(MarkerRenderer.Mark mark, double left, double top, double right, double bottom)
        {
            points.Add(new Point(left, bottom));
            points.Add(new Point(right, bottom));
        }
        private static LineSegment MakeLineSegment(double x, double y)
        {
            return new LineSegment { Point = new Point(x, y) };
        }

        /// <summary>
        /// Creates the geometry.
        /// Returns null when the geometry is empty!
        /// </summary>
        public Geometry CreateGeometry()
        {
            if (points.Count == 0) return null;
            return new PolylineGeometry(points, false);
        }
    }
}
