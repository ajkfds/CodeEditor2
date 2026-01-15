using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Document;

namespace CodeEditor2.CodeEditor
{
    public class FoldingHandler
    {
        public FoldingHandler(CodeDocument codeDocument)
        {
            this.codeDocument = codeDocument;
        }
        CodeDocument codeDocument;

        public List<NewFolding> Foldings = new List<NewFolding>();

        public void AppendBlock(int startIndex, int endIndex)
        {
            Foldings.Add(new NewFolding(startIndex, endIndex));
        }
        public void AppendBlock(int startIndex, int endIndex,bool defaultClosed)
        {
            NewFolding folding = new NewFolding(startIndex, endIndex) { DefaultClosed = default };
            Foldings.Add(folding);
        }
        public void AppendBlock(int startIndex, int endIndex,string blockName)
        {
            Foldings.Add(new NewFolding(startIndex, endIndex) { Name = blockName });
        }

        public void AppendBlock(int startIndex, int endIndex, string blockName, bool defaultClosed)
        {
            NewFolding folding = new NewFolding(startIndex, endIndex) { Name = blockName,DefaultClosed = defaultClosed };
            Foldings.Add(folding);
        }

        public void OnTextEdit(DocumentChangeEventArgs e)
        {
            if (Foldings.Count == 0) return;

            int change = e.InsertionLength - e.RemovalLength;

            for (int i = 0; i < Foldings.Count; i++)
            {
                //     start    last
                //       +=======+

                // |---|                 a0
                // |---------|           a1
                // |-------------------| a2

                //           |---|       b0
                //           |---------| b1

                //                  |--| c0

                int start = Foldings[i].StartOffset;
                int last = Foldings[i].EndOffset-1;

                if (e.Offset <= start) // a0 | a1 | a2
                {
                    if (e.Offset + e.RemovalLength < start)
                    { // a0
                        Foldings[i].StartOffset += change;
                        Foldings[i].EndOffset += change;
                    }
                    else if (e.Offset + e.RemovalLength <= last)
                    { // a1
                        Foldings[i].StartOffset = e.Offset;
                        Foldings[i].EndOffset = e.Offset + change;
                    }
                    else
                    { // a2
                        Foldings[i].EndOffset += change;
                    }
                }
                else if (e.Offset <= Foldings[i].EndOffset + 1) // b0 | b1
                {
                    if (e.Offset + e.RemovalLength <= last + 1)
                    { // b0
                        Foldings[i].EndOffset += change;
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

        }

    }
}
