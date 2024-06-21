using AjkAvaloniaLibs.Contorls;
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

        public void UpdateFolder(NavigatePanelNode node)
        {
            FileNode? fileNode = node as FileNode;
            if (fileNode != null)
            {
                NavigatePanelNode? parentNode = fileNode.Parent as NavigatePanelNode;
                if (parentNode == null) throw new System.Exception();
                UpdateFolder(parentNode);
            }

            FolderNode? folderNode = node as FolderNode;
            if (folderNode != null)
            {
                folderNode.Update();
            }
        }

        #region menuItmes

        private void initializeMenuItems()
        {
            {
                MenuItem menuItem_Add = CodeEditor2.Global.CreateMenuItem(
                    "Add", "MenuItem_Add"
                    //"CodeEditor2/Assets/Icons/plus.svg",
                    //Avalonia.Media.Color.FromArgb(100, 100, 150, 255)
                    );

                ContextMenu.Items.Add(menuItem_Add);
                {
                    MenuItem menuItem_AddFolder = CodeEditor2.Global.CreateMenuItem(
                    "Folder", 
                    "MenuItem_AddFolder",
                    "CodeEditor2/Assets/Icons/folder.svg",
                    Avalonia.Media.Color.FromArgb(100, 100, 150, 255)
                    );
                    menuItem_AddFolder.Click += menuItem_AddFolder_Click;
                    menuItem_Add.Items.Add(menuItem_AddFolder);
                }
            }

            {
                MenuItem menuItem_Delete = CodeEditor2.Global.CreateMenuItem("Delete", "MenuItem_Delete");
                ContextMenu.Items.Add(menuItem_Delete);
            }

            {
                MenuItem menuItem_OpenInExplorer = CodeEditor2.Global.CreateMenuItem(
                    "Open in Explorer", 
                    "MenuItem_OpenInExplorer",
                    "CodeEditor2/Assets/Icons/search.svg",
                    Avalonia.Media.Color.FromArgb(100, 200, 200, 255)
                    );
                menuItem_OpenInExplorer.Click += menuItem_OpenInExplorer_Click;
                ContextMenu.Items.Add(menuItem_OpenInExplorer);
            }
        }

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

        private async void menuItem_AddFolder_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            NavigatePanelNode? node = TreeControl.GetSelectedNode() as NavigatePanelNode;
            if (node == null) return;
            Project project = GetProject(node);

            string relativePath = getRelativeFolderPath(node);
            if (!relativePath.EndsWith(System.IO.Path.DirectorySeparatorChar)) relativePath += System.IO.Path.DirectorySeparatorChar;

            Tools.InputWindow window = new Tools.InputWindow("Create New Folder","new Folder Name");
            await window.ShowDialog(Controller.GetMainWindow());

            if (window.Cancel) return;
            string folderName = window.InputText.Trim();

            System.IO.Directory.CreateDirectory(project.GetAbsolutePath(relativePath + folderName));

            UpdateFolder(node);
        }

        //private async void menuItem_Add_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        //{
        //    Project? project = null;
        //    NavigatePanelNode? node = TreeControl.GetSelectedNode() as NavigatePanelNode;
        //    if (node == null) return;
        //    NavigatePanelNode rootNode = node.GetRootNode();

        //    if(rootNode is ProjectNode)
        //    {
        //        ProjectNode? projectNode = rootNode as ProjectNode;
        //        if (projectNode == null) throw new System.Exception();
        //        project = projectNode.Project;
        //    }
        //    if (project == null) throw new System.Exception();

        //    var newFile = await Global.mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        //    {
        //        Title = "Create New File"
        //    });
        //    if (newFile == null) return;

        //    string relativePath = project.GetRelativePath(newFile.Path.ToString());

        //    FileTypes.FileType? fileType = null;
        //    foreach(FileTypes.FileType fType in Global.FileTypes.Values)
        //    {
        //        if(fType.IsThisFileType(relativePath, project))
        //        {
        //            fileType = fType;
        //        }
        //    }
        //    if(fileType == null)
        //    {
                
        //    }
        //    else
        //    {
        //        fileType.CreateFile(relativePath,project);
        //    }

        //    FolderNode? folderNode = node.Parent as FolderNode;
        //    if (folderNode != null) folderNode.Update();
        //}

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
