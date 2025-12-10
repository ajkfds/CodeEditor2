using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodeEditor2.CodeEditor.PopupMenu;

namespace CodeEditor2.Snippets
{
    public class ToUpper : ToolItem
    {
        public ToUpper():base("ToUpper")
        {

        }

        public override void Apply()
        {
            Data.TextFile? file = CodeEditor2.Controller.CodeEditor.GetTextFile();
            if (file == null) return;
            CodeEditor.CodeDocument? codeDocument = file.CodeDocument;
            if (codeDocument == null) return;
            string replaceText = codeDocument.CreateString(codeDocument.SelectionStart, codeDocument.SelectionLast - codeDocument.SelectionStart+1).ToUpper();

            codeDocument.Replace(codeDocument.SelectionStart, codeDocument.SelectionLast - codeDocument.SelectionStart+1, 0, replaceText);
        }

    }
}
