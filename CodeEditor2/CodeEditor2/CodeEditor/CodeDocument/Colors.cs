using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using CodeEditor2.NavigatePanel;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
using static System.Net.Mime.MediaTypeNames;

namespace CodeEditor2.CodeEditor
{
    public class Colors
    {
        public Colors(CodeDocument codeDocument)
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
            if (lineInfo.Colors.Count > 2000)
            {
                string a = "";
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
            if (codeDocument.TextDocument == null) return;
            if (codeDocument.TextFile == null) return;
            DocumentLine line = codeDocument.TextDocument.GetLineByOffset(index);
            LineInformation lineInfo = GetLineInformation(line.LineNumber);
            Color color = codeDocument.TextFile.DrawStyle.ColorPallet[value];
            lock (lineInfo.Colors)
            {
                lineInfo.Colors.Add(new LineInformation.Color(index, 1, color));
            }
        }

        public virtual void SetColorAt(int index, byte value, int length)
        {
            if (value == 0)
            {
                string a = "";
            }

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
                //     start    last
                //       +=======+

                // |---|                 a0
                // |---------|           a1
                // |-------------------| a2

                //           |---|       b0
                //           |---------| b1

                //                  |--| c0
                lock (lineInfo.Colors)
                {
                    foreach (var color in lineInfo.Colors)
                    {
                        if (color == null) continue;

                        int start = color.Offset;
                        int last = color.Offset + color.Length;
                        int change = e.InsertionLength - e.RemovalLength;

                        if (e.Offset <= start) // a0 | a1 | a2
                        {
                            if (e.Offset + e.RemovalLength <= start)
                            { // a0
                                color.Offset += change;
                            }
                            else if (e.Offset + e.RemovalLength <= last)
                            { // a1
                                color.Offset = e.Offset;
                            }
                            else
                            { // a2
                                if (color.Offset + color.Length + change > color.Offset) color.Length += change;
                                else removeTarget.Add(color);
                            }
                        }
                        else if (e.Offset <= color.Offset + color.Length) // b0 | b1
                        {
                            if (e.Offset + e.RemovalLength <= last)
                            { // b0
                                color.Length += change;
                            }
                            else
                            { // b1
                              // none
                            }
                        }
                        else
                        { // c0
                          // none
                        }

                    }
                    foreach (var removeMark in removeTarget)
                    {
                        lineInfo.Colors.Remove(removeMark);
                    }

                }
            }

        }
    }
}
