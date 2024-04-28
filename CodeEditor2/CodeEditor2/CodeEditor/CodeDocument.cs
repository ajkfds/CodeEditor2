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
            initialize();
        }

        public CodeDocument(Data.TextFile textFile, bool textOnly) : this()
        {
            textDocument = new TextDocument();
            initialize();
        }
        public CodeDocument(Data.TextFile textFile)
        {
            textFileRef = new WeakReference<Data.TextFile>(textFile);
            textDocument = new TextDocument();
            initialize();
        }

        public CodeDocument(Data.TextFile textFile, string text) 
        {
            textFileRef = new WeakReference<Data.TextFile>(textFile);
            textDocument = new TextDocument();
            textDocument.Text = text;
            initialize();
        }

        private void initialize()
        {
            ownerThread = System.Threading.Thread.CurrentThread;
            textDocument.SetOwnerThread(System.Threading.Thread.CurrentThread);
            textDocument.TextChanged += TextDocument_TextChanged;
            textDocument.Changed += TextDocument_Changed;
            textDocument.Changing += TextDocument_Changing;
        }

        System.Threading.Thread? ownerThread = null;

        private void CheckThead()
        {
            //if(!HasThread)
            //{
            //    System.Diagnostics.Debugger.Break();
            //}
        }

        private bool HasThread
        {
            get {
                if(System.Threading.Thread.CurrentThread == ownerThread)
                {
                    return true;
                }
                return false;
            }
        }

        public void LockThreadToUI()
        {
            CheckThead();
            textDocument.SetOwnerThread(Global.UIThread);
            ownerThread = Global.UIThread;
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
            if (!IsDirty)
            {
                Version++;
                NavigatePanel.NavigatePanelNode node;
                Controller.NavigatePanel.GetSelectedNode(out node);
                node?.UpdateVisual();
            }
            else
            {
                Version++;
            }
        }


        protected TextDocument textDocument;
        public TextDocument TextDocument
        {
            get
            {
                CheckThead();
                return textDocument;
            }
        }

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
            NavigatePanel.NavigatePanelNode node;
            Controller.NavigatePanel.GetSelectedNode(out node);
            node?.UpdateVisual();
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
                CheckThead();
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

//        public Action<CodeDocument>? SelectionChanged = null;

        internal  int selectionStart;
        public int SelectionStart
        {
            get
            {
                return selectionStart;
            }
            //set
            //{
            //    if (selectionStart == value) return;
            //    selectionStart = value;
            //    if (SelectionChanged != null) SelectionChanged(this);
            //}
        }

        internal int selectionLast;
        public int SelectionLast
        {
            get
            {
                return selectionLast;
            }
            //    set
            //    {
            //        if (selectionLast == value) return;
            //        selectionLast = value;
            //        if (SelectionChanged != null) SelectionChanged(this);
            //    }
        }

        public void SetSelection(int startIndex, int lastIndex)
        {
            if (Global.codeView.CodeDocument != this) return;

            Global.codeView.SetSelection(startIndex, lastIndex);
        }



        int caretIndex;
        public Action<CodeDocument>? CaretChanged = null;
        public int CaretIndex
        {
            get
            {
                return caretIndex;
            }
            set
            {
                caretIndex = value;
                if (CaretChanged != null) CaretChanged(this);
            }
        }



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
            Marks = document.Marks;
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
            SetMarkAt(index, 1, value);
        }

        public List<CodeDrawStyle.MarkInfo> Marks = new List<CodeDrawStyle.MarkInfo>();
        public void SetMarkAt(int index, int length, byte value)
        {
            if (TextDocument == null) return;
            if (TextFile == null) return;
            if (index >= Length) return;

            CodeDrawStyle.MarkInfo markStyle = TextFile.DrawStyle.MarkStyle[value];
            CodeDrawStyle.MarkInfo mark = new CodeDrawStyle.MarkInfo();
            mark.offset = index;
            mark.endOffset = index + length;
            mark.Color = markStyle.Color;
            mark.Thickness = markStyle.Thickness;
            mark.DecorationHeight = markStyle.DecorationHeight;
            mark.DecorationWidth = markStyle.DecorationWidth;
            mark.Style = markStyle.Style;
            Marks.Add(mark);
        }


        public void RemoveMarkAt(int index, byte value)
        {
            if (TextDocument == null) return;
            if (TextFile == null) return;
            //            marks[index] &= (byte)((1 << value) ^ 0xff);
        }
        public void RemoveMarkAt(int index, int length, byte value)
        {
            for (int i = index; i < index + length; i++)
            {
                RemoveMarkAt(i, value);
            }
        }

        #region Text Color
        public Dictionary<int, LineInfomation> LineInfomations = new Dictionary<int, LineInfomation>();
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


        public virtual void SetColorAt(int index, byte value)
        {
            if (TextDocument == null) return;
            if (TextFile == null) return;
            DocumentLine line = TextDocument.GetLineByOffset(index);
            LineInfomation lineInfo = GetLineInfomation(line.LineNumber);
            Color color = TextFile.DrawStyle.ColorPallet[value];
            lineInfo.Colors.Add(new LineInfomation.Color(index, 1, color));
            if (lineInfo.Colors.Count > 2000)
            {
                string a = "";
            }
        }

        public virtual void SetColorAt(int index, byte value, int length)
        {
            if (TextDocument == null) return;
            if (TextFile == null) return;

            DocumentLine lineStart = TextDocument.GetLineByOffset(index);
            DocumentLine lineLast = TextDocument.GetLineByOffset(index + length);
            Color color = TextFile.DrawStyle.ColorPallet[value];

            if (lineStart == lineLast)
            {
                LineInfomation lineInfo = GetLineInfomation(lineStart.LineNumber);
                lineInfo.Colors.Add(new LineInfomation.Color(index, length, color));
            }
            else
            {
                LineInfomation lineInfo = GetLineInfomation(lineStart.LineNumber);
                lineInfo.Colors.Add(new LineInfomation.Color(index,  GetLineLength(lineStart.LineNumber)- (index - GetLineStartIndex(lineStart.LineNumber)), color));

                lineInfo = GetLineInfomation(lineLast.LineNumber);
                lineInfo.Colors.Add(new LineInfomation.Color(GetLineStartIndex(lineLast.LineNumber), index+length- GetLineStartIndex(lineLast.LineNumber), color));

                for (int line = lineStart.LineNumber+1; line <= lineLast.LineNumber-1; line++)
                {
                    lineInfo = GetLineInfomation(line);
                    lineInfo.Colors.Add(new LineInfomation.Color(GetLineStartIndex(line),GetLineLength(line), color));
                }
            }
        }
        #endregion


        //


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
                SetColorAt(index, colorIndex, text.Length);
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
