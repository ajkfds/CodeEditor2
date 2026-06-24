using AjkAvaloniaLibs.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CodeEditor2.NavigatePanel;
using System;
using System.Threading.Tasks;

namespace CodeEditor2
{
    public static partial class Controller
    {
        public static void AppendLog(string message)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() =>
                {
                    AppendLog(message);
                }, DispatcherPriority.Background);
                return;
            }
            Global.logView.AppendLog(message);
        }
        public static void AppendLog(string message, Avalonia.Media.Color color)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() =>
                {
                    AppendLog(message, color);
                }, DispatcherPriority.Background);
                return;
            }

            Global.logView.AppendLog(message, color);
        }
        public static void AppendLog(System.Exception exception)
        {
            AppendLog("Exception : " + exception.Message, Avalonia.Media.Colors.Red);
        }

        public static async Task<WeakReference<ListViewItem>> AppendLogAndGetItem(string message, Avalonia.Media.Color color)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    return await AppendLogAndGetItem(message, color);
                }
                );
                throw new Exception();
            }

            return Global.logView.AppendLogAndGetLastItem(message, color);
        }

        //public static Window GetMainWindow()
        //{
        //    return Global.mainWindow;
        //}

        /// <summary>
        /// 指定したダイアログをmainWindowの中央に表示します。
        /// Linux+X11環境でも正常に位置合わせされます。
        /// </summary>
        /// <param name="dialog">表示するダイアログウィンドウ</param>
        public static async Task ShowDialog(Window dialog)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                await Dispatcher.UIThread.InvokeAsync(() => ShowDialog(dialog));
                return;
            }

            var mainWindow = Global.mainWindow;
            if (mainWindow == null)
            {
                throw new InvalidOperationException("Main window is not available");
            }

            // dialog.Width/Heightが設定されていない場合、ActualWidth/ActualHeightを使用
            double dialogWidth = dialog.Width > 0 ? dialog.Width : dialog.Bounds.Width;
            double dialogHeight = dialog.Height > 0 ? dialog.Height : dialog.Bounds.Height;

            // それでも0の場合はデフォルト値を設定
            if (dialogWidth <= 0) dialogWidth = 400;
            if (dialogHeight <= 0) dialogHeight = 300;

            // mainWindowの中央に配置
            int x = (int)(mainWindow.Position.X + (mainWindow.Bounds.Width - dialogWidth) / 2);
            int y = (int)(mainWindow.Position.Y + (mainWindow.Bounds.Height - dialogHeight) / 2);
            dialog.Position = new PixelPoint(x, y);

            await dialog.ShowDialog(mainWindow);
        }

        //public static System.Drawing.Color GetBackColor()
        //{
        //    return Global.mainForm.BackColor;
        //}


        public static bool SelectText(Data.ITextFile textFile, int index, int length)
        {
            Data.TextReference textReference = new Data.TextReference(textFile, index, length);
            return true;
        }

        public static bool SelectText(Data.TextReference textReference)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() =>
                {
                    SelectText(textReference);
                });
                return true;
            }

            Data.ITextFile? file = textReference.TextFile;
            if (file == null) return false;

            Data.File? currentFile = NavigatePanel.GetSelectedFile();
            if (file != currentFile)
            {
                NavigatePanelNode node = file.NavigatePanelNode;
                NavigatePanel.SelectNodePost(node);
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
