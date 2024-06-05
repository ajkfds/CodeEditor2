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
            lock (this)
            {
                if (Global.mainView.CodeView.CodeDocument == null) return;

                CodeDocument codeDocument = Global.mainView.CodeView.CodeDocument;
                if (!codeDocument.TextColors.LineInfomations.ContainsKey(line.LineNumber)) return;
                CodeEditor.LineInfomation lineInfo = codeDocument.TextColors.LineInfomations[line.LineNumber];

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


            }
        }
    }
}
