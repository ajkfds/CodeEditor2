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

        public override async Task ApplyAsync()
        {
            Data.TextFile? file = await CodeEditor2.Controller.CodeEditor.GetTextFileAsync();
            if (file == null) return;
            CodeEditor.CodeDocument? codeDocument = file.CodeDocument;
            if (codeDocument == null) return;
            string replaceText = codeDocument.CreateString(codeDocument.SelectionStart, codeDocument.SelectionLast - codeDocument.SelectionStart+1).ToUpper();

            codeDocument.Replace(codeDocument.SelectionStart, codeDocument.SelectionLast - codeDocument.SelectionStart+1, 0, replaceText);
        }

    }
}
