using Avalonia.Controls;
using Avalonia.Threading;
using CodeEditor2.NavigatePanel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2
{
    public static partial class Controller
    {
        public static class NavigatePanel
        {
            public static void UpdateVisualPost()
            {
                Dispatcher.UIThread.Post(
                    () =>
                    {
                        try
                        {
                            Global.navigateView.InvalidateVisual();
                        }
                        catch (Exception ex)
                        {
                            CodeEditor2.Controller.AppendLog("#Exception " + ex.Message, Avalonia.Media.Colors.Red);
                        }
                    }
                );
            }

            public static Action<CodeEditor2.NavigatePanel.NavigatePanelNode>? OpenInExploererClicked = null;
            public static void SelectNodePost(NavigatePanelNode node)
            {
                if (!Dispatcher.UIThread.CheckAccess())
                {
                    Dispatcher.UIThread.Post(() => { SelectNodePost(node); });
                    return;
                }
                Global.navigateView.SelectNode(node);
            }
            public static async Task SelectNodeAsync(NavigatePanelNode node)
            {
                await Dispatcher.UIThread.InvokeAsync(() => {
                    Global.navigateView.SelectNode(node);
                });
            }
            public static void RemoveNodePost(NavigatePanelNode node)
            {
                if (!Dispatcher.UIThread.CheckAccess())
                {
                    Dispatcher.UIThread.Post(() => { RemoveNodePost(node); });
                    return;
                }
                Global.navigateView.RemoveNode(node);
            }
            public static async Task RemoveNodeAsync(NavigatePanelNode node)
            {
                await Dispatcher.UIThread.InvokeAsync(() => {
                    Global.navigateView.RemoveNode(node);
                });
            }
            public static ContextMenu GetContextMenu()
            {
                if (!Dispatcher.UIThread.CheckAccess() && System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                return Global.navigateView.ContextMenu;
            }

            public static MenuItem? GetContextMenuItem(List<string> captions)
            {
                if (!Dispatcher.UIThread.CheckAccess() && System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                return Global.navigateView.getContextMenuItem(captions);
            }

            public static Data.Project GetProject(NavigatePanelNode node)
            {
                if (!Dispatcher.UIThread.CheckAccess() && System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                return Global.navigateView.GetProject(node);
            }

            public static async Task UpdateFolderAsync(NavigatePanelNode node)
            {
                await Global.navigateView.UpdateFolderAsync(node);
            }

            public static CodeEditor2.NavigatePanel.NavigatePanelNode? GetSelectedNode()
            {
                if (!Dispatcher.UIThread.CheckAccess() && System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                return Global.navigateView.GetSelectedNode();
            }
            public static async Task<CodeEditor2.NavigatePanel.NavigatePanelNode?> GetSelectedNodeAsync()
            {
                return await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    return Global.navigateView.GetSelectedNode();
                });
            }
            public static IReadOnlyList<CodeEditor2.NavigatePanel.NavigatePanelNode> GetSelectedNodes()
            {
                if (!Dispatcher.UIThread.CheckAccess() && System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                return Global.navigateView.GetSelectedNodes();
            }

            public static Data.File? GetSelectedFile()
            {
                if (!Dispatcher.UIThread.CheckAccess() && System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
                var node = Global.navigateView.GetSelectedNode();
                Data.File? file = node?.Item as Data.File;
                return file;
            }

            public static async Task<Data.File?> GetSelectedFileAsync()
            {
                return await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var node = Global.navigateView.GetSelectedNode();
                    Data.File? file = node?.Item as Data.File;
                    return file;
                });
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

        }
    }
}
