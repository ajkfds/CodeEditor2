using Avalonia.Controls;
using Avalonia.Threading;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2
{
    public static partial class Controller
    {
        public static class CodeEditor
        {
            public static async Task SetTextFileAsync(Data.TextFile? textFile)
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                await SetTextFileAsync(textFile, true);
            }
            public static async Task SetTextFileAsync(Data.TextFile? textFile, bool parseEntry)
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                if (textFile == null)
                {
                    await Global.codeView.SetTextFileAsync(null,false);
                    //Global.codeView. .mainForm.editorPage.CodeEditor.SetTextFile(null);
                    //Global.mainForm.mainTab.TabPages[0].Text = "-";
                }
                else
                {
                    //Global.mainForm.editorPage.CodeEditor.AbortInteractiveSnippet();
                    await Global.codeView.SetTextFileAsync(textFile, parseEntry);
                    //Global.mainForm.editorPage.CodeEditor.SetTextFile(textFile);
                    //Global.mainForm.mainTab.TabPages[0].Text = textFile.Name;
                    //Global.mainForm.mainTab.SelectedTab = Global.mainForm.mainTab.TabPages[0];
                }
            }

            public static ContextMenu ContextMenu
            {
                get
                {
                    if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                    return Global.codeView.contextMenu;
                }
            }

            public static void SetCaretPosition(int index)
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                Global.codeView.SetCaretPosition(index);
            }

            public static int? GetCaretPosition()
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                return Global.codeView.GetCaretPosition();
            }

            public static void SetSelection(int startIndex, int lastIndex)
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                Global.codeView.SetSelection(startIndex, lastIndex);
            }

            public static void GetSelection(out int startIndex, out int lastIndex)
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                CodeEditor2.CodeEditor.CodeDocument? codeDocument = Global.codeView.CodeDocument;
                if (codeDocument == null)
                {
                    startIndex = -1;
                    lastIndex = -1;
                    return;
                }
                startIndex = codeDocument.selectionStart;
                lastIndex = codeDocument.selectionLast;
            }
            public static async Task SaveAsync()
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                if (Global.codeView.CodeDocument == null) return;
                Data.TextFile? textFile = Global.codeView.CodeDocument.TextFile;
                if (textFile == null) return;
                await textFile.SaveAsync();
            }

            public static bool IsPopupMenuOpened
            {
                get
                {
                    if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                    return Global.codeView.codeViewPopupMenu.IsOpened;
                }
            }

            public static void ForceOpenCustomSelection(List<ToolItem> candidates)
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                Global.codeView.OpenCustomSelection(candidates);
            }
            public static async Task ForceOpenCustomSelectionAsync(List<ToolItem> candidates)
            {
                await Dispatcher.UIThread.InvokeAsync(
                    () =>
                    {
                        ForceOpenCustomSelection(candidates);
                    }
                );

            }
            public static PopupMenuView? OpenAutoComplete(List<ToolItem> candidates)
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                return Global.codeView.codeViewPopupMenu.OpenAutoComplete(candidates);
            }

            public static void UpdateAutoComplete(List<ToolItem> candidates)
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                Global.codeView.codeViewPopupMenu.UpdateAutoComplete(candidates);
            }
            //public static void ForceOpenAutoComplete(List<AutocompleteItem> autocompleteItems)
            //{
            //    Global.codeView.ForceOpenAutoComplete(autocompleteItems);
            //}

            public static void RequestReparse()
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                Global.codeView.RequestReparse();
            }

            public static Data.TextFile? GetTextFile()
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                return Global.codeView.TextFile;
            }

            internal static void StartInteractiveSnippet(Snippets.InteractiveSnippet interactiveSnippet)
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                Global.codeView.StartInteractiveSnippet(interactiveSnippet);
            }

            public static void AbortInteractiveSnippet()
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                Global.codeView.AbortInteractiveSnippet();
            }
            public static async Task AbortInteractiveSnippetAsync()
            {
                await Dispatcher.UIThread.InvokeAsync(() => {
                    AbortInteractiveSnippet();
                });
            }

            public static void AppendHighlight(int highlightStart, int highlightLast)
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                if (Global.codeView.CodeDocument == null) return;
                Global.codeView.CodeDocument.HighLights.AppendHighlight(highlightStart, highlightLast);
            }

            public static void GetHighlightPosition(int highlightIndex, out int highlightStart, out int highlightLast)
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                if (Global.codeView.CodeDocument == null)
                {
                    highlightStart = -1;
                    highlightLast = -1;
                    return;
                }
                Global.codeView.CodeDocument.HighLights.GetHighlightPosition(highlightIndex, out highlightStart, out highlightLast);
            }


            public static void SelectHighlight(int highLightIndex)
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                if (Global.codeView.CodeDocument == null) return;
                Global.codeView.CodeDocument.HighLights.SelectHighlight(highLightIndex);
            }
            public static async Task SelectHighlightAsync(int highLightIndex)
            {
                await Dispatcher.UIThread.InvokeAsync(() => {
                    SelectHighlight(highLightIndex);
                });
            }

            public static int GetHighlightIndex(int index)
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                if (Global.codeView.CodeDocument == null) return -1;
                return Global.codeView.CodeDocument.HighLights.GetHighlightIndex(index);
            }
            public static async Task<int> GetHighlightIndexAsync(int index)
            {
                return await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    return GetHighlightIndex(index);
                });
            }

            public static void ClearHighlight()
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                if (Global.codeView.CodeDocument == null) return;
                Global.codeView.CodeDocument.HighLights.ClearHighlight();
            }
            public static async Task ClearHighlightAsync()
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ClearHighlight();
                });
            }

            public static void EntryParse()
            {
                Dispatcher.UIThread.Invoke(Global.codeView.codeViewParser.EntryParse);
            }
            public static void Refresh()
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                Global.codeView.Redraw();
                Global.codeView.UpdateMarks();
            }

            public static void ScrollToCaret()
            {
                if (!Dispatcher.UIThread.CheckAccess()) System.Diagnostics.Debugger.Break();
                Global.codeView.ScrollToCaret();
            }

            public static double FontSize
            {
                get
                {
                    return Global.codeView._textEditor.FontSize;
                }
            }

        }

    }
}
