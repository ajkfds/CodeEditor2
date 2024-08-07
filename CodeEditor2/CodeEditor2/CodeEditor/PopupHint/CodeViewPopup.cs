using Avalonia.Controls;
using Avalonia.Input;
using CodeEditor2.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Rendering;

namespace CodeEditor2.CodeEditor.PopupHint
{
    public class CodeViewPopup
    {
        // Popup-Hint Handler
        //
        // show mouse over popup-hinting
        public CodeViewPopup(CodeView codeView)
        {
            this.codeView = codeView;
        }

        CodeView codeView;

        private int popupInex = -1;
        public void TextArea_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (codeView.CodeDocument == null || codeView.TextFile == null)
            {
                ToolTip.SetIsOpen(codeView.Editor, false);
                return;
            }
            Avalonia.Point point = e.GetPosition(codeView._textEditor.TextArea);
            var pos = codeView._textEditor.GetPositionFromPoint(point);
            if (pos == null)
            {
                ToolTip.SetIsOpen(codeView.Editor, false);
                return;
            }

            TextViewPosition tpos = (TextViewPosition)pos;
            if (tpos.Line > codeView.CodeDocument.Lines)
            {
                ToolTip.SetIsOpen(codeView.Editor, false);
                return;
            }
            int index = codeView.CodeDocument.TextDocument.GetOffset(tpos.Line, tpos.Column);
            if (index >= codeView.CodeDocument.Length)
            {
                ToolTip.SetIsOpen(codeView.Editor, false);
                return;
            }

            int headIndex, length;
            codeView.CodeDocument.GetWord(index, out headIndex, out length);

            if (popupInex != headIndex) // close once to move popup position
            {
                ToolTip.SetIsOpen(codeView.Editor, false);
            }
            popupInex = headIndex;

            PopupItem pItem = codeView.TextFile.GetPopupItem(codeView.CodeDocument.Version, index);
            if (pItem == null || pItem.ItemCount == 0)
            {
                ToolTip.SetIsOpen(codeView.Editor, false);
                return;
            }
            //            ToolTip.SetIsOpen(codeView.Editor, false);
            //            ToolTip.SetIsOpen(codeView.Editor, true);
            codeView.PopupColorLabel.Clear();
            codeView.PopupColorLabel.Add(pItem);

            //            ToolTip.SetIsOpen(codeView.Editor, false); // close once to update pop-up window position
            if (pItem.GetItems().Count != 0)
            {
                //                ToolTip.SetIsOpen(codeView.Editor, false);
                ToolTip.SetIsOpen(codeView.Editor, true);
            }
        }
    }
}
