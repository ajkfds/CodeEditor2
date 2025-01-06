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
using CodeEditor2.Snippets;
using DynamicData;

namespace CodeEditor2.CodeEditor.PopupMenu
{
    public class PopupMenuHandler
    {
        // popup menu for in-line menu select

        public PopupMenuHandler(CodeView codeView)
        {
            this.codeView = codeView;
        }

        CodeView codeView;

        // tool selection form /////////////////////////////////////////////////////////////////////////

        public List<PopupMenuItem> PopupMenuItems = new List<PopupMenuItem>();

        public PopupMenuView? OpenAutoComplete(List<ToolItem> candidates)
        {
            System.Diagnostics.Debug.Print("## OpenCustomSelection");
            PopupMenuFlyout? flyout = FlyoutBase.GetAttachedFlyout(codeView._textEditor) as PopupMenuFlyout;
            if (flyout == null) return null;
            if (flyout.IsOpen) return null;
            if (codeView.TextFile == null) return null;

            TransformedBounds? tBound = Global.codeView.Editor.GetTransformedBounds();
            if (tBound == null) return null;
            //            TransformedBounds transformedBound = (TransformedBounds)tBound;
            var caretRect = codeView._textEditor.TextArea.Caret.CalculateCaretRectangle();


            //            Avalonia.Point position = transformedBound.Clip.Position;

            PopupMenuItems.Clear();
            foreach (ToolItem item in candidates) { PopupMenuItems.Add(item.CreatePopupMenuItem()); }

            flyout.ShowMode = FlyoutShowMode.Standard;
            flyout.Placement = PlacementMode.AnchorAndGravity;
            flyout.VerticalOffset = caretRect.Top + caretRect.Height;
            flyout.HorizontalOffset = caretRect.Left;
            flyout.PlacementGravity = Avalonia.Controls.Primitives.PopupPositioning.PopupGravity.BottomRight;
            flyout.PlacementAnchor = Avalonia.Controls.Primitives.PopupPositioning.PopupAnchor.TopLeft;

            PopupMenuView popupMenuView = (PopupMenuView)flyout.Content;
            popupMenuView.TextBox0.IsVisible = false;


            flyout.ShowMode = FlyoutShowMode.Transient;
            flyout.ShowAt(codeView._textEditor); // = FlyoutBase.ShowAttachedFlyout(_textEditor);

            return popupMenuView;
        }
        public void UpdateAutoComplete(List<ToolItem> candidates)
        {
            //System.Diagnostics.Debug.Print("## OpenCustomSelection");
            //PopupMenuFlyout? flyout = FlyoutBase.GetAttachedFlyout(codeView._textEditor) as PopupMenuFlyout;
            //if (flyout == null) return;
            //if (flyout.IsOpen) return;
            //if (codeView.TextFile == null) return;

            //TransformedBounds? tBound = Global.codeView.Editor.GetTransformedBounds();
            //if (tBound == null) return;
            ////            TransformedBounds transformedBound = (TransformedBounds)tBound;
            //var caretRect = codeView._textEditor.TextArea.Caret.CalculateCaretRectangle();


            PopupMenuFlyout? flyout = FlyoutBase.GetAttachedFlyout(codeView._textEditor) as PopupMenuFlyout;
            if (flyout == null) return;
//            if (flyout.IsOpen) return;
            if (codeView.TextFile == null) return;
            PopupMenuView popupMenuView = (PopupMenuView)flyout.Content;
            PopupMenuItems.Clear();
            foreach (ToolItem item in candidates) { PopupMenuItems.Add(item.CreatePopupMenuItem()); }

            popupMenuView.ListView.Items.Clear();
            foreach (ToolItem item in candidates)
            {
                popupMenuView.ListView.Items.Add(item.CreatePopupMenuItem());
            }

        }

        public void OpenCustomSelection(List<ToolItem> candidates)
        {
            System.Diagnostics.Debug.Print("## OpenCustomSelection");
            PopupMenuFlyout? flyout = FlyoutBase.GetAttachedFlyout(codeView._textEditor) as PopupMenuFlyout;
            if (flyout == null) return;
            if (flyout.IsOpen) return;
            if (codeView.TextFile == null) return;

            TransformedBounds? tBound = Global.codeView.Editor.GetTransformedBounds();
            if (tBound == null) return;
            //            TransformedBounds transformedBound = (TransformedBounds)tBound;
            var caretRect = codeView._textEditor.TextArea.Caret.CalculateCaretRectangle();


            //            Avalonia.Point position = transformedBound.Clip.Position;

            PopupMenuItems.Clear();
            foreach (ToolItem item in candidates) { PopupMenuItems.Add(item.CreatePopupMenuItem()); }

            flyout.ShowMode = FlyoutShowMode.Standard;
            flyout.Placement = PlacementMode.AnchorAndGravity;
            flyout.VerticalOffset = caretRect.Top;
            flyout.HorizontalOffset = caretRect.Left;
            flyout.PlacementGravity = Avalonia.Controls.Primitives.PopupPositioning.PopupGravity.BottomRight;
            flyout.PlacementAnchor = Avalonia.Controls.Primitives.PopupPositioning.PopupAnchor.TopLeft;

            PopupMenuView popupMenuView = (PopupMenuView)flyout.Content;
            popupMenuView.TextBox0.IsVisible = true;


            flyout.ShowMode = FlyoutShowMode.Standard;
            flyout.ShowAt(codeView._textEditor); // = FlyoutBase.ShowAttachedFlyout(_textEditor);

        }

        public void ShowToolSelectionPopupMenu()
        {
            if (Snippet != null)
            {
                Controller.CodeEditor.AbortInteractiveSnippet();
            }

            System.Diagnostics.Debug.Print("## ShowToolSelectionPopupMenu");
            PopupMenuFlyout? flyout = FlyoutBase.GetAttachedFlyout(codeView._textEditor) as PopupMenuFlyout;
            if (flyout == null) return;
            if (flyout.IsOpen) return;
            if (codeView.TextFile == null) return;


            List<ToolItem>? tools = codeView.TextFile.GetToolItems(codeView.CodeDocument.CaretIndex);
            if (tools == null)
            {
                tools = new List<ToolItem>();
            }
            tools.Add(new ToUpper());
            tools.Add(new ToLower());
            tools.Add(new LlmSnippet());

            if (tools.Count == 0)
            {
                HidePopupMenu();
                return;
            }

            //TransformedBounds? tBound = Global.codeView.Editor.GetTransformedBounds();
            //if (tBound == null) return;
            //TransformedBounds transformedBound = (TransformedBounds)tBound;
            var caretRect = codeView._textEditor.TextArea.Caret.CalculateCaretRectangle();


            Avalonia.Point position = caretRect.Position;// transformedBound.Clip.Position;
            Avalonia.Vector scroll = codeView._textEditor.TextArea.TextView.ScrollOffset;
            //Avalonia.PixelPoint screenPosition = new Avalonia.PixelPoint(
            //    Global.mainWindow.Position.X + (int)(position.X * Global.mainWindow.DesktopScaling),
            //    Global.mainWindow.Position.Y + (int)((position.Y-scroll.Y) * Global.mainWindow.DesktopScaling)
            //    );

            PopupMenuItems.Clear();
            foreach (ToolItem item in tools) { PopupMenuItems.Add(item.CreatePopupMenuItem()); }

            flyout.ShowMode = FlyoutShowMode.Standard;
            flyout.Placement = PlacementMode.AnchorAndGravity;
            flyout.VerticalOffset = position.Y - scroll.Y;
            flyout.HorizontalOffset = position.X - scroll.X;
            flyout.PlacementGravity = Avalonia.Controls.Primitives.PopupPositioning.PopupGravity.BottomRight;
            flyout.PlacementAnchor = Avalonia.Controls.Primitives.PopupPositioning.PopupAnchor.TopLeft;

            PopupMenuView popupMenuView = (PopupMenuView)flyout.Content;
            popupMenuView.TextBox0.IsVisible = true;

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
            System.Diagnostics.Debug.Print("## HidePopupMenu");
            PopupMenuFlyout? flyout = FlyoutBase.GetAttachedFlyout(codeView._textEditor) as PopupMenuFlyout;
            if (flyout == null) return;
            if (!flyout.IsOpen) return;
            flyout.Hide();
        }

        public void PopupMenu_Selected(PopupMenuItem popUpMenuItem)
        {
            System.Diagnostics.Debug.Print("## PopupMenu_Selected");

            //            ToolItem? toolItem = popUpMenuItem as ToolItem;
            //if (popUpMenuItem is ToolItem)
            //{
            //    if (codeView.CodeDocument == null) return;
            //    (popUpMenuItem as ToolItem)?.Apply(codeView.CodeDocument);

            //    //InteractiveSnippet? snippet = popUpMenuItem as InteractiveSnippet;
            //    //if (snippet == null) return;
            //    //Snippet = snippet;
            //}
            //else
            //{
            popUpMenuItem.OnSelected();
            //}
        }

        private InteractiveSnippet? snippet = null;
        public InteractiveSnippet? Snippet
        {
            get
            {
                return snippet;
            }
            set
            {
                if(value == null)
                {
                    System.Diagnostics.Debug.Print("### Snippet = null");
                }
                else
                {
                    System.Diagnostics.Debug.Print("### Snippet = " + value.ToString());
                }
                snippet = value;
            }
        }

        public void StartInteractiveSnippet(InteractiveSnippet interactiveSnippet)
        {
            System.Diagnostics.Debug.Print("## CodeViewSetupMenu.StartInteractiveSnippet");
            AbortInteractiveSnippet();
            Snippet = interactiveSnippet;
        }

        public void AbortInteractiveSnippet()
        {
            if (Snippet == null) return;
            System.Diagnostics.Debug.Print("## CodeViewSetupMenu.AbortInteractiveSnippet for snippet");
            Snippet.Aborted();
            Snippet = null;
        }

        public void TextArea_KeyDown(object? sender, KeyEventArgs e)
        {
            System.Diagnostics.Debug.Print("## TextArea_KeyDown for snippet enter");
            if (Snippet == null) return;
            Snippet.KeyDown(sender, e, codeView.PopupMenu);
            System.Diagnostics.Debug.Print("## TextArea_KeyDown for snippet leave");
        }
        public void TextEntering(object? sender, TextInputEventArgs e)
        {
            if (Snippet == null) return;
            System.Diagnostics.Debug.Print("## TextEntering for snippet");
            Snippet.BeforeKeyDown(sender, e, codeView.PopupMenu);
        }

        public void TextEntered(object? sender, TextInputEventArgs e)
        {
            if (Snippet == null) return;
            System.Diagnostics.Debug.Print("## TextEntered for snippet");
            Snippet.AfterKeyDown(sender, e, codeView.PopupMenu);
        }
        public virtual void AfterAutoCompleteHandled()
        {
            if (Snippet == null) return;
            System.Diagnostics.Debug.Print("## AfterAutoCompleteHandled for snippet");
            Snippet.AfterAutoCompleteHandled(codeView.PopupMenu);
        }

    }
}
