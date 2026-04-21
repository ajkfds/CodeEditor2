using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;

namespace CodeEditor2.CodeEditor.TextDecollation
{
    public class CodeDocumentColorTransformer : DocumentColorizingTransformer
    {
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
                        ChangeLinePart(
                            color.Offset + line.Offset,
                            color.Offset + line.Offset + color.Length,
                            visualLine =>
                            {
                                visualLine.TextRunProperties.SetForegroundBrush(new SolidColorBrush(color.DrawColor));
                            }
                        );
                    }
                }
            }
        }
    }
}
