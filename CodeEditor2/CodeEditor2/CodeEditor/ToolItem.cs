using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor
{
    public class ToolItem : PopupMenuItem
    {
        public ToolItem(string text) : base(text)
        {
        }

        public virtual void Apply(CodeDocument codeDocument)
        {

        }
    }
}
