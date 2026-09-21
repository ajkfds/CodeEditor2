using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit;
using CodeEditor2.Views;
using System.Collections.Generic;
using System;

namespace CodeEditor2.CodeEditor.PopupHint
{
    public class PopupHandler
    {
        // Popup-Hint Handler
        //
        // show mouse over popup-hinting
        public PopupHandler(CodeView codeView)
        {
            this.codeView = codeView;
        }

        CodeView codeView;

        private int popupInex = -1;

        // Combine the given PopupItems into a single PopupItem. Items are joined
        // by a newline so that each item appears on its own line.
        // Returns null if the resulting PopupItem would be empty.
        private static PopupItem? CombinePopupItems(IEnumerable<PopupItem?>? popupItems)
        {
            if (popupItems == null) return null;
            PopupItem combined = new PopupItem();
            bool any = false;
            foreach (PopupItem? item in popupItems)
            {
                if (item == null) continue;
                List<AjkAvaloniaLibs.Controls.ColorLabel.labelItem> itemLabels = item.GetItems();
                if (itemLabels == null) continue;
                foreach (var label in itemLabels)
                {
                    combined.GetItems().Add(label);
                }
                // Insert a newline between popup items so they appear on separate lines.
                combined.GetItems().Add(new AjkAvaloniaLibs.Controls.ColorLabel.labelNewLine());
                any = true;
            }
            if (!any) return null;
            combined.RemoveLastNewLine();
            return combined;
        }

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
            if (codeView.PopupTextBlock.Inlines == null) throw new Exception();

            codeView.PopupTextBlock.Inlines.Clear();

            // Mouse-over hint popup: use the lightweight GetPopupItem API so that
            // we do not trigger heavy parsing on every pointer-move event.
            PopupItem? popupItem = codeView.TextFile.GetPopupItem(codeView.CodeDocument.Version, index);
            if (popupItem != null && popupItem.ItemCount > 0)
            {
                popupItem.RemoveLastNewLine();
                popupItem.AppendToTextBlock(codeView.PopupTextBlock);
            }

            if (codeView.PopupTextBlock.Inlines.Count == 0)
            {
                ToolTip.SetIsOpen(codeView.Editor, false);
                return;
            }

            // Mouse hover: anchor the ToolTip to the pointer (default placement).
            // OpenPopup() may have previously set BottomEdgeAlignedLeft for the
            // caret, so make sure pointer-driven popups restore the pointer
            // placement here.
            ToolTip.SetPlacement(codeView.Editor, PlacementMode.Pointer);
            ToolTip.SetHorizontalOffset(codeView.Editor, 0);
            ToolTip.SetVerticalOffset(codeView.Editor, 0);
            ToolTip.SetIsOpen(codeView.Editor, true);
        }

        public void OpenPopup(List<PopupItem> popupItems)
        {
            if (codeView == null || codeView.Editor == null) return;
            if (popupItems == null || popupItems.Count == 0) return;

            // Calculate caret rectangle in editor (text area) coordinates.
            var caretRect = codeView._textEditor.TextArea.Caret.CalculateCaretRectangle();

            // Combine all popup items into a single PopupItem so that we can
            // reuse the existing ToolTip-based popup mechanism (PopupTextBlock).
            PopupItem? combined = CombinePopupItems(popupItems);
            if (combined == null) return;

            // Update the ToolTip content (TextBlock shared with TextArea_PointerMoved).
            if (codeView.PopupTextBlock.Inlines == null) return;
            codeView.PopupTextBlock.Inlines.Clear();
            if (combined.ItemCount > 0)
            {
                combined.AppendToTextBlock(codeView.PopupTextBlock);
            }

            if (codeView.PopupTextBlock.Inlines.Count == 0)
            {
                ToolTip.SetIsOpen(codeView.Editor, false);
                return;
            }

            // Anchor the ToolTip to the caret position. The placement target
            // is codeView.Editor, so the offset is interpreted relative to it.
            //
            // caretRect is in TextArea coordinates, so translate it into the
            // Editor (placement target) coordinate space before applying it
            // as the ToolTip offset.
            Avalonia.Point? caretOriginInEditor = codeView._textEditor.TextArea
                .TranslatePoint(new Avalonia.Point(caretRect.X, caretRect.Y), codeView.Editor);
            double caretOffsetX = caretOriginInEditor?.X ?? caretRect.X;
            double caretOffsetY = caretOriginInEditor?.Y ?? caretRect.Y;

            ToolTip.SetPlacement(codeView.Editor, PlacementMode.BottomEdgeAlignedLeft);
            ToolTip.SetHorizontalOffset(codeView.Editor, caretOffsetX);
            ToolTip.SetVerticalOffset(codeView.Editor, caretOffsetY - caretRect.Height);

            // Close once so that the popup repositions at the new caret location,
            // then open it.
            ToolTip.SetIsOpen(codeView.Editor, false);
            ToolTip.SetIsOpen(codeView.Editor, true);
        }

        public void ClosePopup()
        {
            if (codeView == null || codeView.Editor == null) return;
            ToolTip.SetIsOpen(codeView.Editor, false);
        }

    }

}
