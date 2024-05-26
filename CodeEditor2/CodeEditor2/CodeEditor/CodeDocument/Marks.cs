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
    public class Marks
    {
		public Marks(CodeDocument codeDocument)
		{
			this.codeDocument = codeDocument;
		}
		CodeDocument codeDocument;

		public List<CodeDrawStyle.MarkDetail> Details = new List<CodeDrawStyle.MarkDetail>();
		public void SetMarkAt(int index, int length, byte value)
		{
			if (codeDocument.TextDocument == null) return;
			if (codeDocument.TextFile == null) return;
			if (index >= codeDocument.Length) return;

			CodeDrawStyle.MarkDetail markStyle = codeDocument.TextFile.DrawStyle.MarkStyle[value];
			CodeDrawStyle.MarkDetail mark = new CodeDrawStyle.MarkDetail();
			mark.Offset = index;
			mark.LastOffset = index + length;
			mark.Color = markStyle.Color;
			mark.Thickness = markStyle.Thickness;
			mark.DecorationHeight = markStyle.DecorationHeight;
			mark.DecorationWidth = markStyle.DecorationWidth;
			mark.Style = markStyle.Style;
			Details.Add(mark);
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

		internal TextSegmentCollection<TextSegment>? CurrentMarks = null;
		public void OnTextEdit(DocumentChangeEventArgs e)
		{
			if (CurrentMarks == null) return;

			int change = e.InsertionLength - e.RemovalLength;
			List<TextSegment> removeTarget = new List<TextSegment>();

			foreach (var mark in CurrentMarks)
			{
				//     start    last
				//       +=======+

				// |---|                 a0
				// |---------|           a1
				// |-------------------| a2

				//           |---|       b0
				//           |---------| b1

				//                  |--| c0

				int start = mark.StartOffset;
				int last = mark.EndOffset;

				if (e.Offset <= start) // a0 | a1 | a2
				{
					if (e.Offset + e.RemovalLength < start)
					{ // a0
						mark.StartOffset += change;
					}
					else if (e.Offset + e.RemovalLength <= last)
					{ // a1
						mark.StartOffset = e.Offset;
					}
					else
					{ // a2
						if (mark.EndOffset + change > mark.StartOffset) mark.EndOffset += change;
						else removeTarget.Add(mark);
					}
				}
				else if (e.Offset <= mark.EndOffset + 1) // b0 | b1
				{
					if (e.Offset + e.RemovalLength <= last + 1)
					{ // b0
						mark.EndOffset += change;
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
				CurrentMarks.Remove(removeMark);
			}
		}



	}
}
