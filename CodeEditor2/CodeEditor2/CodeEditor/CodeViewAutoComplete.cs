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
            string cantidateWord;
            List<AutocompleteItem> items = codeView.TextFile.GetAutoCompleteItems(codeView._textEditor.CaretOffset, out cantidateWord);
            if (cantidateWord == null) return;
            //System.Diagnostics.Debug.Print("## checkAutoComplete " + cantidateWord + " " + cantidateWord.Length);
            //System.Diagnostics.Debug.Print("## checkAutoCompleteCar _" + codeView.TextFile.CodeDocument.GetCharAt(prevIndex) + "_" + prevIndex.ToString());

            if (codeView._completionWindow != null) return;
            //if (CodeDocument.SelectionStart == CodeDocument.SelectionLast)
            {


                if (items == null || cantidateWord == null || cantidateWord == "")
                {
                    if (codeView._completionWindow != null)
                    {
                        codeView._completionWindow.Close();
                    }
                }
                else
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
            int prevIndex = codeView.CodeDocument.CaretIndex;
            if (codeView.CodeDocument.GetLineStartIndex(codeView.CodeDocument.GetLineAt(prevIndex)) != prevIndex && prevIndex != 0)
            {
                prevIndex--;
            }

            string cantidateWord;
            List<AutocompleteItem> items = codeView.TextFile.GetAutoCompleteItems(codeView.CodeDocument.CaretIndex, out cantidateWord);
            items = autocompleteItems;  // override items
            if (items == null || cantidateWord == null)
            {
                if (codeView._completionWindow != null) codeView._completionWindow.Close();
            }
            else
            {
                codeView._completionWindow = new AutoCompleteWindow(codeView._textEditor.TextArea);
                codeView._completionWindow.Closed += (o, args) => codeView._completionWindow = null;
                var data = codeView._completionWindow.CompletionList.CompletionData;
                int machedCount = 0;
                //foreach (AutocompleteItem item in items)
                //{
                //    data.Add(item);
                //    if (item.Text.StartsWith(cantidateWord)) machedCount++;
                //}
                //if(machedCount != 0)
                //{
                //    _completionWindow.Show();
                //}
                codeView._completionWindow.Show();
            }
        }
    }
}
