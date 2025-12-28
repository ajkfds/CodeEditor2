using AjkAvaloniaLibs.Controls;
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
    public static partial class Controller
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
            await Global.navigateView.AddProject(project);

            // parse project
            CodeEditor2.Tools.ParseProject parser = new Tools.ParseProject();
            ProjectNode? projectNode = Global.navigateView.GetProjectNode(project.Name);
            if (projectNode == null) return;
            
            await Dispatcher.UIThread.InvokeAsync(
                async () =>
                {
                    await parser.Run(projectNode);
                }
            );
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
