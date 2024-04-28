using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Rendering;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Resources;
using System.Security.Cryptography.X509Certificates;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CodeEditor2.CodeEditor
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

            if (Global.mainView.CodeView.CodeDocument == null) return;

            CodeDocument codeDocument = Global.mainView.CodeView.CodeDocument;
            if (!codeDocument.LineInfomations.ContainsKey(line.LineNumber)) return;
            CodeEditor.LineInfomation lineInfo = codeDocument.LineInfomations[line.LineNumber];

            foreach (var color in lineInfo.Colors)
            {
                if (line.Offset > color.Offset | color.Offset + color.Length > line.EndOffset) continue;
                ChangeLinePart(
                    color.Offset,
                    color.Offset + color.Length,
                    visualLine =>
                    {
                        visualLine.TextRunProperties.SetForegroundBrush(new SolidColorBrush(color.DrawColor));
                    }
                );
            }

            //foreach (var effect in lineInfo.Effects)
            //{
            //    if (line.Offset > effect.Offset | effect.Offset + effect.Length > line.EndOffset) continue;
            //    ChangeLinePart(
            //        effect.Offset,
            //        effect.Offset + effect.Length,
            //        visualLine =>
            //        {
            //            TextDecoration underline = new TextDecoration();// TextDecorations.Underline.Last();
            //            underline.Stroke = new SolidColorBrush(effect.DrawColor);
            //            underline.StrokeThickness = 2;
            //            underline.StrokeThicknessUnit = TextDecorationUnit.Pixel;
            //            underline.StrokeOffset = 2;
            //            underline.StrokeOffsetUnit = TextDecorationUnit.Pixel;
            //            switch (effect.MarkStyle)
            //            {
            //                case MarkStyleEnum.DashedLine0:
            //                    underline.StrokeDashArray = new Avalonia.Collections.AvaloniaList<double>(2, 2);
            //                    break;
            //                case MarkStyleEnum.ThinUnderLine:
            //                    underline.StrokeThickness = 1;
            //                    break;
            //            }

            //            if (visualLine.TextRunProperties.TextDecorations == null)
            //            {
            //                visualLine.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
            //                var textDecorations = new TextDecorationCollection(visualLine.TextRunProperties.TextDecorations) { underline };
            //                visualLine.TextRunProperties.SetTextDecorations(textDecorations);
            //            }
            //            else
            //            {
            //                visualLine.TextRunProperties.TextDecorations.Add(underline);
            //            }
            //        }
            //    );
            //}
        }
    }
}
