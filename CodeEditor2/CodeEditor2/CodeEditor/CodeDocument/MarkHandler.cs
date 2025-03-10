﻿using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using AvaloniaEdit.Document;
using AvaloniaEdit.TextMate;
using CodeEditor2.NavigatePanel;
using ExCSS;
using HarfBuzzSharp;


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
    public class MarkHandler
    {
		public MarkHandler(CodeDocument codeDocument)
		{
			this.codeDocument = codeDocument;
		}
		CodeDocument codeDocument;

        // Details : Details information Assigned to this CodeDocument
		public List<CodeDrawStyle.MarkDetail> marks = new List<CodeDrawStyle.MarkDetail>();
		public void SetMarkAt(int index, int length, byte value)
		{
			if (codeDocument.TextDocument == null) return;
			if (codeDocument.TextFile == null) return;
			if (index >= codeDocument.Length) return;

			int startLine = codeDocument.GetLineAt(index);
			int lastLine = codeDocument.GetLineAt(index + length);

			if(startLine == lastLine)
			{
				CodeDrawStyle.MarkDetail mark = createDetail(codeDocument.TextFile.DrawStyle.MarkStyle[value]);
                mark.Offset = index;
                mark.LastOffset = index + length;
                lock (marks)
                {
                    marks.Add(mark);
                }
				return;
            }

			{ // startLine
                CodeDrawStyle.MarkDetail mark = createDetail(codeDocument.TextFile.DrawStyle.MarkStyle[value]);
				mark.Offset = index;
                mark.LastOffset = codeDocument.GetLineStartIndex(startLine) + codeDocument.GetLineLength(startLine);

                lock (marks)
                {
                    marks.Add(mark);
                }
            }
            for (int i = startLine + 1; i < lastLine; i++)
			{
                CodeDrawStyle.MarkDetail mark = createDetail(codeDocument.TextFile.DrawStyle.MarkStyle[value]);
				mark.Offset = codeDocument.GetLineStartIndex(i);
				mark.LastOffset = mark.Offset + codeDocument.GetLineLength(i);

                lock (marks)
                {
                    marks.Add(mark);
                }
			}
			{ // last line
                CodeDrawStyle.MarkDetail mark = createDetail(codeDocument.TextFile.DrawStyle.MarkStyle[value]);
				mark.Offset = codeDocument.GetLineStartIndex(lastLine);
				mark.LastOffset = index + length;

                lock (marks)
                {
                    marks.Add(mark);
                }
            }
        }
		private CodeDrawStyle.MarkDetail createDetail(CodeDrawStyle.MarkDetail markStyle)
		{
            CodeDrawStyle.MarkDetail mark = new CodeDrawStyle.MarkDetail();
            mark.Color = markStyle.Color;
            mark.Thickness = markStyle.Thickness;
            mark.DecorationHeight = markStyle.DecorationHeight;
            mark.DecorationWidth = markStyle.DecorationWidth;
            mark.Style = markStyle.Style;
			return mark;
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

		public void RemoveMarkAt(int index, byte value)
		{
			if (codeDocument.TextDocument == null) return;
			if (codeDocument.TextFile == null) return;
			//            marks[index] &= (byte)((1 << value) ^ 0xff);
		}
		public void RemoveMarkAt(int index, int length, byte value)
		{
			for (int i = index; i < index + length; i++)
			{
				RemoveMarkAt(i, value);
			}
		}

		public void RemoveMarks()
		{
//            if (CurrentMarks == null) return;
//            lock (CurrentMarks)
//            {
              marks.Clear();
//            }
        }


	}
}
