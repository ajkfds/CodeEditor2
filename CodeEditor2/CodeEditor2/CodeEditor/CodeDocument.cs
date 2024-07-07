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


        [MemberNotNull(nameof(Marks))]
        [MemberNotNull(nameof(TextColors))]
        [MemberNotNull(nameof(HighLights))]
        private void initialize()
        {
            Marks = new Marks(this);
            TextColors = new Colors(this);
            HighLights = new HIghLights(this);
            ownerThread = System.Threading.Thread.CurrentThread;
            textDocument.SetOwnerThread(System.Threading.Thread.CurrentThread);
            textDocument.TextChanged += TextDocument_TextChanged;
            textDocument.Changed += TextDocument_Changed;
            textDocument.Changing += TextDocument_Changing;
        }

        public Marks Marks;
        public Colors TextColors;
        public HIghLights HighLights;

        public Action<object?, DocumentChangeEventArgs>? Changing = null;

        #region ThreadControl

        System.Threading.Thread? ownerThread = null;

        private void CheckThread()
        {
            //if(!HasThread)
            //{
            //    System.Diagnostics.Debugger.Break();
            //}
        }

        private bool HasThread
        {
            get
            {
                if (System.Threading.Thread.CurrentThread == ownerThread)
                {
                    return true;
                }
                return false;
            }
        }

        public void LockThreadToUI()
        {
            CheckThread();
            textDocument.SetOwnerThread(Global.UIThread);
            ownerThread = Global.UIThread;
        }

        #endregion

        #region handle AvaloniaEdit.TextDocument

        protected TextDocument textDocument;
        public TextDocument TextDocument
        {
            get
            {
                CheckThread();
                return textDocument;
            }
        }

        public System.WeakReference<Data.TextFile>? textFileRef;
        public Data.TextFile? TextFile
        {
            get
            {
                Data.TextFile? ret;
                if (textFileRef == null) return null;
                if (!textFileRef.TryGetTarget(out ret)) return null;
                return ret;
            }
        }
        private void TextDocument_Changing(object? sender, DocumentChangeEventArgs e)
        {
            TextColors.OnTextEdit(e);
            HighLights.OnTextEdit(e);
            if(Changing != null) Changing(sender, e);
        }

        private void TextDocument_Changed(object? sender, DocumentChangeEventArgs e)
        {
        }

        private void TextDocument_TextChanged(object? sender, EventArgs e)
        {
            if (!IsDirty)
            {
                Version++;
                NavigatePanel.NavigatePanelNode? node = Controller.NavigatePanel.GetSelectedNode();
                node?.UpdateVisual();
            }
            else
            {
                Version++;
            }
        }

        #endregion

 


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
            NavigatePanel.NavigatePanelNode? node = Controller.NavigatePanel.GetSelectedNode();
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
                CheckThread();
                return textDocument.TextLength;
            }
        }


        // block handling /////////////////////////////

        List<int> blockStartIndexes = new List<int>();
        List<int> blockEndIndexes = new List<int>();

        // block information cash
        bool blockCashActive = false;
        List<int> blockStartLines = new List<int>();
        List<int> blockEndLines = new List<int>();
        private void createBlockCash()
        {
            if (textOnly) return;

            blockStartLines.Clear();
            blockEndLines.Clear();
            for (int i = 0; i < blockStartIndexes.Count; i++)
            {
                blockStartLines.Add(GetLineAt(blockStartIndexes[i]));
                blockEndLines.Add(GetLineAt(blockEndIndexes[i]));
            }
            blockCashActive = true;
        }


        public void ClearBlock()
        {
            blockCashActive = false;
            blockStartIndexes.Clear();
            blockEndIndexes.Clear();
        }
        public void AppendBlock(int startIndex, int endIndex)
        {
            blockCashActive = false;
            blockStartIndexes.Add(startIndex);
            blockEndIndexes.Add(endIndex);
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
        }

        internal int selectionLast;
        public int SelectionLast
        {
            get
            {
                return selectionLast;
            }
        }

        public void SetSelection(int startIndex, int lastIndex)
        {
            if (Global.codeView.CodeDocument != this) return;

            Global.codeView.SetSelection(startIndex, lastIndex);
        }



        internal int caretIndex;
        public int CaretIndex
        {
            get
            {
                return caretIndex;
            }
        }



        public char GetCharAt(int index)
        {
            if(Length <= index)
            {
                return ' ';
            }
            return textDocument.GetCharAt(index);
        }

        public void SetCharAt(int index, char value)
        {
            textDocument.Replace(index, 1, value.ToString());
        }

        public void CopyColorMarkFrom(CodeDocument document)
        {
            TextColors.LineInformation = document.TextColors.LineInformation;
            Marks.marks = document.Marks.marks;
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
                TextColors.SetColorAt(index, colorIndex, text.Length);
                // set color
            }
        }

        public int GetLineAt(int index)
        {
            if (textDocument == null) return 0;
            if (index > Length) return 0;
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
