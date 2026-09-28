using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using System.Collections.Generic;

namespace CodeEditor2.CodeEditor.TextDecollation
{
    public class CodeDocumentColorTransformer : DocumentColorizingTransformer
    {
        // Shared brush cache.
        // ColorizeLine is called for every visual line rebuild; creating a new
        // SolidColorBrush per color segment on each rebuild is expensive.
        // The number of distinct colors is small (DrawStyle palette), so cache
        // brushes per Avalonia color and reuse them.
        private static readonly object brushCacheLock = new object();
        private static readonly Dictionary<Color, SolidColorBrush> brushCache = new Dictionary<Color, SolidColorBrush>();

        private static SolidColorBrush GetBrush(Color color)
        {
            lock (brushCacheLock)
            {
                if (!brushCache.TryGetValue(color, out SolidColorBrush? brush))
                {
                    brush = new SolidColorBrush(color);
                    brushCache[color] = brush;
                }
                return brush;
            }
        }

        //public enum MarkStyleEnum
        //{
        //    ThickUnderLine,
        //    ThinUnderLine,
        //    DashedLine0,
        //    DashedLine1,
        //}

        protected override void ColorizeLine(DocumentLine line)
        {
            CodeDocument? codeDocument = Global.mainView.CodeView.CodeDocument;
            if (codeDocument == null) return;

            lock (this)
            {
                if (!codeDocument.TextColors.LineInformation.ContainsKey(line.LineNumber)) return;
                LineInformation lineInfo = codeDocument.TextColors.LineInformation[line.LineNumber];

                lock (lineInfo.Colors)
                {
                    foreach (var color in lineInfo.Colors)
                    {
                        if (color.Offset < 0 || line.Length < color.Offset + color.Length) continue;
                        SolidColorBrush brush = GetBrush(color.DrawColor);
                        ChangeLinePart(
                            color.Offset + line.Offset,
                            color.Offset + line.Offset + color.Length,
                            visualLine =>
                            {
                                visualLine.TextRunProperties.SetForegroundBrush(brush);
                            }
                        );
                    }
                }
            }
        }
    }
}
