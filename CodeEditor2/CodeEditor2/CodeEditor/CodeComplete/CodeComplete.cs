﻿using CodeEditor2.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor.CodeComplete
{
    public class CodeComplete
    {
        public CodeComplete(CodeView codeView)
        {
            this.codeView = codeView;
        }

        private CodeView codeView;
        private AutoCompleteWindow? _completionWindow;

        /// <summary>
        /// update auto complete word text
        /// </summary>
        public void CheckAutoComplete()
        {
            if (codeView.TextFile == null) return;
            if (codeView.CodeDocument == null) return;


            char? prevChar = null;  // character before caret
            int prevIndex = codeView._textEditor.CaretOffset;
            if (prevIndex != 0)
            {
                prevIndex--;
                prevChar = codeView.CodeDocument.GetCharAt(prevIndex);
            }


            string candidateWord;
            List<AutocompleteItem> items = codeView.TextFile.GetAutoCompleteItems(codeView._textEditor.CaretOffset, out candidateWord);
//            System.Diagnostics.Debug.Print("## GetAutoCompleteItems try");
            if (candidateWord == "")
            {
                if (_completionWindow != null) _completionWindow.Close();
                return;
            }


            if(_completionWindow == null)
            {   // open window
                _completionWindow = new AutoCompleteWindow(codeView._textEditor.TextArea);
                _completionWindow.Closed += (o, args) => _completionWindow_Closed();

                var data = _completionWindow.CompletionList.CompletionData;
                foreach (AutocompleteItem item in items)
                {
                    item.Clean();
                }
                _completionWindow.Show();
                foreach (AutocompleteItem item in items)
                {
                    item.Assign(codeView.CodeDocument);
                    data.Add(item.CreateItemView());
                }
                _completionWindow.StartOffset = prevIndex;
            }
            else
            {   // update
                if (_completionWindow.CompletionList._listBox.ItemCount == 0)
                {
                    _completionWindow.Close();
                    return;
                }
                if (items == null || candidateWord == null || candidateWord == "" & prevChar != '.')
                {
                    _completionWindow.Close();
                    return;
                }
            }
        }

        private void _completionWindow_Closed()
        {
            if (_completionWindow == null) return;
//            _completionWindow.CompletionList._listBox.ItemsSource;
            _completionWindow = null;
            return;
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
                if (_completionWindow != null) _completionWindow.Close();
            }
            else
            {
                _completionWindow = new AutoCompleteWindow(codeView._textEditor.TextArea);
                _completionWindow.Closed += (o, args) => _completionWindow = null;
                var data = _completionWindow.CompletionList.CompletionData;
                _completionWindow.Show();
            }
        }
    }
}
