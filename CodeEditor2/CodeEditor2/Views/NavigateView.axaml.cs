using AjkAvaloniaLibs.Contorls;
using Avalonia.Controls;
using Avalonia.Media;
using CodeEditor2.Data;
using CodeEditor2.NavigatePanel;
using Microsoft.CodeAnalysis;
using System.Text;

namespace CodeEditor2.Views
{
    public partial class NavigateView : UserControl
    {
        public NavigateView()
        {
            InitializeComponent();
            initializeMenuItes();

            Global.navigateView = this;
            TreeControl.Background = new SolidColorBrush(Color.FromRgb(10,10,10));
        }

        public void AddProject(Project project)
        {
            ProjectNode pNode = new NavigatePanel.ProjectNode(project);
            TreeControl.Nodes.Add(pNode);

            pNode.Update();
        }

        public NavigatePanelNode? GetSelectedNode()
        {
            NavigatePanelNode? node = TreeControl.GetSelectedNode() as NavigatePanelNode;
            return node;
        }

        public ProjectNode GetPeojectNode(string projectName)
        {
            ProjectNode ret = null;
            foreach (NavigatePanel.NavigatePanelNode node in TreeControl.Nodes)
            {
                if (node is ProjectNode)
                {
                    ProjectNode pnode = node as ProjectNode;
                    if (pnode.Project.Name == projectName) ret = pnode;
                }
            }
            return ret;
        }

        #region menuItmes

        private void initializeMenuItes()
        {
            {
                MenuItem_OpenInExplorer.Click += MenuItem_OpenInExplorer_Click;

                Image image = new Image();
                image.Source = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                            "CodeEditor2/Assets/Icons/search.svg",
                            Avalonia.Media.Color.FromArgb(100, 200, 200, 255)
                            );
                image.Width = 12;
                image.Height = 12;
                MenuItem_OpenInExplorer.Icon = image;
            }
        }

        private void MenuItem_OpenInExplorer_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            TreeNode? node = TreeControl.GetSelectedNode();
            if (node == null) return;

            string command = "";
            if (System.OperatingSystem.IsLinux())
            {
                command = "thunar&";
            }
            else
            {
                command = "EXPLORER.EXE";
            }

            if (node is FolderNode)
            {
                Data.Folder folder = (node as FolderNode).Folder;
                if (folder == null || folder.Project == null) return;
                System.Diagnostics.Process.Start(command, folder.Project.GetAbsolutePath(folder.RelativePath));

            }
            else if (node is FileNode)
            {
                Data.File file = (node as FileNode).FileItem;
                if (file == null || file.Project == null) return;
                System.Diagnostics.Process.Start(command, "/select,\"" + file.Project.GetAbsolutePath(file.RelativePath) + "\"");
            }
        }




    }

    #endregion
}
