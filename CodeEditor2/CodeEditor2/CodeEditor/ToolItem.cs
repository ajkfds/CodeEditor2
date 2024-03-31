using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor
{
    public class ToolItem
    {
        public ToolItem(string text)
        {
            this.text = text;
        }

        private string text;

        public virtual void Apply(CodeDocument codeDocument)
        {

        }
    }
}
