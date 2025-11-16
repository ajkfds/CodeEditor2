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
                        if ( color.Offset　<　0 || line.Length < color.Offset + color.Length  ) continue;
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
