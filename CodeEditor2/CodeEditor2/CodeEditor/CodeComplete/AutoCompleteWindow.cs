using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using Avalonia;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using CodeEditor2.Views;

namespace CodeEditor2.CodeEditor.CodeComplete
{
    internal class AutoCompleteWindow : CompletionWindowBase
    {

        private PopupWithCustomPosition _toolTip;
        private CompletionTipContentControl _toolTipContent;

        /// <summary>
        /// Gets the completion list used in this completion window.
        /// </summary>
        public CompletionList CompletionList { get; }

        /// <summary>
        /// Creates a new code completion window.
        /// </summary>
        public AutoCompleteWindow(TextArea textArea) : base(textArea)
        {
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.AutoCompleteWindow enter");
            CompletionList = new CompletionList();

            // keep height automatic
            CloseAutomatically = true;
            MaxHeight = 225;
            Width = 175;
            Child = CompletionList;
            // prevent user from resizing window to 0x0
            MinHeight = 15;
            MinWidth = 30;

            _toolTipContent = new CompletionTipContentControl();

            _toolTip = new PopupWithCustomPosition
            {
                IsLightDismissEnabled = true,
                PlacementTarget = this,
                Child = _toolTipContent,
            };

            LogicalChildren.Add(_toolTip);

            //_toolTip.Closed += (o, e) => ((Popup)o).Child = null;

            AttachEvents();

            CloseWhenCaretAtBeginning = true;
            //{
            //    var document = TextArea.Document;
            //    if (document != null)
            //    {
            //        string candidateWord = "";

            //        // update candidateWord
            //        if (Global.codeView.TextFile != null)
            //        {
            //            List<AutocompleteItem> items = Global.codeView.TextFile.GetAutoCompleteItems(Global.codeView._textEditor.CaretOffset, out candidateWord);
            //        }
            //        CompletionList.SelectItem(candidateWord);

            //        //if (CompletionList.ListBox.ItemCount == 0)
            //        //{
            //        //    IsVisible = false;
            //        //}
            //        //else
            //        //{
            //        //    IsVisible = true;
            //        //}
            //    }

            //}


            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.AutoCompleteWindow leave");
        }

        protected override void OnClosed()
        {
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.OnClosed enter");
            base.OnClosed();

            if (_toolTip != null)
            {
                _toolTip.IsOpen = false;
                _toolTip = null;
                _toolTipContent = null;
            }
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.OnClosed leave");
        }
        #region ToolTip handling

        private void CompletionList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.CompletionList_SelectionChanged enter");
            if (_toolTipContent == null) return;

            var item = CompletionList.SelectedItem;
            var description = item?.Description;


            if (description != null && Host is Control placementTarget && CompletionList.CurrentList != null)
            {
                _toolTipContent.Content = description;

                double yOffset = 0;
                var selectedIndex = CompletionList.ListBox.SelectedIndex;

                var itemContainer = CompletionList.ListBox.ContainerFromIndex(selectedIndex);

                if (itemContainer != null)
                {
                    _toolTip.Placement = PlacementMode.RightEdgeAlignedTop;
                    var position = itemContainer.TranslatePoint(new Point(0, 0), placementTarget);
                    if (position.HasValue) yOffset = position.Value.Y;
                }
                else
                {
                    //When scrolling down the container is not always ready
                    //If that happens we align the tooltip at the bottom or top
                    if (CompletionList.ListBox.FirstVisibleItem < selectedIndex)
                    {
                        _toolTip.Placement = PlacementMode.RightEdgeAlignedBottom;
                    }
                    else
                    {
                        _toolTip.Placement = PlacementMode.RightEdgeAlignedTop;
                    }
                }

                _toolTip.Offset = new Point(2, yOffset);
                _toolTip.PlacementTarget = placementTarget;
                _toolTip.IsOpen = true;
            }
            else
            {
                _toolTip.IsOpen = false;
            }
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.CompletionList_SelectionChanged leave");
        }

        #endregion

        private void CompletionList_InsertionRequested(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.CompletionList_InsertionRequested enter");
            Hide();
            // The window must close before Complete() is called.
            // If the Complete callback pushes stacked input handlers, we don't want to pop those when the CC window closes.
            var item = CompletionList.SelectedItem;
            item?.Complete(TextArea, new AnchorSegment(TextArea.Document, StartOffset, EndOffset - StartOffset), e);
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.CompletionList_InsertionRequested leave");
        }

        private void AttachEvents()
        {
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.AttachEvents enter");
            CompletionList.InsertionRequested += CompletionList_InsertionRequested;
            CompletionList.SelectionChanged += CompletionList_SelectionChanged;
            TextArea.Caret.PositionChanged += CaretPositionChanged;
            TextArea.PointerWheelChanged += TextArea_MouseWheel;
            TextArea.TextInput += TextArea_PreviewTextInput;
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.AttachEvents leave");
        }

        /// <inheritdoc/>
        protected override void DetachEvents()
        {
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.DetachEvents enter");
            CompletionList.InsertionRequested -= CompletionList_InsertionRequested;
            CompletionList.SelectionChanged -= CompletionList_SelectionChanged;
            TextArea.Caret.PositionChanged -= CaretPositionChanged;
            TextArea.PointerWheelChanged -= TextArea_MouseWheel;
            TextArea.TextInput -= TextArea_PreviewTextInput;

            foreach (var item in CompletionList.ListBox.Items)
            {
                AutocompleteItem? aItem = item as AutocompleteItem;
                if (aItem != null) aItem.Clean();
            }
            base.DetachEvents();
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.DetachEvents leave");
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.OnKeyDown enter");
            base.OnKeyDown(e);
            if (!e.Handled)
            {
                CompletionList.HandleKey(e);
            }
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.OnKeyDown leave");
        }

        private void TextArea_PreviewTextInput(object? sender, TextInputEventArgs e)
        {
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.TextArea_PreviewTextInput enter");
            e.Handled = RaiseEventPair(this, null, TextInputEvent,
                                       new TextInputEventArgs { Text = e.Text });
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.TextArea_PreviewTextInput leave");
        }

        private void TextArea_MouseWheel(object? sender, PointerWheelEventArgs e)
        {
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.TextArea_MouseWheel enter");
            e.Handled = RaiseEventPair(GetScrollEventTarget(),
                                       null, PointerWheelChangedEvent, e);
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.TextArea_MouseWheel leave");
        }

        private Control GetScrollEventTarget()
        {
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.GetScrollEventTarget enter");
            if (CompletionList == null)
                return this;
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.GetScrollEventTarget leave");
            return CompletionList.ScrollViewer ?? CompletionList.ListBox ?? (Control)CompletionList;
        }

        /// <summary>
        /// Gets/Sets whether the completion window should close automatically.
        /// The default value is true.
        /// </summary>
        public bool CloseAutomatically { get; set; }

        /// <inheritdoc/>
        protected override bool CloseOnFocusLost => CloseAutomatically;

        /// <summary>
        /// When this flag is set, code completion closes if the caret moves to the
        /// beginning of the allowed range. This is useful in Ctrl+Space and "complete when typing",
        /// but not in dot-completion.
        /// Has no effect if CloseAutomatically is false.
        /// </summary>
        public bool CloseWhenCaretAtBeginning { get; set; }

        private void CaretPositionChanged(object? sender, EventArgs e)
        {
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.CaretPositionChanged enter");
            var offset = TextArea.Caret.Offset;

            if (offset == StartOffset)
            {
                System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.CaretPositionChanged startOffset");
                if (CloseAutomatically && CloseWhenCaretAtBeginning)
                {
                    Hide();
                }
                else
                {
                    CompletionList.SelectItem(string.Empty);

                    if (CompletionList.ListBox.ItemCount == 0) IsVisible = false;
                    else IsVisible = true;
                }
                return;
            }else if (offset < StartOffset || offset > EndOffset)
            {
                if (CloseAutomatically)
                {
                    Hide();
                    System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.CaretPositionChanged hide");
                    return;
                }
            }
            else
            {
                var document = TextArea.Document;
                if (document != null)
                {
                    string candidateWord = "";

                    // update candidateWord
                    if(Global.codeView.TextFile != null)
                    {
                        List<AutocompleteItem> items = Global.codeView.TextFile.GetAutoCompleteItems(Global.codeView._textEditor.CaretOffset, out candidateWord);
                    }
                    System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.CaretPositionChanged SelectItem");
                    CompletionList.SelectItem(candidateWord);

                    System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.CaretPositionChanged ItemCount "+CompletionList.ListBox.ItemCount.ToString());
                    if (CompletionList.ListBox.ItemCount == 0)
                    {
                        IsVisible = false;
                    }
                    else
                    {
                        IsVisible = true;
                    }
                }
            }
            System.Diagnostics.Debug.Print("#=# AutoCompleteWindow.CaretPositionChanged leave");
        }
    }
}
