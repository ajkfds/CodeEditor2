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
            public static void UpdateVisual()
            {
                Dispatcher.UIThread.Post(
                    new Action(() =>
                    {
                        try
                        {
                            Global.navigateView.InvalidateVisual();
                        }
                        catch (Exception ex)
                        {
                            CodeEditor2.Controller.AppendLog("#Exception " + ex.Message, Avalonia.Media.Colors.Red);
                        }
                    })
                );
            }

            public static Action<CodeEditor2.NavigatePanel.NavigatePanelNode>? OpenInExploererClicked = null;
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

            public static async Task UpdateFolder(NavigatePanelNode node)
            {
                await Global.navigateView.UpdateFolderAsync(node);
            }

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

        }
    }
}
