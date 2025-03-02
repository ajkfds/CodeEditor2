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
using System.Security.AccessControl;
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
                lock (LineInformation)
                {
                    LineInformation.Add(lineNumber, lineInfo);
                }
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
                    lineInfo.Colors.Add(new LineInformation.Color(index- codeDocument.GetLineStartIndex(lineStart.LineNumber), length, color));
                }
            }
            else
            {
                LineInformation lineInfo = GetLineInformation(lineStart.LineNumber);
                lineInfo.Colors.Add(new LineInformation.Color(index - codeDocument.GetLineStartIndex(lineStart.LineNumber), codeDocument.GetLineLength(lineStart.LineNumber) - (index - codeDocument.GetLineStartIndex(lineStart.LineNumber)), color));

                lineInfo = GetLineInformation(lineLast.LineNumber);
                lock (lineInfo.Colors)
                {
                    lineInfo.Colors.Add(new LineInformation.Color(0, index + length - codeDocument.GetLineStartIndex(lineLast.LineNumber), color));
                }

                for (int line = lineStart.LineNumber + 1; line <= lineLast.LineNumber - 1; line++)
                {
                    lineInfo = GetLineInformation(line);
                    lock (lineInfo.Colors)
                    {
                        lineInfo.Colors.Add(new LineInformation.Color(0, codeDocument.GetLineLength(line), color));
                    }
                }
            }
        }

        public void OnTextEdit(DocumentChangeEventArgs e)
        {
            DocumentLine startLine = codeDocument.TextDocument.GetLineByOffset(e.Offset);
            DocumentLine endLine = codeDocument.TextDocument.GetLineByOffset(e.Offset + e.RemovalLength);

            int removeLines = endLine.LineNumber - startLine.LineNumber;

            string insertedText = e.InsertedText.Text;
            insertedText = insertedText.Replace("\r\n", "\n");
            insertedText = insertedText.Replace("\r", "\n");

            int insertLines = 0;
            {
                int i = insertedText.IndexOf('\n', 0, insertedText.Length);
                while (i >= 0)
                {
                    insertLines++;
                    i = insertedText.IndexOf('\n', i+1, insertedText.Length-i-1);
                }
            }

            // edit in line
            if(removeLines == 0 && insertLines == 0)
            { // inline edi
                LineInformation lineInfo = GetLineInformation(startLine.LineNumber);
                List<LineInformation.Color> removeTarget = new List<LineInformation.Color>();

                lock (lineInfo.Colors)
                {
                    foreach (var color in lineInfo.Colors)
                    {
                        int offset = e.Offset - codeDocument.GetLineStartIndex(startLine.LineNumber);
                        updateColor(offset, e.InsertionLength, e.RemovalLength, color, removeTarget);
                    }
                    foreach (var removeMark in removeTarget)
                    {
                        lineInfo.Colors.Remove(removeMark);
                    }
                }
                return;
            }

            // remove
            if(removeLines != 0)
            {
                int startLineLength = 0;
                { // startline
                    LineInformation lineInfo = GetLineInformation(startLine.LineNumber);
                    List<LineInformation.Color> removeTarget = new List<LineInformation.Color>();

                    lock (lineInfo.Colors)
                    {
                        int insertionLength = e.InsertionLength;
                        int removalLength = e.RemovalLength;
                        int offset = e.Offset - codeDocument.GetLineStartIndex(startLine.LineNumber);
                        int lineLength = codeDocument.GetLineLength(startLine.LineNumber);
                        if (offset + removalLength > lineLength) removalLength = lineLength - offset;
                        startLineLength = lineLength - removalLength;
                        foreach (var color in lineInfo.Colors)
                        {
                            updateColor(offset, 0, removalLength, color, removeTarget);
                        }
                        foreach (var removeMark in removeTarget)
                        {
                            lineInfo.Colors.Remove(removeMark);
                        }
                    }
                }
                { // endline
                    LineInformation lineInfo = GetLineInformation(endLine.LineNumber);
                    List<LineInformation.Color> removeTarget = new List<LineInformation.Color>();

                    lock (lineInfo.Colors)
                    {
                        foreach (var color in lineInfo.Colors)
                        {
                            if (color == null) continue;
                            int insertionLength = e.InsertionLength;
                            int removalLength = e.RemovalLength;
                            int lineLength = codeDocument.GetLineLength(endLine.LineNumber);
                            removalLength = e.Offset + e.RemovalLength - codeDocument.GetLineStartIndex(endLine.LineNumber);

                            updateColor(0, 0, removalLength, color, removeTarget);

                            color.Offset += startLineLength;
                            }
                        foreach (var removeMark in removeTarget)
                        {
                            lineInfo.Colors.Remove(removeMark);
                        }
                    }
                }
                lock (LineInformation)
                {
                    for (int i = startLine.LineNumber + 1; i < endLine.LineNumber; i++)
                    {
                        if (LineInformation.ContainsKey(i)) LineInformation.Remove(i);
                    }
                    List<KeyValuePair<int, LineInformation>> shiftLines = new List<KeyValuePair<int, LineInformation>>();
                    foreach (var line in LineInformation)
                    {
                        if (line.Key >= endLine.LineNumber) shiftLines.Add(line);
                    }
                    foreach (var line in shiftLines)
                    {
                        if (LineInformation.ContainsKey(line.Key)) LineInformation.Remove(line.Key);
                    }
                    foreach (var line in shiftLines)
                    {
                        if (!LineInformation.ContainsKey(line.Key - removeLines))
                        {
                            LineInformation.Add(line.Key - removeLines, line.Value);
                        }
                        else
                        {
                            LineInformation prev = LineInformation[line.Key - removeLines];
                            LineInformation append = line.Value;
                            foreach (var color in append.Colors)
                            {
                                prev.Colors.Add(color);
                            }
                        }
                    }
                }
            }

            // insert
            if (insertLines == 0) return;

            // shift lines
            lock (LineInformation)
            {
                List<KeyValuePair<int, LineInformation>> shiftLines = new List<KeyValuePair<int, LineInformation>>();
                foreach (var line in LineInformation)
                {
                    if (line.Key >  startLine.LineNumber) shiftLines.Add(line);
                }
                foreach (var line in shiftLines)
                {
                    if (LineInformation.ContainsKey(line.Key)) LineInformation.Remove(line.Key);
                }
                foreach (var line in shiftLines)
                {
                    if (!LineInformation.ContainsKey(line.Key + insertLines))
                    {
                        LineInformation.Add(line.Key + insertLines, line.Value);
                    }
                    else
                    {
                        LineInformation prev = LineInformation[line.Key + insertLines];
                        LineInformation append = line.Value;
                        foreach (var color in append.Colors)
                        {
                            prev.Colors.Add(color);
                        }
                    }
                }
            }
            // create new line
            lock (LineInformation)
            {
                if (!LineInformation.ContainsKey(startLine.LineNumber)) return;

                // duplicate insert start line to new linew
                LineInformation.Add(startLine.LineNumber + insertLines, LineInformation[startLine.LineNumber].Clone());
            }

            int insertionLengthAtStartline = insertedText.IndexOf('\n', 0, insertedText.Length);
            int insertionLengthAtLastline = insertedText.Length - insertedText.LastIndexOf('\n', insertedText.Length-1, insertedText.Length)-1;
            int insertOffsetAtStartline = e.Offset - codeDocument.GetLineStartIndex(startLine.LineNumber);

            { // insert startline
                LineInformation lineInfo = GetLineInformation(startLine.LineNumber);
                List<LineInformation.Color> removeTarget = new List<LineInformation.Color>();

                lock (lineInfo.Colors)
                {
                    int lineLength = codeDocument.GetLineLength(startLine.LineNumber);
                    foreach (var color in lineInfo.Colors)
                    {
                        updateColor(insertOffsetAtStartline, insertionLengthAtStartline, lineLength, color, removeTarget);
                    }
                    foreach (var removeMark in removeTarget)
                    {
                        lineInfo.Colors.Remove(removeMark);
                    }
                }
            }
            { // insertion endline
                LineInformation lineInfo = GetLineInformation(startLine.LineNumber + insertLines);
                List<LineInformation.Color> removeTarget = new List<LineInformation.Color>();
                List<LineInformation.Color> addTarget = new List<LineInformation.Color>();

                lock (lineInfo.Colors)
                {
                    foreach (var color in lineInfo.Colors)
                    {
                        updateColor(0, insertionLengthAtLastline, insertOffsetAtStartline, color, removeTarget);
                    }
                    foreach (var removeMark in removeTarget)
                    {
                        lineInfo.Colors.Remove(removeMark);
                    }
                }
            }
        }

        public void updateColor(int offset,int insertionLength,int removalLength,LineInformation.Color color, List<LineInformation.Color> removeTarget)
        {
            if (offset < color.Offset)
            {
                if (offset + removalLength < color.Offset)
                {
                    //                      color
                    //               start       last
                    //                 v           v
                    // .   .   .   .   .   .   .   .   .   .   .   .
                    //                 =============
                    //     |------->                 removal area
                    // remove
                    color.Offset -= removalLength;
                    // insert
                    color.Offset += insertionLength;
                }
                else if (offset + removalLength < color.Offset + color.Length)
                {
                    //                      color
                    //               start       last
                    //                 v           v
                    // .   .   .   .   .   .   .   .   .   .   .   .
                    //                 =============
                    //     |------------------>      removalarea
                    // remove
                    //                 <------> duplicate
                    int duplicate = offset + removalLength - color.Offset;

                    color.Offset = offset;
                    color.Length -= duplicate;
                    // insert
                    color.Offset += insertionLength;
                }
                else
                {
                    //                      color
                    //               start       last
                    //                 v           v
                    // .   .   .   .   .   .   .   .   .   .   .   .
                    //                 =============
                    //     |-------------------------------> // removalarea
                    removeTarget.Add(color);
                }
            }
            else if (offset == color.Offset)
            {
                if (offset + removalLength < color.Offset + color.Length)
                {
                    //                      color
                    //               start       last
                    //                 v           v
                    // .   .   .   .   .   .   .   .   .   .   .   .
                    //                 =============
                    //                 |------->     removal area
                    // remove
                    color.Length -= removalLength;
                    // insert
                    color.Offset += insertionLength;
                }
                else
                {
                    //                      color
                    //               start       last
                    //                 v           v
                    // .   .   .   .   .   .   .   .   .   .   .   .
                    //                 =============
                    //                 |-------------------> removal area
                    removeTarget.Add(color);
                }
            }
            else if (offset <= color.Offset + color.Length)
            {
                if (offset + removalLength < color.Offset + color.Length)
                {
                    //                      color
                    //               start       last
                    //                 v           v
                    // .   .   .   .   .   .   .   .   .   .   .   .
                    //                 =============
                    //                     |--->     removal area
                    // remove
                    color.Length -= removalLength;
                    // insert
                    color.Length += insertionLength;
                }
                else
                {
                    //                      color
                    //               start       last
                    //                 v           v
                    // .   .   .   .   .   .   .   .   .   .   .   .
                    //                 =============
                    //                     |---------------> removal area
                    // remove
                    color.Length = offset - color.Offset;
                }
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
