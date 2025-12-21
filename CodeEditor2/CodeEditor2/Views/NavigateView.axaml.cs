using AjkAvaloniaLibs.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CodeEditor2.Data;
using CodeEditor2.NavigatePanel;
using Microsoft.CodeAnalysis;
using Splat;
using System.Collections.Generic;
using System.Text;

namespace CodeEditor2.Views
{
    public partial class NavigateView : UserControl
    {
        public NavigateView()
        {
            InitializeComponent();

            Global.navigateView = this;
            TreeControl.Background = new SolidColorBrush(Color.FromRgb(10,10,10));
            TreeControl.SelectedForegroundColor = Color.FromRgb(255, 255, 255);
            TreeControl.ToggleButtonColor = Color.FromRgb(200, 200, 200);
        }

        internal async System.Threading.Tasks.Task AddProject(Project project)
        {
            ProjectNode pNode = new NavigatePanel.ProjectNode(project);
            TreeControl.Nodes.Add(pNode);

            await pNode.UpdateAsync();
        }

        internal NavigatePanelNode? GetSelectedNode()
        {
            NavigatePanelNode? node = TreeControl.GetSelectedNode() as NavigatePanelNode;
            return node;
        }

        internal void SelectNode(NavigatePanelNode node)
        {
            TreeControl.SelectNode(node);
        }

        public ProjectNode? GetProjectNode(string projectName)
        {
            ProjectNode? ret = null;
            foreach (NavigatePanel.NavigatePanelNode node in TreeControl.Nodes)
            {
                if (node is ProjectNode)
                {
                    ProjectNode? pNode = node as ProjectNode;
                    if (pNode == null) throw new System.Exception();
                    if (pNode.Project.Name == projectName) ret = pNode;
                }
            }
            return ret;
        }

        public Project GetProject(NavigatePanelNode node)
        {
            Project? project = null;
            NavigatePanelNode rootNode = node.GetRootNode();
            if (rootNode is ProjectNode)
            {
                ProjectNode? projectNode = rootNode as ProjectNode;
                if (projectNode == null) throw new System.Exception();
                project = projectNode.Project;
            }
            if (project == null) throw new System.Exception();
            return project;
        }

        public async System.Threading.Tasks.Task UpdateFolderAsync(NavigatePanelNode node)
        {
            FileNode? fileNode = node as FileNode;
            if (fileNode != null)
            {
                NavigatePanelNode? parentNode = fileNode.Parent as NavigatePanelNode;
                if (parentNode == null) throw new System.Exception();
                await UpdateFolderAsync(parentNode);
            }

            FolderNode? folderNode = node as FolderNode;
            if (folderNode != null)
            {
                await folderNode.UpdateAsync();
            }
        }

        #region menuItmes


        public MenuItem? getContextMenuItem(List<string> captions)
        {
            if (captions.Count < 0) return null;
            foreach (MenuItem? item in ContextMenu.Items)
            {
                if (item == null) continue;
                if (item.Header == null) continue;
                if ((string)item.Header == captions[0])
                {
                    captions.RemoveAt(0);
                    return getMenuItem(item, captions);
                }
            }
            return null;
        }
        private MenuItem? getMenuItem(MenuItem menuItem, List<string> captions)
        {
            if (captions.Count < 1) return menuItem;
            foreach (MenuItem? item in menuItem.Items)
            {
                if (item == null) continue;
                if (item.Header == null) continue;
                if ((string)item.Header == captions[0])
                {
                    captions.RemoveAt(0);
                    return getMenuItem(item, captions);
                }
            }
            return null;
        }


        private string getRelativeFolderPath(NavigatePanelNode node)
        {
            FileNode? fileNode = node as FileNode;
            if(fileNode != null)
            {
                NavigatePanelNode? parentNode = fileNode.Parent as NavigatePanelNode;
                if (parentNode == null) throw new System.Exception(); 
                return getRelativeFolderPath(parentNode);
            }

            FolderNode? folderNode = node as FolderNode;
            if(folderNode!= null)
            {
                return folderNode.Folder.RelativePath;
            }
            return "";
        }



    }

    #endregion
}
