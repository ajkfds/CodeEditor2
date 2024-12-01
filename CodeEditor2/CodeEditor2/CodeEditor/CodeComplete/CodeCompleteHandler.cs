using Avalonia.Input;
using CodeEditor2.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor.CodeComplete
{
    public class CodeCompleteHandler
    {
        public CodeCompleteHandler(CodeView codeView)
        {
            this.codeView = codeView;
        }

        private CodeView codeView;
        private AutoCompleteWindow? _completionWindow;

        private bool forceOpened = false;

        /// <summary>
        /// update auto complete word text
        /// </summary>
        public void OnTextEntered(TextInputEventArgs e)
        {
            if (codeView.TextFile == null) return;
            if (codeView.CodeDocument == null) return;

            System.Diagnostics.Debug.Print("#=# CodeCompleteHandler.OnTextEntered enter");

            char? prevChar = null;  // character before caret
            int prevIndex = codeView._textEditor.CaretOffset;
            if (prevIndex != 0)
            {
                prevIndex--;
                prevChar = codeView.CodeDocument.GetCharAt(prevIndex);
            }


            string? candidateWord;
            List<AutocompleteItem>? items = codeView.TextFile.GetAutoCompleteItems(codeView._textEditor.CaretOffset, out candidateWord);
            if (items == null ||candidateWord == null) return;

            if (_completionWindow == null)
            {   // open window
                _completionWindow = new AutoCompleteWindow(codeView._textEditor.TextArea);
                _completionWindow.Closed += (o, args) => _completionWindow_Closed();

                var data = _completionWindow.CompletionList.CompletionData;
                data.Clear();
                foreach (AutocompleteItem item in items)
                {
                    item.Clean();
                }
                forceOpened = false;
                _completionWindow.Show();
                foreach (AutocompleteItem item in items)
                {
                    item.Assign(codeView.CodeDocument);
                    data.Add(item.CreateItemView());
                }
                _completionWindow.StartOffset = prevIndex;
                _completionWindow.CompletionList.SelectItem(candidateWord);
            }
            else
            {   // update
                if (candidateWord == "")
                {
                    _completionWindow.Close();
                    System.Diagnostics.Debug.Print("#=# CodeCompleteHandler.OnTextEntered leave1");
                    return;
                }


                if (_completionWindow.CompletionList._listBox.ItemCount == 0)
                {
                    _completionWindow.Close();
                    System.Diagnostics.Debug.Print("#=# CodeCompleteHandler.OnTextEntered leave2");
                    return;
                }
                if (items == null || candidateWord == null || candidateWord == "" & prevChar != '.')
                {
                    _completionWindow.Close();
                    System.Diagnostics.Debug.Print("#=# CodeCompleteHandler.OnTextEntered leave3");
                    return;
                }
            }
            System.Diagnostics.Debug.Print("#=# CodeCompleteHandler.OnTextEntered leave4");
        }

        private void _completionWindow_Closed()
        {
            System.Diagnostics.Debug.Print("#=# CodeCompleteHandler._completionWindow_Closed enter");
            if (_completionWindow == null) return;
            _completionWindow = null;
            System.Diagnostics.Debug.Print("#=# CodeCompleteHandler._completionWindow_Closed leave");
            return;
        }

        public void ForceOpenAutoComplete(List<AutocompleteItem> autocompleteItems)
        {
            System.Diagnostics.Debug.Print("#=# CodeCompleteHandler.ForceOpenAutoComplete enter");
            if (codeView.TextFile == null)
            {
                System.Diagnostics.Debug.Print("#=# CodeCompleteHandler.ForceOpenAutoComplete leave1");
                return;
            }
            CodeDocument? codeDocument = codeView.CodeDocument;
            if (codeDocument == null)
            {
                System.Diagnostics.Debug.Print("#=# CodeCompleteHandler.ForceOpenAutoComplete leave2");
                return;
            }

            int prevIndex = codeDocument.CaretIndex;
            if (codeDocument.GetLineStartIndex(codeDocument.GetLineAt(prevIndex)) != prevIndex && prevIndex != 0)
            {
                prevIndex--;
            }

            forceOpened = true;
            string candidateWord;
            List<AutocompleteItem>? items = codeView.TextFile.GetAutoCompleteItems(codeDocument.CaretIndex, out candidateWord);
            items = autocompleteItems;  // override items
            if (items == null || candidateWord == null)
            {
                if (_completionWindow != null) _completionWindow.Close();
            }
            else
            {
                _completionWindow = new AutoCompleteWindow(codeView._textEditor.TextArea);
                _completionWindow.Closed += (o, args) => _completionWindow = null;
                var data = _completionWindow.CompletionList.CompletionData;
                _completionWindow.Show();
            }
            System.Diagnostics.Debug.Print("#=# CodeCompleteHandler.ForceOpenAutoComplete leave3");
        }
    }
}
