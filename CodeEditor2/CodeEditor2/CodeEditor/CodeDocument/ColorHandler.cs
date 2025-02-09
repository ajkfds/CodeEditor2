using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using CodeEditor2.CodeEditor.TextDecollation;
using CodeEditor2.NavigatePanel;
using DynamicData;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReactiveUI;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static CodeEditor2.CodeEditor.TextDecollation.MarkerRenderer;
using static System.Net.Mime.MediaTypeNames;

namespace CodeEditor2.CodeEditor
{
    public class ColorHandler
    {
        public ColorHandler(CodeDocument codeDocument)
        {
            this.codeDocument = codeDocument;
        }
        CodeDocument codeDocument;

        public Dictionary<int, LineInformation> LineInformation = new Dictionary<int, LineInformation>();
        protected LineInformation GetLineInformation(int lineNumber)
        {
            LineInformation lineInfo;
            if (LineInformation.ContainsKey(lineNumber))
            {
                lineInfo = LineInformation[lineNumber];
            }
            else
            {
                lineInfo = new LineInformation();
                LineInformation.Add(lineNumber, lineInfo);
            }
            return lineInfo;
        }


        public byte GetColorAt(int index)
        {
            //            return colors[index];
            return 0;
        }

        public void RemoveColors()
        {
            LineInformation.Clear();
        }

        public virtual void SetColorAt(int index, byte value)
        {
            SetColorAt(index, value, 1);
            //if (codeDocument.TextDocument == null) return;
            //if (codeDocument.TextFile == null) return;
            //DocumentLine line = codeDocument.TextDocument.GetLineByOffset(index);
            //LineInformation lineInfo = GetLineInformation(line.LineNumber);
            //Color color = codeDocument.TextFile.DrawStyle.ColorPallet[value];
            //lock (lineInfo.Colors)
            //{
            //    lineInfo.Colors.Add(new LineInformation.Color(index, 1, color));
            //}
        }

        public virtual void SetColorAt(int index, byte value, int length)
        {
            if (codeDocument.TextDocument == null) return;
            if (codeDocument.TextFile == null) return;

            DocumentLine lineStart = codeDocument.TextDocument.GetLineByOffset(index);
            DocumentLine lineLast = codeDocument.TextDocument.GetLineByOffset(index + length);
            Color color = codeDocument.TextFile.DrawStyle.ColorPallet[value];

            if (lineStart == lineLast)
            {
                LineInformation lineInfo = GetLineInformation(lineStart.LineNumber);
                lock (lineInfo.Colors)
                {
                    lineInfo.Colors.Add(new LineInformation.Color(index, length, color));
                }
            }
            else
            {
                LineInformation lineInfo = GetLineInformation(lineStart.LineNumber);
                lineInfo.Colors.Add(new LineInformation.Color(index, codeDocument.GetLineLength(lineStart.LineNumber) - (index - codeDocument.GetLineStartIndex(lineStart.LineNumber)), color));

                lineInfo = GetLineInformation(lineLast.LineNumber);
                lock (lineInfo.Colors)
                {
                    lineInfo.Colors.Add(new LineInformation.Color(codeDocument.GetLineStartIndex(lineLast.LineNumber), index + length - codeDocument.GetLineStartIndex(lineLast.LineNumber), color));
                }

                for (int line = lineStart.LineNumber + 1; line <= lineLast.LineNumber - 1; line++)
                {
                    lineInfo = GetLineInformation(line);
                    lock (lineInfo.Colors)
                    {
                        lineInfo.Colors.Add(new LineInformation.Color(codeDocument.GetLineStartIndex(line), codeDocument.GetLineLength(line), color));
                    }
                }
            }
        }

        public void OnTextEdit(DocumentChangeEventArgs e)
        {
            DocumentLine startLine = codeDocument.TextDocument.GetLineByOffset(e.Offset);
            DocumentLine endLine = codeDocument.TextDocument.GetLineByOffset(e.Offset + e.RemovalLength);


            if (startLine.LineNumber == endLine.LineNumber)
            { // startLine = endLine

                LineInformation lineInfo = GetLineInformation(startLine.LineNumber);
                List<LineInformation.Color> removeTarget = new List<LineInformation.Color>();

                lock (lineInfo.Colors)
                {
                    foreach (var color in lineInfo.Colors)
                    {
                        if (color == null) continue;
                        updateColor(e, color, removeTarget);
                    }
                    foreach (var removeMark in removeTarget)
                    {
                        lineInfo.Colors.Remove(removeMark);
                    }

                }
            }

        }

        public void updateColor(DocumentChangeEventArgs e, LineInformation.Color color, List<LineInformation.Color> removeTarget)
        {
            int change = e.InsertionLength - e.RemovalLength;

            if (e.Offset < color.Offset)
            {
                //                      color
                //               start       last
                //                 v           v
                // .   .   .   .   .   .   .   .   .   .   .   .
                //                 =============
                //     |------->
                //     |------------------->
                //     |------------------------------->
                if (e.Offset + e.RemovalLength < color.Offset)
                {
                    color.Offset += change;
                }
                else if (e.Offset + e.RemovalLength == color.Offset)
                {
                    color.Offset += change;
                }
                else if (e.Offset + e.RemovalLength < color.Offset + color.Length)
                {
                    int length = color.Offset + color.Length - (e.Offset + e.RemovalLength);
                    color.Offset = e.Offset + e.RemovalLength + change;
                    color.Length = length;
                }
                else if (e.Offset + e.RemovalLength == color.Offset + color.Length)
                {
                    removeTarget.Add(color);
                }
                else
                {
                    removeTarget.Add(color);
                }
            }
            else if (e.Offset == color.Offset)
            {
                //                      color
                //               start       last
                //                 v           v
                // .   .   .   .   .   .   .   .   .   .   .   .
                //                 =============
                //                 |------->
                //                 |----------->
                //                 |------------------->
                if (e.Offset + e.RemovalLength < color.Offset + color.Length)
                {
                    color.Offset = e.Offset + e.RemovalLength + change;
                    // color.length kept
                }
                else if (e.Offset + e.RemovalLength == color.Offset + color.Length)
                {
                    removeTarget.Add(color);
                }
                else
                {
                    removeTarget.Add(color);
                }
            }
            else if (e.Offset < color.Offset + color.Length)
            {
                //                      color
                //               start       last
                //                 v           v
                // .   .   .   .   .   .   .   .   .   .   .   .
                //                 =============
                //                     |--->
                //                     |------->
                //                     |--------------->
                if (e.Offset + e.RemovalLength < color.Offset + color.Length)
                {
                    color.Length += change;
                }
                else if (e.Offset + e.RemovalLength == color.Offset + color.Length)
                {
                    color.Length = e.Offset - color.Offset;
                }
                else
                {
                    color.Length = e.Offset - color.Offset;
                }
            }
            else if (e.Offset == color.Offset + color.Length)
            {
                //                      color
                //               start       last
                //                 v           v
                // .   .   .   .   .   .   .   .   .   .   .   .
                //                 =============
                //                             |------->

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

    }

}
