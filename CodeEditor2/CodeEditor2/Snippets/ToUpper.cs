using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.Snippets
{
    public class ToUpper : CodeEditor.ToolItem
    {
        public ToUpper():base("ToUpper")
        {

        }

        public override void Apply()
        {
            Data.TextFile? file = CodeEditor2.Controller.CodeEditor.GetTextFile();
            if (file == null) return;
            CodeEditor.CodeDocument codeDocument = file.CodeDocument;

            string replaceText = codeDocument.CreateString(codeDocument.SelectionStart, codeDocument.SelectionLast - codeDocument.SelectionStart).ToUpper();

            codeDocument.Replace(codeDocument.SelectionStart, codeDocument.SelectionLast - codeDocument.SelectionStart, 0, replaceText);
        }

    }
}
