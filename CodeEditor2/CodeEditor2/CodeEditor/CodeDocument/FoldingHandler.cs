using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AvaloniaEdit.Folding;

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

    }
}
