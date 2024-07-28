using CodeEditor2.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor
{
    public class CodeViewAutoComplete
    {
        public CodeViewAutoComplete(CodeView codeView)
        {
            this.codeView = codeView;
        }

        private CodeView codeView;

        /// <summary>
        /// update auto complete word text
        /// </summary>
        public void CheckAutoComplete()
        {
            if (codeView.TextFile == null) return;
            int prevIndex = codeView._textEditor.CaretOffset;

            if (prevIndex != 0)
            {
                prevIndex--;
            }
            char prevChar = codeView.CodeDocument.GetCharAt(prevIndex);

            string candidateWord;
            List<AutocompleteItem> items = codeView.TextFile.GetAutoCompleteItems(codeView._textEditor.CaretOffset, out candidateWord);
            System.Diagnostics.Debug.Print("## GetAutoCompleteItems try");
            if (candidateWord == "") return;

            System.Diagnostics.Debug.Print("## GetAutoCompleteItems "+candidateWord+","+items.Count);

            if (codeView._completionWindow != null)
            {
                if (codeView._completionWindow.CompletionList._listBox.ItemCount == 0)
                {
                    codeView._completionWindow.Close();
                    return;
                }
                if (items == null || candidateWord == null || (candidateWord == "" & prevChar != '.'))
                {
                    codeView._completionWindow.Close();
                    return;
                }
            }

            if (codeView._completionWindow != null) return;
            //if (CodeDocument.SelectionStart == CodeDocument.SelectionLast)
            {


                {
                    codeView._completionWindow = new CodeEditor2.CodeEditor.AutoCompleteWindow(codeView._textEditor.TextArea);
                    codeView._completionWindow.Closed += (o, args) => codeView._completionWindow = null;
                    var data = codeView._completionWindow.CompletionList.CompletionData;
                   foreach (AutocompleteItem item in items)
                    {
                        item.Clean();
                    }
                    codeView._completionWindow.Show();
                    foreach (AutocompleteItem item in items)
                    {
                        item.Assign(codeView.CodeDocument);
                        data.Add(item);
                    }
                    codeView._completionWindow.StartOffset = prevIndex;
                }
            }
        }

        public void ForceOpenAutoComplete(List<AutocompleteItem> autocompleteItems)
        {
            if (codeView.TextFile == null) return;
            CodeDocument? codeDocument = codeView.CodeDocument;
            if (codeDocument == null) return;

            int prevIndex = codeDocument.CaretIndex;
            if (codeDocument.GetLineStartIndex(codeDocument.GetLineAt(prevIndex)) != prevIndex && prevIndex != 0)
            {
                prevIndex--;
            }

            string candidateWord;
            List<AutocompleteItem> items = codeView.TextFile.GetAutoCompleteItems(codeDocument.CaretIndex, out candidateWord);
            items = autocompleteItems;  // override items
            if (items == null || candidateWord == null)
            {
                if (codeView._completionWindow != null) codeView._completionWindow.Close();
            }
            else
            {
                codeView._completionWindow = new AutoCompleteWindow(codeView._textEditor.TextArea);
                codeView._completionWindow.Closed += (o, args) => codeView._completionWindow = null;
                var data = codeView._completionWindow.CompletionList.CompletionData;
                codeView._completionWindow.Show();
            }
        }
    }
}
