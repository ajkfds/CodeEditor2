﻿using Avalonia.LogicalTree;
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


        internal static void AddProject(Project project)
        {
            if (Global.Projects.ContainsKey(project.Name))
            {
                System.Diagnostics.Debugger.Break();
                return;
            }
            Global.Projects.Add(project.Name, project);
            addProject(project);
        }

        private static async void addProject(Project project)
        {
            Global.navigateView.AddProject(project);

            CodeEditor2.Tools.ParseProject.Run(Global.navigateView.GetPeojectNode(project.Name));

            //Tools.ProgressWindow progressWindow = new Tools.ProgressWindow(project.Name, "Loading...", 100);
            //progressWindow.Show();

            //Tools.ParseProjectForm pform = new Tools.ParseProjectForm(Global.navigateView.GetPeojectNode(project.Name));
            //await pform.ShowDialog(Global.currentWindow);
        }

        //public static System.Windows.Forms.MenuStrip GetMenuStrip()
        //{
        //    return Global.mainForm.Controller_GetMenuStrip();
        //}

        //public static System.Windows.Forms.DialogResult ShowMessageBox(string text, string caption, System.Windows.Forms.MessageBoxButtons buttons, System.Windows.Forms.MessageBoxIcon icon)
        //{
        //    return System.Windows.Forms.MessageBox.Show(text, caption, buttons, icon);
        //}
        //public static System.Windows.Forms.DialogResult ShowMessageBox(string text, string caption, System.Windows.Forms.MessageBoxButtons buttons)
        //{
        //    return System.Windows.Forms.MessageBox.Show(text, caption, buttons);
        //}

        public static void ShowForm(Avalonia.Controls.Window form)
        {
            Dispatcher.UIThread.Invoke(new Action(() => {
                form.Show(Global.mainWindow);
            }));
        }

        public static void ShowDialogForm(Avalonia.Controls.Window form)
        {
            Dispatcher.UIThread.Invoke(new Action(() => {
                form.ShowDialog(Global.mainWindow);
            }));
        }

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
                if (textFile == null)
                {
                    //Global.codeView. .mainForm.editorPage.CodeEditor.SetTextFile(null);
                    //Global.mainForm.mainTab.TabPages[0].Text = "-";
                }
                else
                {
                    //Global.mainForm.editorPage.CodeEditor.AbortInteractiveSnippet();
                    Global.codeView.SetTextFile(textFile);
                    //Global.mainForm.editorPage.CodeEditor.SetTextFile(textFile);
                    //Global.mainForm.mainTab.TabPages[0].Text = textFile.Name;
                    //Global.mainForm.mainTab.SelectedTab = Global.mainForm.mainTab.TabPages[0];
                }
            }

            //public static void ForceOpenCustomSelection(EventHandler applySelection, List<CodeEditor2.CodeEditor.ToolItem> cantidates)
            //{
            //    Global.mainForm.editorPage.CodeEditor.OpenCustomSelection(cantidates);
            //}

            //public static void ForceOpenAutoComplete(List<CodeEditor2.CodeEditor.AutocompleteItem> autocompleteItems)
            //{
            //    Global.mainForm.editorPage.CodeEditor.ForceOpenAutoComplete(autocompleteItems);
            //}

            public static void RequestReparse()
            {
                Global.codeView.RequestReparse();
//                Global.mainForm.editorPage.CodeEditor.RequestReparse();
            }

            public static Data.ITextFile GetTextFile()
            {
                return Global.codeView.TextFile;
            }

            //internal static void startInteractiveSnippet(Snippets.InteractiveSnippet interactiveSnippet)
            //{
            //    Global.mainForm.editorPage.CodeEditor.StartInteractiveSnippet(interactiveSnippet);
            //}

            //public static void AbortInteractiveSnippet()
            //{
            //    Global.mainForm.editorPage.CodeEditor.AbortInteractiveSnippet();
            //}

            //public static void AppendHighlight(int highlightStart, int highlightLast)
            //{
            //    Global.mainForm.editorPage.CodeEditor.codeTextbox.AppendHighlight(highlightStart, highlightLast);
            //}

            //public static void GetHighlightPosition(int highlightIndex, out int highlightStart, out int highlightLast)
            //{
            //    Global.mainForm.editorPage.CodeEditor.codeTextbox.GetHighlightPosition(highlightIndex, out highlightStart, out highlightLast);
            //}


            //public static void SelectHighlight(int highLightIndex)
            //{
            //    Global.mainForm.editorPage.CodeEditor.codeTextbox.SelectHighlight(highLightIndex);
            //}

            //public static int GetHighlightIndex(int index)
            //{
            //    return Global.mainForm.editorPage.CodeEditor.codeTextbox.GetHighlightIndex(index);
            //}

            //public static void ClearHighlight()
            //{
            //    Global.mainForm.editorPage.CodeEditor.codeTextbox.ClearHighlight();
            //}
            //public static void Refresh()
            //{
            //    Global.mainForm.Controller_RefreshCodeEditor();
            //}

            public static void ScrollToCaret()
            {
                Global.codeView.ScrollToCaret();
            }

        }

        public static class NavigatePanel
        {
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

            public static void GetSelectedNode(out CodeEditor2.NavigatePanel.NavigatePanelNode node)
            {
                node = Global.navigateView.GetSelectedNode();
            }

            //public static System.Windows.Forms.ContextMenuStrip GetContextMenuStrip()
            //{
            //    return Global.mainForm.navigatePanel.GetContextMenuStrip();
            //}

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

        //    public static void AddPage(ajkControls.TabControl.TabPage tabPage)
        //    {
        //        Global.mainForm.Controller_AddTabPage(tabPage);
        //    }

        //    public static void RemovePage(ajkControls.TabControl.TabPage tabPage)
        //    {
        //        Global.mainForm.Controller_RemoveTabPage(tabPage);
        //    }

        //    public static void Refresh()
        //    {
        //    }
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
