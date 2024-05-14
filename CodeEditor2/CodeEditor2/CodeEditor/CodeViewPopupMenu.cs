using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using Avalonia.VisualTree;
using CodeEditor2.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Input;
using System.Net.Http.Headers;

namespace CodeEditor2.CodeEditor
{
    public class CodeViewPopupMenu
    {
        // popup menu for in-line menu select

        public CodeViewPopupMenu(CodeView codeView)
        {
            this.codeView = codeView;
        }

        CodeView codeView;

        // tool selection form /////////////////////////////////////////////////////////////////////////

        public List<PopupMenuItem> PopupMenuItems = new List<PopupMenuItem>();

        public void OpenCustomSelection(List<CodeEditor2.CodeEditor.ToolItem> cantidates)
        {
            PopupMenuFlyout? flyout = FlyoutBase.GetAttachedFlyout(codeView._textEditor) as PopupMenuFlyout;
            if (flyout == null) return;
            if (flyout.IsOpen) return;
            if (codeView.TextFile == null) return;

            if (cantidates.Count == 0)
            {
                HidePopupMenu();
                return;
            }

            TransformedBounds? tbound = Global.codeView.Editor.GetTransformedBounds();
            if (tbound == null) return;
            TransformedBounds transformedBound = (TransformedBounds)tbound;
            var carletRect = codeView._textEditor.TextArea.Caret.CalculateCaretRectangle();


            Avalonia.Point position = transformedBound.Clip.Position;

            Avalonia.PixelPoint screenPosition = new Avalonia.PixelPoint(
                Global.mainWindow.Position.X + (int)(transformedBound.Clip.Position.X * Global.mainWindow.DesktopScaling),
                Global.mainWindow.Position.Y + (int)(transformedBound.Clip.Position.Y * Global.mainWindow.DesktopScaling)
                );

            PopupMenuItems.Clear();
            foreach (ToolItem item in cantidates) { PopupMenuItems.Add(item); }

            flyout.Placement = PlacementMode.AnchorAndGravity;
            flyout.VerticalOffset = carletRect.Top;
            flyout.HorizontalOffset = carletRect.Left;
            flyout.PlacementGravity = Avalonia.Controls.Primitives.PopupPositioning.PopupGravity.BottomRight;
            flyout.PlacementAnchor = Avalonia.Controls.Primitives.PopupPositioning.PopupAnchor.TopLeft;

            flyout.ShowAt(codeView._textEditor); // = FlyoutBase.ShowAttachedFlyout(_textEditor);
        }

        public void ShowToolSelectionPopupMenu()
        {
            PopupMenuFlyout? flyout = FlyoutBase.GetAttachedFlyout(codeView._textEditor) as PopupMenuFlyout;
            if (flyout == null) return;
            if (flyout.IsOpen) return;
            if (codeView.TextFile == null) return;


            List<ToolItem> tools = codeView.TextFile.GetToolItems(codeView.CodeDocument.CaretIndex);
            //items.Add(new Snippets.ToLower());
            if (tools == null)
            {
                tools = new List<ToolItem>();
            }
            tools.Add(new Snippets.ToUpper());
            tools.Add(new Snippets.ToLower());

            if (tools.Count == 0)
            {
                HidePopupMenu();
                return;
            }

            //TransformedBounds? tbound = Global.codeView.Editor.GetTransformedBounds();
            //if (tbound == null) return;
            //TransformedBounds transformedBound = (TransformedBounds)tbound;
            var carletRect = codeView._textEditor.TextArea.Caret.CalculateCaretRectangle();


            Avalonia.Point position = carletRect.Position;// transformedBound.Clip.Position;
            Avalonia.Vector scroll = codeView._textEditor.TextArea.TextView.ScrollOffset;
            //Avalonia.PixelPoint screenPosition = new Avalonia.PixelPoint(
            //    Global.mainWindow.Position.X + (int)(position.X * Global.mainWindow.DesktopScaling),
            //    Global.mainWindow.Position.Y + (int)((position.Y-scroll.Y) * Global.mainWindow.DesktopScaling)
            //    );

            PopupMenuItems.Clear();
            foreach (ToolItem item in tools) { PopupMenuItems.Add(item); }

            flyout.Placement = PlacementMode.AnchorAndGravity;
            flyout.VerticalOffset = position.Y-scroll.Y;
            flyout.HorizontalOffset = position.X-scroll.X;
            flyout.PlacementGravity = Avalonia.Controls.Primitives.PopupPositioning.PopupGravity.BottomRight;
            flyout.PlacementAnchor = Avalonia.Controls.Primitives.PopupPositioning.PopupAnchor.TopLeft;

            flyout.ShowAt(codeView._textEditor); // = FlyoutBase.ShowAttachedFlyout(_textEditor);
        }


        public bool IsOpened
        {
            get
            {
                PopupMenuFlyout? flyout = FlyoutBase.GetAttachedFlyout(codeView._textEditor) as PopupMenuFlyout;
                if (flyout == null) return false;
                if (!flyout.IsOpen) return false;
                return true;
            }
        }

        public void HidePopupMenu()
        {
            PopupMenuFlyout? flyout = FlyoutBase.GetAttachedFlyout(codeView._textEditor) as PopupMenuFlyout;
            if (flyout == null) return;
            if (!flyout.IsOpen) return;
            flyout.Hide();
        }

        public void PopupMenu_Selected(PopupMenuItem popUpMenuItem)
        {
            if (popUpMenuItem is ToolItem)
            {
                (popUpMenuItem as ToolItem)?.Apply(codeView.CodeDocument);
            }
            else
            {
                popUpMenuItem.OnSelected();
            }
        }

        public Snippets.InteractiveSnippet Snippet = null;

        public void StartInteractiveSnippet(Snippets.InteractiveSnippet interactiveSnippet)
        {
            AbortInteractiveSnippet();
            Snippet = interactiveSnippet;
        }

        public void AbortInteractiveSnippet()
        {
            if (Snippet == null) return;
            Snippet.Aborted();
            Snippet = null;
        }

        public void TextArea_KeyDown(object? sender, KeyEventArgs e)
        {
            if (Snippet == null) return;
            Snippet.KeyDown(sender, e, codeView.PopupMenu);
        }
        public void TextEntering(object? sender, TextInputEventArgs e)
        {
            if (Snippet == null) return;
            Snippet.BeforeKeyDown(sender, e, codeView.PopupMenu);
        }

        public void TextEntered(object? sender, TextInputEventArgs e)
        {
            if (Snippet == null) return;
            Snippet.AfterKeyDown(sender, e, codeView.PopupMenu);
        }
        public virtual void AfterAutoCompleteHandled()
        {
            if (Snippet == null) return;
            Snippet.AfterAutoCompleteHandled(codeView.PopupMenu);
        }

    }
}
