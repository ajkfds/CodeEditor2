﻿using Avalonia.Controls;
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
            lock (this)
            {
                if (Global.mainView.CodeView.CodeDocument == null) return;

                CodeDocument codeDocument = Global.mainView.CodeView.CodeDocument;
                if (!codeDocument.TextColors.LineInformation.ContainsKey(line.LineNumber)) return;
                LineInformation lineInfo = codeDocument.TextColors.LineInformation[line.LineNumber];

                lock (lineInfo.Colors)
                {
                    foreach (var color in lineInfo.Colors)
                    {
//                        if (line.Offset > color.Offset | color.Offset + color.Length > line.EndOffset) continue;
                        if ( color.Offset<0 | color.Offset + color.Length > line.Length ) continue;
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
