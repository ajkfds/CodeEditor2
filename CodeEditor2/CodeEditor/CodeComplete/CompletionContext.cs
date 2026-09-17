using System;
using System.Collections.Generic;
using System.Text;

namespace CodeEditor2.CodeEditor.CodeComplete
{
    public class CompletionContext
    {

        public string CandidateWord = "";
        public int CandidateStartIndex;
        public List<PopupMenu.ToolItem> AutoCompleteItems = new List<PopupMenu.ToolItem>();
        public List<CodeEditor2.CodeEditor.PopupHint.PopupItem> PopupItems = new List<CodeEditor2.CodeEditor.PopupHint.PopupItem>();


    }
}
