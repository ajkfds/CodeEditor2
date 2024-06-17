using AjkAvaloniaLibs.Contorls;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CodeEditor2.Data;
using CodeEditor2.NavigatePanel;
using Microsoft.CodeAnalysis;
using Splat;
using System.Text;

namespace CodeEditor2.Views
{
    public partial class NavigateView : UserControl
    {
        public NavigateView()
        {
            InitializeComponent();
            initializeMenuItems();

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

        public ProjectNode GetProjectNode(string projectName)
        {
            ProjectNode ret = null;
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

        #region menuItmes

        private void initializeMenuItems()
        {
            {
                MenuItem menuItem_Add = CodeEditor2.Global.CreateMenuItem("Add", "MenuItem_Add");
                menuItem_Add.Click += menuItem_Add_Click;
                ContextMenu.Items.Add(menuItem_Add);
            }

            {
                MenuItem menuItem_Delete = CodeEditor2.Global.CreateMenuItem("Delete", "MenuItem_Delete");
                ContextMenu.Items.Add(menuItem_Delete);
            }

            {
                MenuItem menuItem_OpenInExplorer = CodeEditor2.Global.CreateMenuItem("Open in Explorer", "MenuItem_OpenInExplorer","search", Avalonia.Media.Color.FromArgb(100, 200, 200, 255));
                menuItem_OpenInExplorer.Click += menuItem_OpenInExplorer_Click;
                ContextMenu.Items.Add(menuItem_OpenInExplorer);
            }
        }

        private async void menuItem_Add_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var file = await Global.mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Create New File"
            });
            if (file == null) return;

//            string relativePath = 




        }

        private void menuItem_OpenInExplorer_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            TreeNode? node = TreeControl.GetSelectedNode();
            if (node == null) return;

            if (node is FolderNode)
            {
                FolderNode? folderNode = node as FolderNode;
                if (folderNode == null) throw new System.Exception();
                Data.Folder folder = folderNode.Folder;
                if (folder == null || folder.Project == null) return;
                string folderPath = folder.Project.GetAbsolutePath(folder.RelativePath).Replace('\\',System.IO.Path.DirectorySeparatorChar);
                
                if (System.OperatingSystem.IsLinux())
                {
                    System.Diagnostics.Process.Start("thunar "+folderPath+" &");
                }
                else
                {
                    System.Diagnostics.Process.Start("EXPLORER.EXE", folderPath);
                }
            }
            else if (node is FileNode)
            {
                FileNode? fileNode = node as FileNode;
                if (fileNode == null) throw new System.Exception();
                Data.File file = fileNode.FileItem;
                if (file == null || file.Project == null) return;
                string filePath = file.Project.GetAbsolutePath(file.RelativePath).Replace('\\', System.IO.Path.DirectorySeparatorChar);

                if (System.OperatingSystem.IsLinux())
                {
                }
                else
                {
                    System.Diagnostics.Process.Start("EXPLORER.EXE", "/select,\"" + file.Project.GetAbsolutePath(file.RelativePath) + "\"");
                }
            }
        }




    }

    #endregion
}
