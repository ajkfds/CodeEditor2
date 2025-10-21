using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using CodeEditor2.CodeEditor.CodeComplete;
using CodeEditor2.CodeEditor.PopupMenu;
using CodeEditor2.FileTypes;
using CodeEditor2.NavigatePanel;
using CodeEditor2.Views;
using ExCSS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static CodeEditor2.CodeEditor.ParsedDocument;
using static CodeEditor2.Controller;

namespace CodeEditor2
{
    public static class Controller
    {
        public static void AppendLog(string message)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                Global.logView.AppendLog(message);
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    Global.logView.AppendLog(message);
                });
            }

            System.Diagnostics.Debug.Print(message);
        }
        public static void AppendLog(string message, Avalonia.Media.Color color)
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                Global.logView.AppendLog(message, color);
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    Global.logView.AppendLog(message, color);
                });
            }
        }

        public static Window GetMainWindow()
        {
            return Global.mainWindow;
        }

        //public static System.Drawing.Color GetBackColor()
        //{
        //    return Global.mainForm.BackColor;
        //}

        
        public static bool SelectText(Data.ITextFile textFile,int index,int length)
        {
            Data.TextReference textReference = new Data.TextReference(textFile, index, length);
            return true;
        }

        public static bool SelectText(Data.TextReference textReference)
        {
            Data.ITextFile? file = textReference.TextFile;
            if (file == null) return false;

            Data.File? currentFile = NavigatePanel.GetSelectedFile();
            if (file != currentFile)
            {
                NavigatePanelNode node = file.NavigatePanelNode;
                NavigatePanel.SelectNode(node);
            }
            CodeEditor.SetSelection(textReference.StartIndex, textReference.StartIndex + textReference.Length);
            return true;
        }

        public static void AddSelectHistory(Data.TextReference textReference)
        {
            Global.mainView.AddSelectHistory(textReference);
        }

        internal static async Task AddProject(Data.Project project)
        {
            if (Global.Projects.ContainsKey(project.Name))
            {
                System.Diagnostics.Debugger.Break();
                //return null;
            }
            else
            {
                Global.Projects.Add(project.Name, project);
                await addProject(project);
            }
        }

        private static async Task addProject(Data.Project project)
        {
            // add project node
            Global.navigateView.AddProject(project);

            // parse project
            CodeEditor2.Tools.ParseProject parser = new Tools.ParseProject();
            ProjectNode? projectNode = Global.navigateView.GetProjectNode(project.Name);
            if (projectNode == null) return;
            
            if (Dispatcher.UIThread.CheckAccess())
            {
                await parser.Run(projectNode);
            }
            else
            {
                Dispatcher.UIThread.Post(
                async () =>
                {
                    await parser.Run(projectNode);
                }
                );
            }
            
        }

        //public static Menu GetMenuStrip()
        //{
        //    return Global.mainView.Menu;
        //}

        public static void ShellExecute(string command)
        {
            Global.mainView.ShellPanelView.Execute(command);
        }

        public static class Menu
        {
            public static MenuItem File
            {
                get
                {
                    return Global.mainView.MenuItem_File;
                }
            }

            public static MenuItem Edit
            {
                get
                {
                    return Global.mainView.MenuItem_Edit;
                }
            }

            public static MenuItem Project
            {
                get
                {
                    return Global.mainView.MenuItem_Project;
                }
            }

            public static MenuItem Tool
            {
                get
                {
                    return Global.mainView.MenuItem_Tools;
                }
            }

            public static MenuItem Help
            {
                get
                {
                    return Global.mainView.MenuItem_Help;
                }
            }

        }

        //public static System.Windows.Forms.DialogResult ShowMessageBox(string text, string caption, System.Windows.Forms.MessageBoxButtons buttons, System.Windows.Forms.MessageBoxIcon icon)
        //{
        //    return System.Windows.Forms.MessageBox.Show(text, caption, buttons, icon);
        //}
        //public static System.Windows.Forms.DialogResult ShowMessageBox(string text, string caption, System.Windows.Forms.MessageBoxButtons buttons)
        //{
        //    return System.Windows.Forms.MessageBox.Show(text, caption, buttons);
        //}

        //public static async void ShowForm(Avalonia.Controls.Window form)
        //{
        //    form.Show(Global.mainWindow);
        //}

        //public static async Task ShowDialogForm(Avalonia.Controls.Window form)
        //{
        //    await form.ShowDialog(Global.mainWindow);
        //}


        //public static System.Windows.Forms.DialogResult ShowDialogForm(System.Windows.Forms.CommonDialog dialog)
        //{
        //    return dialog.ShowDialog(Global.mainForm);
        //}
        //public static void ShowForm(System.Windows.Forms.Form form, System.Drawing.Point position)
        //{
        //    form.Show(Global.mainForm);
        //    form.Location = position;
        //}

        //public static void DisposeOwndesForms()
        //{
        //    System.Windows.Forms.Form[] forms = Global.mainForm.OwnedForms;
        //    for (int i = forms.Count(); i >= 0; i--)
        //    {
        //        forms[i].Close();
        //    }
        //}

        public static class CodeEditor
        {
            public static void SetTextFile(Data.TextFile textFile)
            {
                SetTextFile(textFile, true);
            }
            public static void SetTextFile(Data.TextFile textFile,bool parseEntry)
            {
                if (textFile == null)
                {
                    //Global.codeView. .mainForm.editorPage.CodeEditor.SetTextFile(null);
                    //Global.mainForm.mainTab.TabPages[0].Text = "-";
                }
                else
                {
                    //Global.mainForm.editorPage.CodeEditor.AbortInteractiveSnippet();
                    Global.codeView.SetTextFile(textFile,parseEntry);
                    //Global.mainForm.editorPage.CodeEditor.SetTextFile(textFile);
                    //Global.mainForm.mainTab.TabPages[0].Text = textFile.Name;
                    //Global.mainForm.mainTab.SelectedTab = Global.mainForm.mainTab.TabPages[0];
                }
            }

            public static ContextMenu ContextMenu
            {
                get { return Global.codeView.contextMenu; }
            }

            public static void SetCaretPosition(int index)
            {
                Global.codeView.SetCaretPosition(index);
            }

            public static int? GetCaretPosition()
            {
                return Global.codeView.GetCaretPosition();
            }

            public static void SetSelection(int startIndex,int lastIndex)
            {
                Global.codeView.SetSelection(startIndex, lastIndex);
            }

            public static void GetSelection(out int startIndex, out int lastIndex)
            {
                CodeEditor2.CodeEditor.CodeDocument? codeDocument = Global.codeView.CodeDocument;
                if(codeDocument == null)
                {
                    startIndex = -1;
                    lastIndex = -1;
                    return;
                }
                startIndex = codeDocument.selectionStart;
                lastIndex =  codeDocument.selectionLast;
            }
            public static void Save()
            {
                if (Global.codeView.CodeDocument == null) return;
                Data.TextFile? textFile = Global.codeView.CodeDocument.TextFile;
                if (textFile == null) return;
                textFile.Save();
            }

            public static bool IsPopupMenuOpened
            {
                get
                {
                    return Global.codeView.codeViewPopupMenu.IsOpened;
                }
            }

            public static void ForceOpenCustomSelection(List<ToolItem> candidates)
            {
                Global.codeView.OpenCustomSelection(candidates);
            }
            public static PopupMenuView? OpenAutoComplete(List<ToolItem> candidates)
            {
                return Global.codeView.codeViewPopupMenu.OpenAutoComplete(candidates);
            }

            public static void UpdateAutoComplete(List<ToolItem> candidates)
            {
                Global.codeView.codeViewPopupMenu.UpdateAutoComplete(candidates);
            }
            //public static void ForceOpenAutoComplete(List<AutocompleteItem> autocompleteItems)
            //{
            //    Global.codeView.ForceOpenAutoComplete(autocompleteItems);
            //}

            public static void RequestReparse()
            {
                Global.codeView.RequestReparse();
            }

            public static Data.TextFile? GetTextFile()
            {
                return Global.codeView.TextFile;
            }

            internal static void StartInteractiveSnippet(Snippets.InteractiveSnippet interactiveSnippet)
            {
                Global.codeView.StartInteractiveSnippet(interactiveSnippet);
            }

            public static void AbortInteractiveSnippet()
            {
                Global.codeView.AbortInteractiveSnippet();
            }

            public static void AppendHighlight(int highlightStart, int highlightLast)
            {
                if (Global.codeView.CodeDocument == null) return;
                Global.codeView.CodeDocument.HighLights.AppendHighlight(highlightStart, highlightLast);
            }

            public static void GetHighlightPosition(int highlightIndex, out int highlightStart, out int highlightLast)
            {
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
                if (Global.codeView.CodeDocument == null) return;
                Global.codeView.CodeDocument.HighLights.SelectHighlight(highLightIndex);
            }

            public static int GetHighlightIndex(int index)
            {
                if (Global.codeView.CodeDocument == null) return -1;
                return Global.codeView.CodeDocument.HighLights.GetHighlightIndex(index);
            }

            public static void ClearHighlight()
            {
                if (Global.codeView.CodeDocument == null) return;
                Global.codeView.CodeDocument.HighLights.ClearHighlight();
            }
            public static void Refresh()
            {
                Global.codeView.Redraw();
                Global.codeView.UpdateMarks();
            }

            public static void ScrollToCaret()
            {
                Global.codeView.ScrollToCaret();
            }


        }

        public static class NavigatePanel
        {
            public static void UpdateVisual()
            {
                Dispatcher.UIThread.Post(
                    new Action(() =>
                    {
                        Global.navigateView.InvalidateVisual();
                    })
                );
            }

            public static void SelectNode(NavigatePanelNode node)
            {
                Global.navigateView.SelectNode(node);
            }
            public static ContextMenu GetContextMenu()
            {
                return Global.navigateView.ContextMenu;
            }

            public static MenuItem? GetContextMenuItem(List<string> captions)
            {
                return Global.navigateView.getContextMenuItem(captions);
            }

            public static Data.Project GetProject(NavigatePanelNode node)
            {
                return Global.navigateView.GetProject(node);
            }

            public static void UpdateFolder(NavigatePanelNode node)
            {
                Global.navigateView.UpdateFolder(node);
            }

            //public static void Refresh()
            //{
            //    if (Global.mainForm.InvokeRequired)
            //    {
            //        Global.mainForm.Invoke(new Action(() => { Global.mainForm.navigatePanel.Refresh(); }));
            //    }
            //    else
            //    {
            //        Global.mainForm.navigatePanel.Refresh();
            //    }
            //}

            //public static void UpdateVisibleNode()
            //{
            //    Global.mainForm.navigatePanel.UpdateWholeVisibleNode();
            //}

            //public static void UpdateVisibleNode(CodeEditor2.NavigatePanel.NavigatePanelNode node)
            //{
            //    Global.mainForm.navigatePanel.UpdateWholeVisibleNode(node);
            //}

            public static CodeEditor2.NavigatePanel.NavigatePanelNode? GetSelectedNode()
            {
                return Global.navigateView.GetSelectedNode();
            }

            public static Data.File? GetSelectedFile()
            {
                var node = Global.navigateView.GetSelectedNode();
                Data.File? file = node?.Item as Data.File;
                return file;
            }

            //public static void Parse(CodeEditor2.NavigatePanel.NavigatePanelNode node)
            //{
            //    Tools.ParseHierarchyForm form = new Tools.ParseHierarchyForm(node);
            //    while (form.Visible)
            //    {
            //        System.Threading.Thread.Sleep(1);
            //    }
            //    Controller.ShowForm(form);
            //}

            //public static void Update()
            //{
            //    if (Global.mainForm.InvokeRequired)
            //    {
            //        Global.mainForm.Invoke(new Action(() => { Global.mainForm.navigatePanel.Update(); }));
            //    }
            //    else
            //    {
            //        Global.mainForm.navigatePanel.Update();
            //    }
            //}

            //public static void Invalidate()
            //{
            //    Global.mainForm.navigatePanel.Invalidate();
            //}
        }

        public static class Tabs
        {

            public static void AddItem(Avalonia.Controls.TabItem tabItem)
            {
                Global.mainView.TabControl0.Items.Add(tabItem);
            }

            public static void RemoveItem(Avalonia.Controls.TabItem tabItem)
            {
                Global.mainView.TabControl0.Items.Remove(tabItem);
            }

            public static void SelectTab(Avalonia.Controls.TabItem tabItem)
            {
                if (Global.mainView.TabControl0.SelectedItem == tabItem) return;
                Global.mainView.TabControl0.SelectedItem = tabItem;
            }
        }


        public static class MessageView
        {
            public static void Update(CodeEditor2.CodeEditor.ParsedDocument parsedDocument)
            {
                Global.infoView.UpdateMessages(parsedDocument);
            }
        }
    }
}
