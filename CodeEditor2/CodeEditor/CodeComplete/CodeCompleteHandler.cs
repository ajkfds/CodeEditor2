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
        private bool hintWorking = false;

        public void Close()
        {
            CloseHint();

            if (popupMenuView == null) return;
            popupMenuView.Cancel();

            working = false;
        }

        public void CloseHint()
        {
            hintWorking = false;
            Controller.CodeEditor.ClosePopup();
        }

        public void KeyDown(object? sender, KeyEventArgs e)
        {
            if (hintWorking)
            {
                if(e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right)
                {
                    CloseHint();
                }
            }

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
                if (hintWorking) CloseHint();
                Apply();
                Close();
                return;
            }
            else if (e.Key == Key.Enter)
            {
                if (hintWorking) CloseHint();
                Apply();
                Close();
                e.Handled = true;
                return;
            }
            else if (e.Key == Key.Tab)
            {
                if (hintWorking) CloseHint();
                Apply();
                Close();
                e.Handled = true;
                return;
            }
            else if (e.Key == Key.Space)
            {
                if (hintWorking) CloseHint();
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
            Controller.CodeEditor.ClosePopup();
            if (codeView.TextFile == null) return;
            if (codeView.CodeDocument == null) return;
            if (popupMenuView == null) return;

            PopupMenu.PopupMenuItem? popupMenuItem = popupMenuView.GetSlectedItem();
            if (popupMenuItem == null) return;
            popupMenuItem.OnSelected();
        }

        public void TextEntered(object? sender, TextInputEventArgs e)
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

            CompletionContext? completionContext = codeView.TextFile.GetAutoCompleteItems(codeView._textEditor.CaretOffset);
            if(completionContext== null)
            {
                Close();
                return;
            }
            // Input-time hint popup (anchored to the caret).
            // Show the carlet popup when there is at least one popup item and the
            // popup-menu (auto-complete) is not currently working, so that the hint
            // does not collide with the auto-complete dropdown.
            if (completionContext.CarletPopupItems.Count == 0)
            {
                if (!working) CloseHint();
            }
            else
            {
                Controller.CodeEditor.OpenPopup(completionContext.CarletPopupItems);
                hintWorking = true;
            }

            List<PopupMenu.ToolItem>? items = completionContext.AutoCompleteItems;
            if (items == null || completionContext.CandidateWord == null)
            {
                Close();
                return;
            }
            if (completionContext.CandidateWord == "" & prevChar != '.')
            {
                Close();
                return;
            }

            List<PopupMenu.ToolItem> toolItems = new List<PopupMenu.ToolItem>();
            foreach (PopupMenu.ToolItem aItem in items)
            {
                if (completionContext.CandidateWord.Length < 1 || aItem.Text.StartsWith(completionContext.CandidateWord))
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
