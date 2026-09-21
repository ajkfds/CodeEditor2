using System;
using System.Collections.Generic;
using System.Text;

namespace CodeEditor2.CodeEditor.CodeComplete
{
    public class CompletionContext
    {

        public string CandidateWord = "";
        public int CandidateStartIndex;

        // auto complete items for the current candidate word, this is used to show the auto complete popup menu
        public List<PopupMenu.ToolItem> AutoCompleteItems = new List<PopupMenu.ToolItem>();

        // hint popup items for the current candidate word, this is used to show the hint popup
        // on the caret when the user types a word (input-time hint, anchored to the caret).
        public List<CodeEditor2.CodeEditor.PopupHint.PopupItem> CarletPopupItems = new List<CodeEditor2.CodeEditor.PopupHint.PopupItem>();

        // hint popup items for the current candidate word, this is used to show the hint popup
        // when the user hovers over a word (mouse-over hint, anchored to the pointer).
        public List<CodeEditor2.CodeEditor.PopupHint.PopupItem> MouseOverPopupItems = new List<CodeEditor2.CodeEditor.PopupHint.PopupItem>();


    }
}
