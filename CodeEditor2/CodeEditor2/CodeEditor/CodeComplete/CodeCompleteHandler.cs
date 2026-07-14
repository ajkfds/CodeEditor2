using Avalonia.Input;
using CodeEditor2.Views;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace CodeEditor2.CodeEditor.CodeComplete
{
    public class CodeCompleteHandler
    {
        public CodeCompleteHandler(CodeView codeView)
        {
            this.codeView = codeView;
        }

        private CodeView codeView;
        //        private AutoCompleteWindow? _completionWindow;
        private PopupMenuView? popupMenuView = null;
        private bool working = false;


        public void Close()
        {
            if (popupMenuView == null) return;
            popupMenuView.Cancel();
            working = false;
        }

        public void KeyDown(object? sender, KeyEventArgs e)
        {
            if (!working) return;
            if (codeView.TextFile == null) return;
            if (codeView.CodeDocument == null) return;
            if (popupMenuView == null) return;

            if (e.Key == Key.Up)
            {
                popupMenuView.SelectUp();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                popupMenuView.SelectDown();
                e.Handled = true;
            }
            else if (e.Key == Key.OemComma)
            {
                Apply();
                Close();
                return;
            }
            else if (e.Key == Key.Enter)
            {
                Apply();
                Close();
                e.Handled = true;
                return;
            }
            else if (e.Key == Key.Tab)
            {
                Apply();
                Close();
                e.Handled = true;
                return;
            }
            else if (e.Key == Key.Space)
            {
                Apply();
                Close();
                return;
            }
            else if (e.Key == Key.Escape)
            {
                Close();
                return;
            }
        }

        public void Apply()
        {
            if (codeView.TextFile == null) return;
            if (codeView.CodeDocument == null) return;
            if (popupMenuView == null) return;

            PopupMenu.PopupMenuItem? popupMenuItem = popupMenuView.GetSlectedItem();
            if (popupMenuItem == null) return;
            popupMenuItem.OnSelected();

        }

        public void TextEntered(object? sender, TextInputEventArgs e)
        {
            System.Diagnostics.Debug.Print("### TextEnteted " + working.ToString());
            if (codeView.TextFile == null) return;
            if (codeView.CodeDocument == null) return;

            char? prevChar = null;  // character before caret
            int prevIndex = codeView._textEditor.CaretOffset;
            if (prevIndex != 0)
            {
                prevIndex--;
                prevChar = codeView.CodeDocument.GetCharAt(prevIndex);
            }

            string? candidateWord;
            List<AutocompleteItem>? items = codeView.TextFile.GetAutoCompleteItems(codeView._textEditor.CaretOffset, out candidateWord);
            if (items == null || candidateWord == null)
            {
                Close();
                return;
            }
            if (candidateWord == "" & prevChar != '.')
            {
                Close();
                return;
            }

            List<PopupMenu.ToolItem> toolItems = new List<PopupMenu.ToolItem>();
            foreach (AutocompleteItem aItem in items)
            {
                if (candidateWord.Length < 1 || aItem.Text.StartsWith(candidateWord))
                {
                    aItem.Assign(codeView.CodeDocument);
                    toolItems.Add(aItem);
                }
            }

            if (toolItems.Count == 0)
            {
                Close();
                return;
            }

            if (!working)
            {
                popupMenuView = Controller.CodeEditor.OpenAutoComplete(toolItems);
                if (popupMenuView == null)
                {
                    Close();
                    working = false;
                    return;
                }
                popupMenuView.SelectDown();
                working = true;
            }
            else
            {
                if (popupMenuView == null)
                {
                    Close();
                    return;
                }
                Controller.CodeEditor.UpdateAutoComplete(toolItems);
                popupMenuView.SelectDown();
                working = true;
            }
        }

        public void UpdateMenu()
        {

        }



        public void ForceOpenAutoComplete(List<AutocompleteItem> autocompleteItems)
        {
        }


    }
}
