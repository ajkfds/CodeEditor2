using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using CodeEditor2.NavigatePanel;
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
using static System.Net.Mime.MediaTypeNames;


namespace CodeEditor2.CodeEditor
{
    public interface ICodeDocument
    {

        public void Dispose() { }

        public bool IsDisposed { get; }

        public void Clean() { }

        public void ClearHistory() { }

        public bool IsDirty { get; }

        public ulong Version { get; set; }

        public ulong CleanVersion { get; }


        public int Length { get; }
        public void AppendBlock(int startIndex, int endIndex) { }
        public void AppendBlock(int startIndex, int endIndex, string name, bool defaultClose) { }

        /////////////////////////////////////////

        public int SelectionStart { get; }

        public int SelectionLast { get; }

        public void SetSelection(int startIndex, int lastIndex);
        public int CaretIndex { get; }

        public char GetCharAt(int index);

        public void SetCharAt(int index, char value);

        public void CopyColorMarkFrom(CodeDocument document);
        public void CopyFrom(CodeDocument document);
        public void CopyTextOnlyFrom(CodeDocument document);

        public void ClearColorMark();
        public void Replace(int index, int replaceLength, byte colorIndex, string text);
        public int GetLineAt(int index);


        public int GetLineStartIndex(int line);

        public int GetLineLength(int line);

        public int Lines { get; }

        public int FindIndexOf(string targetString, int startIndex);
        public int FindPreviousIndexOf(string targetString, int startIndex);

        public string CreateString();
        public string CreateString(int index, int length);

        public string CreateLineString(int line);

        public void GetWord(int index, out int headIndex, out int length);
    }
}
