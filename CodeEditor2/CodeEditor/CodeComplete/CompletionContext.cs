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
        public List<AjkAvaloniaLibs.Controls.ColorLabel> PopupLabels = new List<AjkAvaloniaLibs.Controls.ColorLabel>();


    }
}
