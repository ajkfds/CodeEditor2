using AjkAvaloniaLibs.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using CodeEditor2.NavigatePanel;
using System;
using System.Threading;
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
            if (Global.logView == null) return; // headless host (LSP / tests): no log view
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
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(() =>
                {
                    AppendLog(exception);
                }, DispatcherPriority.Background);
                return;
            }
            AppendLog("Exception : " + exception.Message, Avalonia.Media.Colors.Red);
            if (exception is System.OperationCanceledException) return;

            Tools.ExceptionWindow exceptionWindow = new Tools.ExceptionWindow(exception);
            Dispatcher.UIThread.Post(async () =>
            {
                await ShowDialogAsync(exceptionWindow);
            }, DispatcherPriority.Background);
        }

        public static async Task<WeakReference<ListViewItem>> AppendLogAndGetItemAsync(string message, Avalonia.Media.Color color)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    return await AppendLogAndGetItemAsync(message, color);
                }
                );
                throw new Exception();
            }

            return Global.logView.AppendLogAndGetLastItem(message, color);
        }


        public static async Task ShowDialogAsync(Window dialog,Window rootWindow)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                // InvokeAsync(Func<Task>) オーバーロードは ShowDialog 完了まで await 済み
                await Dispatcher.UIThread.InvokeAsync(() => ShowDialogAsync(dialog));
                return;
            }

            await dialogShowSemaphore.WaitAsync();
            try
            {
                if (rootWindow.WindowState == WindowState.Maximized)
                {
                    // 最大化時は Position がフレーム原点や負のオフセットを指すため
                    // 手動中央配置計算が実表示領域と乖離する。CenterOwner に任せる。
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                else
                {
                    // dialog.Width/Heightが設定されていない場合、ActualWidth/ActualHeightを使用
                    double dialogWidth = dialog.Width > 0 ? dialog.Width : dialog.Bounds.Width;
                    double dialogHeight = dialog.Height > 0 ? dialog.Height : dialog.Bounds.Height;

                    // それでも0の場合はデフォルト値を設定
                    if (dialogWidth <= 0) dialogWidth = rootWindow.Bounds.Width * 0.8;
                    if (dialogHeight <= 0) dialogHeight = rootWindow.Bounds.Height * 0.8;

                    // mainWindowの中央に配置。
                    // Position はデバイスピクセル (PixelPoint)、Bounds/Width は DIP のため、
                    // DPIスケール (RenderScaling) を反映して単位を合わせる。
                    // (混在計算だと DPI 100% 以外の環境で中央からずれ、画面外に出て
                    //  OS/WM が強制再配置し「別の場所に出る」原因になる)
                    double scaling = rootWindow.RenderScaling;
                    double mainWindowPixelWidth = rootWindow.Bounds.Width * scaling;
                    double mainWindowPixelHeight = rootWindow.Bounds.Height * scaling;
                    double dialogPixelWidth = dialogWidth * scaling;
                    double dialogPixelHeight = dialogHeight * scaling;
                    int x = (int)(rootWindow.Position.X + (mainWindowPixelWidth - dialogPixelWidth) / 2);
                    int y = (int)(rootWindow.Position.Y + (mainWindowPixelHeight - dialogPixelHeight) / 2);
                    dialog.Position = new PixelPoint(x, y);
                }

                await dialog.ShowDialog(rootWindow);
            }
            finally
            {
                dialogShowSemaphore.Release();
            }
        }


        /// <summary>
        /// 指定したダイアログをmainWindowの中央に表示します。
        /// Linux+X11環境でも正常に位置合わせされます。
        /// </summary>
        /// <param name="dialog">表示するダイアログウィンドウ</param>
        // 同一時刻に複数のモーダルダイアログが開くことを防ぐ。
        // 2つ目以降の ShowDialog は最初のモーダルが閉じるまで待機する。
        // (同時多重時、後発のダイアログが activation 待ちで mainWindow の前面に出ず、
        //  背後に表示される問題の対策)
        private static readonly SemaphoreSlim dialogShowSemaphore = new SemaphoreSlim(1, 1);

        public static async Task ShowDialogAsync(Window dialog)
        {
            if (Global.mainWindow == null) return;
            await ShowDialogAsync(dialog, Global.mainWindow);
        }

        /// <summary>
        /// YesNoダイアログをmainWindowの中央に表示し、ユーザの回答を取得する。
        /// UIスレッド以外から呼び出した場合も正しく回答完了まで待機する。
        /// (TextFile.FileCheckBackgroundAsync 等の background スレッド経路向け。
        ///  Dispatcher.UIThread.InvokeAsync(async () => { ... await ShowDialogAsync ... })
        ///  形式は async lambda が Action (async void) に解決され回答待ちが行われないため、
        ///  その代わりに使用する)
        /// </summary>
        public static async Task<bool> ShowDialogYesNoAsync(string title, string caption)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                return await Dispatcher.UIThread.InvokeAsync(() => ShowDialogYesNoAsync(title, caption));
            }

            Tools.YesNoWindow dialog = new Tools.YesNoWindow(title, caption);
            await ShowDialogAsync(dialog);
            return dialog.Yes;
        }

        //public static System.Drawing.Color GetBackColor()
        //{
        //    return Global.mainForm.BackColor;
        //}

        public static void ShowWindow(Window window)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Invoke(() => ShowWindow(window));
                return;
            }

            var mainWindow = Global.mainWindow;
            if (mainWindow == null)
            {
                throw new InvalidOperationException("Main window is not available");
            }

            if (mainWindow.WindowState == WindowState.Maximized)
            {
                // 最大化時は Position がフレーム原点や負のオフセットを指すため
                // 手動中央配置計算が実表示領域と乖離する。CenterOwner に任せる。
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else
            {
                // dialog.Width/Heightが設定されていない場合、ActualWidth/ActualHeightを使用
                double dialogWidth = window.Width > 0 ? window.Width : window.Bounds.Width;
                double dialogHeight = window.Height > 0 ? window.Height : window.Bounds.Height;

                // それでも0の場合はデフォルト値を設定
                if (dialogWidth <= 0) dialogWidth = mainWindow.Bounds.Width * 0.8;
                if (dialogHeight <= 0) dialogHeight = mainWindow.Bounds.Height * 0.8;

                // mainWindowの中央に配置。
                // Position はデバイスピクセル (PixelPoint)、Bounds/Width は DIP のため、
                // DPIスケール (RenderScaling) を反映して単位を合わせる。
                // (混在計算だと DPI 100% 以外の環境で中央からずれ、画面外に出て
                //  OS/WM が強制再配置し「別の場所に出る」原因になる)
                double scaling = mainWindow.RenderScaling;
                double mainWindowPixelWidth = mainWindow.Bounds.Width * scaling;
                double mainWindowPixelHeight = mainWindow.Bounds.Height * scaling;
                double dialogPixelWidth = dialogWidth * scaling;
                double dialogPixelHeight = dialogHeight * scaling;
                int x = (int)(mainWindow.Position.X + (mainWindowPixelWidth - dialogPixelWidth) / 2);
                int y = (int)(mainWindow.Position.Y + (mainWindowPixelHeight - dialogPixelHeight) / 2);
                window.Position = new PixelPoint(x, y);
            }

            window.Show();
        }

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

        internal static async Task addProjectAsync(Data.Project project)
        {
            if (Global.Projects.ContainsKey(project.Name))
            {
                System.Diagnostics.Debugger.Break();
                //return null;
            }
            else
            {
                await addProjectToNavigatePanelAsync(project);
                
                await loadProjectAsync(project);
            }
        }

        /// <summary>
        /// プロジェクトを Global.Projects と NavigatePanel に登録する(まだ parse は行わない)。
        /// Property 設定のために先に project node を表示したい場合に使用する。
        /// </summary>
        internal static async Task addProjectToNavigatePanelAsync(Data.Project project)
        {
            AppendLog("Add Project "+project.Name);
            if (Global.Projects.ContainsKey(project.Name)) return;
            Global.Projects.Add(project.Name, project);
            await Global.navigateView.AddProjectAsync(project);
        }

        /// <summary>
        /// NavigatePanel に登録済みのプロジェクトの parse を行う。
        /// </summary>
        internal static async Task loadProjectAsync(Data.Project project)
        {
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

        /// <summary>
        /// プロジェクトを Global.Projects と NavigatePanel から取り除く。
        /// まだ parse が走っていない段階で property 設定をキャンセルした場合などに使用する。
        /// </summary>
        internal static void removeProject(Data.Project project)
        {
            if (Global.Projects.TryGetValue(project.Name, out Data.Project? existing) && existing == project)
            {
                Global.Projects.Remove(project.Name);
            }
            ProjectNode? projectNode = Global.navigateView.GetProjectNode(project.Name);
            if (projectNode != null)
            {
                Global.navigateView.RemoveNode(projectNode);
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
