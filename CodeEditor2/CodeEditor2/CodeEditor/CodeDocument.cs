﻿using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor
{
    public class CodeDocument : IDisposable
    {
        public CodeDocument()
        {
            textDocument = new TextDocument();
            textDocument.SetOwnerThread(System.Threading.Thread.CurrentThread);
            textDocument.TextChanged += TextDocument_TextChanged;
            textDocument.Changed += TextDocument_Changed;
            textDocument.Changing += TextDocument_Changing;
        }

        private void TextDocument_Changing(object? sender, DocumentChangeEventArgs e)
        {
//            System.Diagnostics.Debug.Print("## TextDocument_Changing");
        }

        private void TextDocument_Changed(object? sender, DocumentChangeEventArgs e)
        {
//            System.Diagnostics.Debug.Print("## TextDocument_Changed");
        }

        private void TextDocument_TextChanged(object? sender, EventArgs e)
        {
//            System.Diagnostics.Debug.Print("## TextDocument_TextChanged");
            Version++;
        }

        public CodeDocument(Data.TextFile textFile, bool textOnly) : this()
        {

        }
        public CodeDocument(Data.TextFile textFile) : this()
        {
            textFileRef = new WeakReference<Data.TextFile>(textFile);
        }

        public CodeDocument(Data.TextFile textFile, string text) : this()
        {
            textFileRef = new WeakReference<Data.TextFile>(textFile);
        }

        protected TextDocument textDocument;
        public TextDocument TextDocument
        {
            get
            {
                return textDocument;
            }
        }

        //public void LockThread()
        //{
        //    textDocument.SetOwnerThread(System.Threading.Thread.CurrentThread);
        //}

        public void LockThreadToUI()
        {
            textDocument.SetOwnerThread(Global.UIThread);
        }

        //public void UnlockThread()
        //{
        //    textDocument.SetOwnerThread(null);
        //}


        public System.WeakReference<Data.TextFile>? textFileRef;
        public Data.TextFile TextFile
        {
            get
            {
                Data.TextFile ret;
                if (textFileRef == null) return null;
                if (!textFileRef.TryGetTarget(out ret)) return null;
                return ret;
            }
        }

        private readonly bool textOnly = false;
        private bool disposed = false;
        public void Dispose()
        {
            //if (Global.mainForm.editorPage.CodeEditor.codeTextbox.Document == this)
            //{
            //    Global.mainForm.editorPage.CodeEditor.codeTextbox.Document = null;
            //}
            disposed = true;
            //chars.Dispose();
            if (!textOnly)
            {
                //colors.Dispose();
                //marks.Dispose();
                //newLineIndex.Dispose();
                //lineVisible.Dispose();
            }
        }

        public bool IsDisposed
        {
            get
            {
                return disposed;
            }
        }

        public void Clean()
        {
            CleanVersion = Version;
        }

        public bool IsDirty
        {
            get
            {
                if (CleanVersion == Version) return false;
                return true;
            }
        }

        public Action<int, int, byte, string>? Replaced = null;


        private int visibleLines = 0;
        List<int> collapsedLines = new List<int>();

        public virtual ulong Version { get; set; } = 0;

        public ulong CleanVersion { get; private set; } = 0;

        List<History> histories = new List<History>();
        public int HistoryMaxLimit = 100;

        public class History
        {
            public History(int index, int length, string changedFrom)
            {
                Index = index;
                Length = length;
                ChangedFrom = changedFrom;
            }
            public readonly int Index;
            public readonly int Length;
            public readonly string ChangedFrom;
        }

        public string _tag = "";

        public int Length
        {
            get
            {
                return textDocument.TextLength;
            }
        }


        // block handling /////////////////////////////

        List<int> blockStartIndexs = new List<int>();
        List<int> blockEndIndexs = new List<int>();

        // block infomation cash
        bool blockCashActive = false;
        List<int> blockStartLines = new List<int>();
        List<int> blockEndLines = new List<int>();
        private void createBlockCash()
        {
            if (textOnly) return;

            blockStartLines.Clear();
            blockEndLines.Clear();
            for (int i = 0; i < blockStartIndexs.Count; i++)
            {
                blockStartLines.Add(GetLineAt(blockStartIndexs[i]));
                blockEndLines.Add(GetLineAt(blockEndIndexs[i]));
            }
            blockCashActive = true;
        }



        public int VisibleLines
        {
            get
            {
                return visibleLines;
            }
        }

        //public int GetVisibleLineNo(int lineNo)
        //{
        //    if (textOnly) return 0;

        //    if (!blockCashActive) createBlockCash();
        //    if (collapsedLines.Count == 0) return lineNo;
        //    int vline = 0;
        //    for (int i = 0; i < lineNo; i++)
        //    {
        //        if (lineVisible[i]) vline++;
        //    }
        //    return vline;
        //}

        //public int GetActialLineNo(int visibleLineNo)
        //{
        //    if (textOnly) return 0;

        //    if (!blockCashActive) createBlockCash();
        //    if (collapsedLines.Count == 0) return visibleLineNo;
        //    int lineNo = 0;
        //    int vLine = 0;
        //    for (lineNo = 0; lineNo < Lines; lineNo++)
        //    {
        //        if (lineVisible[lineNo]) vLine++;
        //        if (visibleLineNo == vLine) break;
        //    }
        //    if (lineNo == 0) lineNo = 1;
        //    return lineNo;
        //}

        public void ClearBlock()
        {
            blockCashActive = false;
            blockStartIndexs.Clear();
            blockEndIndexs.Clear();
        }
        public void AppendBlock(int startIndex, int endIndex)
        {
            blockCashActive = false;
            blockStartIndexs.Add(startIndex);
            blockEndIndexs.Add(endIndex);
        }

        //public bool IsVisibleLine(int lineNo)
        //{
        //    if (!blockCashActive) createBlockCash();
        //    return lineVisible[lineNo - 1];
        //}
        public bool IsBlockHeadLine(int lineNo)
        {
            if (!blockCashActive) createBlockCash();
            return blockStartLines.Contains(lineNo);
        }

        //public void CollapseBlock(int lineNo)
        //{
        //    if (!blockCashActive) createBlockCash();
        //    if (!blockStartLines.Contains(lineNo)) return;
        //    if (!collapsedLines.Contains(lineNo))
        //    {
        //        collapsedLines.Add(lineNo);
        //        refreshVisibleLines();
        //    }
        //}

        //public void ExpandBlock(int lineNo)
        //{
        //    if (!blockCashActive) createBlockCash();
        //    if (!blockStartLines.Contains(lineNo)) return;
        //    if (collapsedLines.Contains(lineNo))
        //    {
        //        collapsedLines.Remove(lineNo);
        //        refreshVisibleLines();
        //    }
        //}

        public bool IsCollapsed(int lineNo)
        {
            if (!blockStartLines.Contains(lineNo)) return false;
            if (collapsedLines.Contains(lineNo)) return true;
            return false;
        }

        /////////////////////////////////////////

        int selectionStart;
        public int SelectionStart
        {
            get
            {
                return selectionStart;
            }
            set
            {
                selectionStart = value;
            }
        }

        int selectionLast;
        public int SelectionLast
        {
            get
            {
                return selectionLast;
            }
            set
            {
                selectionLast = value;
            }
        }

        int caretIndex;
        public int CaretIndex
        {
            get
            {
                return caretIndex;
            }
            set
            {
                caretIndex = value;
                if (CarletChanged != null) CarletChanged(this);
            }
        }

        public Action<CodeDocument>? CarletChanged  = null;


        public char GetCharAt(int index)
        {
            return textDocument.GetCharAt(index);
        }

        public void SetCharAt(int index, char value)
        {
            textDocument.Replace(index, 1, value.ToString());
        }

        public void CopyColorMarkFrom(CodeDocument document)
        {
            LineInfomations = document.LineInfomations;
        }
        public void CopyFrom(CodeDocument document)
        {
            textDocument.Text = document.textDocument.Text;
        }

        public void CopyTextOnlyFrom(CodeDocument document)
        {
            var snap = document.textDocument.CreateSnapshot();
            textDocument.Text = snap.Text;
            Version = document.Version;
        }

        public byte GetMarkAt(int index)
        {
            //            return marks[index];
            return 0;
        }

        public virtual void SetMarkAt(int index, byte value)
        {
            if (index >= Length) return;
            if (TextDocument == null) return;
            DocumentLine line = TextDocument.GetLineByOffset(index);
            LineInfomation lineInfo = GetLineInfomation(line.LineNumber);
            Color color = Global.DefaultDrawStyle.MarkColor[value];
            lineInfo.Effects.Add(new LineInfomation.Effect(index, 1, color, null));
        }

        public void SetMarkAt(int index, int length, byte value)
        {
            for (int i = index; i < index + length; i++)
            {
                SetMarkAt(i, value);
            }
        }


        public void RemoveMarkAt(int index, byte value)
        {
//            marks[index] &= (byte)((1 << value) ^ 0xff);
        }
        public void RemoveMarkAt(int index, int length, byte value)
        {
            for (int i = index; i < index + length; i++)
            {
                RemoveMarkAt(i, value);
            }
        }

        public Dictionary<int, LineInfomation> LineInfomations = new Dictionary<int, LineInfomation>();

        public byte GetColorAt(int index)
        {
            //            return colors[index];
            return 0;
        }


        public virtual void SetColorAt(int index, byte value)
        {
            if (TextDocument == null) return;
            DocumentLine line = TextDocument.GetLineByOffset(index);
            LineInfomation lineInfo = GetLineInfomation(line.LineNumber);
            Color color = Global.DefaultDrawStyle.ColorPallet[value];
            lineInfo.Colors.Add(new LineInfomation.Color(index, 1, color));
        }

        public virtual void SetColorAt(int index, byte value, int length)
        {
            if (TextDocument == null) return;

            DocumentLine lineStart = TextDocument.GetLineByOffset(index);
            DocumentLine lineLast = TextDocument.GetLineByOffset(index + index);
            Color color = Global.DefaultDrawStyle.ColorPallet[value];

            if (lineStart == lineLast)
            {
                LineInfomation lineInfo = GetLineInfomation(lineStart.LineNumber);
                lineInfo.Colors.Add(new LineInfomation.Color(index, length, color));
            }
            else
            {
                for(int line = lineStart.LineNumber; line <= lineLast.LineNumber; line++)
                {
                    LineInfomation lineInfo = GetLineInfomation(line);
                    lineInfo.Colors.Add(new LineInfomation.Color(index, index + length, color));
                }
            }
        }

        protected LineInfomation GetLineInfomation(int lineNumber)
        {
            LineInfomation lineInfo;
            if (LineInfomations.ContainsKey(lineNumber))
            {
                lineInfo = LineInfomations[lineNumber];
            }
            else
            {
                lineInfo = new LineInfomation();
                LineInfomations.Add(lineNumber, lineInfo);
            }
            return lineInfo;
        }


        public void Undo()
        {
            //lock (this)
            //{
            //    if (histories.Count == 0) return;
            //    History history = histories.Last();
            //    histories.RemoveAt(histories.Count - 1);
            //    Version--;
            //    replace(history.Index, history.Length, 0, history.ChangedFrom);
            //}
        }

        public void ClearHistory()
        {
            histories.Clear();
        }

        public void Replace(int index, int replaceLength, byte colorIndex, string text)
        {
            if (textDocument == null) return;
            lock (this)
            {
                textDocument.Replace(index, replaceLength, text);
                // set color
            }
        }

        public int GetLineAt(int index)
        {
            if (textDocument == null) return 0;
            return textDocument.GetLineByOffset(index).LineNumber;
        }

        //public int GetVisibleLine(int line)
        //{
        //    lock (this)
        //    {
        //        int visibleLine = 0;
        //        for (int l = 0; l < line; l++)
        //        {
        //            if (lineVisible[l]) visibleLine++;
        //        }
        //        return visibleLine;
        //    }
        //}


        public int GetLineStartIndex(int line)
        {
            if (textDocument == null) return 0;
            TextLocation location = new TextLocation(line, 0);
            return textDocument.GetOffset(location);
        }

        public int GetLineLength(int line)
        {
            if (textDocument == null) return 0;
            return textDocument.GetLineByNumber(line).Length;
        }

        public int Lines
        {
            get
            {
                if (textDocument == null) return 0;
                return textDocument.LineCount;
            }
        }

        public int FindIndexOf(string targetString, int startIndex)
        {
            if (textDocument == null) return -1;
            if (targetString.Length == 0) return -1;
            for (int i = startIndex; i < Length - targetString.Length; i++)
            {
                if (targetString[0] != textDocument.GetCharAt(i)) continue;
                bool match = true;
                for (int j = 1; j < targetString.Length; j++)
                {
                    if (targetString[j] != textDocument.GetCharAt(i + j))
                    {
                        match = false;
                        break;
                    }
                }
                if (match) return i;
            }
            return -1;
        }

        public int FindPreviousIndexOf(string targetString, int startIndex)
        {
            lock (this)
            {
                if (targetString.Length == 0) return -1;
                if (startIndex > Length - targetString.Length) startIndex = Length - targetString.Length;

                for (int i = startIndex; i >= 0; i--)
                {
                    if (targetString[0] != textDocument.GetCharAt(i)) continue;
                    bool match = true;
                    for (int j = 1; j < targetString.Length; j++)
                    {
                        if (targetString[j] != textDocument.GetCharAt(i + j))
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match) return i;
                }
                return -1;
            }
        }

        public string CreateString()
        {
            return textDocument.GetText(0, textDocument.TextLength);
        }

        public string CreateString(int index, int length)
        {
            return textDocument.GetText(index, length);
        }

        //public char[] CreateCharArray()
        //{
        //    return chars.CreateArray();
        //}

        public string CreateLineString(int line)
        {
            return textDocument.GetText(GetLineStartIndex(line), GetLineLength(line));
        }

        //public char[] CreateLineArray(int line)
        //{
        //    unsafe
        //    {
        //        char[] array = chars.CreateArray(GetLineStartIndex(line), GetLineLength(line));
        //        return array;
        //    }
        //}

        public virtual void GetWord(int index, out int headIndex, out int length)
        {
            lock (this)
            {
                headIndex = index;
                length = 0;
                char ch = GetCharAt(index);
                if (ch == ' ' || ch == '\r' || ch == '\n' || ch == '\t') return;

                while (headIndex > 0)
                {
                    ch = GetCharAt(headIndex);
                    if (ch == ' ' || ch == '\r' || ch == '\n' || ch == '\t')
                    {
                        break;
                    }
                    headIndex--;
                }
                headIndex++;

                while (headIndex + length < Length)
                {
                    ch = GetCharAt(headIndex + length);
                    if (ch == ' ' || ch == '\r' || ch == '\n' || ch == '\t')
                    {
                        break;
                    }
                    length++;
                }
            }
        }


    }
}
