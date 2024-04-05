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

namespace CodeEditor2.CodeEditor
{
    public class CodeViewPopup
    {
        public CodeViewPopup(CodeView codeView)
        {
            this.codeView = codeView;
        }

        CodeView codeView;

        private int popupInex = -1;
        public void TextArea_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (codeView.CodeDocument == null || codeView.TextFile == null) return;
            Avalonia.Point point = e.GetPosition(codeView._textEditor.TextArea);
            var pos = codeView._textEditor.GetPositionFromPoint(point);
            if (pos == null) return;

            TextViewPosition tpos = (TextViewPosition)pos;
            int index = codeView.CodeDocument.TextDocument.GetOffset(tpos.Line, tpos.Column);

            int headIndex, length;
            codeView.CodeDocument.GetWord(index, out headIndex, out length);

            if (popupInex == headIndex) return;
            popupInex = headIndex;

            PopupItem pitem = codeView.TextFile.GetPopupItem(codeView.CodeDocument.Version, index);
            if (pitem == null)
            {
                ToolTip.SetIsOpen(codeView.Editor, false);
                return;
            }
            ToolTip.SetIsOpen(codeView.Editor, false);
            if (pitem.GetItems().Count != 0)
            {
                ToolTip.SetIsOpen(codeView.Editor, true);
            }
        }
    }
}
