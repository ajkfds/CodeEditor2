using CodeEditor2.CodeEditor.PopupMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.Snippets
{

    public class LlmSnippet : ToolItem
    {
        private static Views.BrowserWindow LLMWindow;
        public LlmSnippet() : base("LLM launch")
        {
        }

        public override void Apply()
        {
            if (LLMWindow == null)
            {
                LLMWindow = new Views.BrowserWindow();
                LLMWindow.Show();
            }

        }
    }
}
