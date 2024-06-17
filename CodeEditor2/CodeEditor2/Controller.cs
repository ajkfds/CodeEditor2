using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using CodeEditor2.Data;
using CodeEditor2.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CodeEditor2.CodeEditor.ParsedDocument;
using static CodeEditor2.Controller;

namespace CodeEditor2
{
    public static class Controller
    {
        public static void AppendLog(string message)
        {
            Global.logView.AppendLog(message);
            System.Diagnostics.Debug.Print(message);
        }
        public static void AppendLog(string message, Avalonia.Media.Color color)
        {
            Global.logView.AppendLog(message, color);
        }

        //public static System.Drawing.Color GetBackColor()
        //{
        //    return Global.mainForm.BackColor;
        //}


        internal static async Task AddProject(Project project)
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

        private static async Task addProject(Project project)
        {
            Global.navigateView.AddProject(project);


            CodeEditor2.Tools.ParseProject parser = new Tools.ParseProject();
            await parser.Run(Global.navigateView.GetProjectNode(project.Name)); 

            //Tools.ProgressWindow progressWindow = new Tools.ProgressWindow(project.Name, "Loading...", 100);
            //progressWindow.Show();

            //Tools.ParseProjectForm pform = new Tools.ParseProjectForm(Global.navigateView.GetPeojectNode(project.Name));
            //await pform.ShowDialog(Global.currentWindow);
        }

        public static Menu GetMenuStrip()
        {
            return Global.mainView.Menu;
        }

        public static void ShellExecute(string command)
        {
            Global.mainView.ShellPanelView.Execute(command);
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

            public static void SetCaretPosition(int index)
            {
                Global.codeView.SetCaretPosition(index);
            }
            public static void SetSelection(int startIndex,int lastIndex)
            {
                Global.codeView.SetSelection(startIndex, lastIndex);
            }
            public static void Save()
            {
                if (Global.codeView.CodeDocument == null) return;
                Global.codeView.CodeDocument.TextFile.Save();
            }

            public static bool IsPopupMenuOpened
            {
                get
                {
                    return Global.codeView.codeViewPopupMenu.IsOpened;
                }
            }

            public static void ForceOpenCustomSelection(List<CodeEditor2.CodeEditor.ToolItem> cantidates)
            {
                Global.codeView.OpenCustomSelection(cantidates);
            }

            public static void ForceOpenAutoComplete(List<CodeEditor2.CodeEditor.AutocompleteItem> autocompleteItems)
            {
                Global.codeView.ForceOpenAutoComplete(autocompleteItems);
            }

            public static void RequestReparse()
            {
                Global.codeView.RequestReparse();
            }

            public static Data.ITextFile GetTextFile()
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

            public static ContextMenu GetContextMenu()
            {
                return Global.navigateView.ContextMenu;
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
