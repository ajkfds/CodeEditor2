using AjkAvaloniaLibs.Controls;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CodeEditor2.Data;
using CodeEditor2.Tools;
using HarfBuzzSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CodeEditor2.NavigatePanel
{
    public class NavigatePanelNode : AjkAvaloniaLibs.Controls.TreeNode
    {
        protected NavigatePanelNode()
        {
            setImage();
        }

        private void setImage()
        {
            Image =  AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                "CodeEditor2/Assets/Icons/document.svg",
                Avalonia.Media.Color.FromArgb(100, 100, 100, 100)
                );
        }
        public long ObjectID
        {
            get
            {
                bool firstTime;
                return Global.ObjectIDGenerator.GetId(this, out firstTime);
            }
        }



        public NavigatePanelNode(Item item)
        {
            itemRef = new WeakReference<Item>(item);
            if (NavigatePanelNodeCreated != null) NavigatePanelNodeCreated(this);
        }


        private bool link = false;

        //public bool Link
        //{
        //    get
        //    {
        //        return link;
        //    }
        //    set
        //    {
        //        link = value;
        //    }
        //}

        private WeakReference<Item>? itemRef;
        public Item? Item
        {
            get
            {
                Item? ret;
                if (itemRef == null) return null;
                if (!itemRef.TryGetTarget(out ret)) return null;
                return ret;
            }
        }

        public static Action<NavigatePanelNode>? NavigatePanelNodeCreated;

        /// <summary>
        /// update this node and children
        /// </summary>
        public virtual void Update()
        {
        }

        public virtual void UpdateVisual()
        {
        }

        public virtual void Dispose()
        {

        }

        /// <summary>
        /// update all nodes under this node
        /// </summary>
        public virtual void HierarchicalUpdate()
        {
            HierarchicalUpdate(0);
        }

        public virtual void HierarchicalUpdate(int depth)
        {
            Update();
            if (depth > 100) return;
            foreach (NavigatePanelNode node in Nodes)
            {
                node.HierarchicalUpdate(depth + 1);
            }
        }
        public override void OnExpand()
        {
            Task.Run(
                async () =>
                {
                    await HierarchicalVisibleUpdateAsync();
                }
            );
        }

        public override void OnCollapse()
        {
            Task.Run(
                async () =>
                {
                    await HierarchicalVisibleUpdateAsync();
                }
            );
        }

        public async virtual Task HierarchicalVisibleUpdateAsync()
        {
            await HierarchicalVisibleUpdateAsync(0, IsExpanded);
        }

        public async virtual Task HierarchicalVisibleUpdateAsync(int depth, bool expanded)
        {
            Update();
            if (depth > 100) return;
            if (!expanded) return;
            await Dispatcher.UIThread.InvokeAsync(
                async () =>
                {
                    foreach (NavigatePanelNode node in Nodes)
                    {
                        await node.HierarchicalVisibleUpdateAsync(depth + 1, node.IsExpanded);
                    }
                }
            );
        }



        public virtual void ShowPropertyForm()
        {

        }

        public NavigatePanelNode GetRootNode()
        {
            NavigatePanelNode? parent = Parent as NavigatePanelNode;
            if (parent == null) return this;
            return parent.GetRootNode();
        }
        public override void OnSelected()
        {
            createContextMenu();
        }

        public Project GetProject()
        {
            Project? project = null;
            NavigatePanelNode rootNode = GetRootNode();
            if (rootNode is ProjectNode)
            {
                ProjectNode? projectNode = rootNode as ProjectNode;
                if (projectNode == null) throw new System.Exception();
                project = projectNode.Project;
            }
            if (project == null) throw new System.Exception();
            return project;
        }


        public static Action<ContextMenu>? CustomizeNavigateNodeContextMenu;
        // Context Menu
        //public virtual void CustomizeContextMenu(ContextMenu contextMenu)
        //{

        //}
        private void createContextMenu()
        {
            // re-generate context menu
            ContextMenu contextMenu = Controller.NavigatePanel.GetContextMenu();
            contextMenu.Items.Clear();

            {
                MenuItem menuItem_Add = CodeEditor2.Global.CreateMenuItem(
                    "Add", "MenuItem_Add"
                    //"CodeEditor2/Assets/Icons/plus.svg",
                    //Avalonia.Media.Color.FromArgb(100, 100, 150, 255)
                    );
                contextMenu.Items.Add(menuItem_Add);

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

                {
                    MenuItem menuItem_AddBlankFile = CodeEditor2.Global.CreateMenuItem(
                    "Blank File",
                    "MenuItem_AddBlankFile",
                    "CodeEditor2/Assets/Icons/paper2.svg",
                    Avalonia.Media.Color.FromArgb(100, 100, 100, 255)
                    );
                    menuItem_AddBlankFile.Click += menuItem_AddBlankFile_Click;
                    menuItem_Add.Items.Add(menuItem_AddBlankFile);
                }

            }
            {
                MenuItem menuItem_Delete = CodeEditor2.Global.CreateMenuItem("Delete", "MenuItem_Delete");
                menuItem_Delete.Click += menuItem_Delete_Click;
                contextMenu.Items.Add(menuItem_Delete);
            }
            contextMenu.Items.Add(new Separator());

            CustomizeNavigateNodeContextMenu?.Invoke(contextMenu);

            {
                MenuItem menuItem_OpenInExplorer = CodeEditor2.Global.CreateMenuItem(
                    "Open in Explorer",
                    "MenuItem_OpenInExplorer",
                    "CodeEditor2/Assets/Icons/search.svg",
                    Avalonia.Media.Color.FromArgb(100, 200, 200, 255)
                    );
                menuItem_OpenInExplorer.Click += menuItem_OpenInExplorer_Click;
                contextMenu.Items.Add(menuItem_OpenInExplorer);
            }

            {
                MenuItem menuItem_OpenProperty = CodeEditor2.Global.CreateMenuItem(
                    "Property",
                    "MenuItem_Property",
                    "CodeEditor2/Assets/Icons/gear.svg",
                    Avalonia.Media.Color.FromArgb(100, 200, 200, 255)
                    );
                menuItem_OpenProperty.Click += menuItem_OpenProperty_Click;
                contextMenu.Items.Add(menuItem_OpenProperty);
            }
        }

        public async void menuItem_OpenProperty_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            Tools.ItemPropertyForm form = new Tools.ItemPropertyForm(this);
            await form.ShowDialog(Controller.GetMainWindow());
        }

        public virtual void InitializePropertyForm(ItemPropertyForm form)
        {
            Project project = GetProject();
            foreach (var property in project.ProjectProperties.Values)
            {
                property.InitializePropertyForm(form,this,project);
            }
        }
        public virtual void UpdatePropertyForm(Tools.ItemPropertyForm form)
        {

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

        private string getRelativeFolderPath(NavigatePanelNode node)
        {
            FileNode? fileNode = node as FileNode;
            if (fileNode != null)
            {
                NavigatePanelNode? parentNode = fileNode.Parent as NavigatePanelNode;
                if (parentNode == null) throw new System.Exception();
                return getRelativeFolderPath(parentNode);
            }

            FolderNode? folderNode = node as FolderNode;
            if (folderNode != null && folderNode.Folder != null)
            {
                return folderNode.Folder.RelativePath;
            }
            return "";
        }
        private async void menuItem_AddFolder_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            NavigatePanelNode? node = Controller.NavigatePanel.GetSelectedNode(); 
            if (node == null) return;
            Project project = GetProject();

            string relativePath = getRelativeFolderPath(node);
            if (!relativePath.EndsWith(System.IO.Path.DirectorySeparatorChar)) relativePath += System.IO.Path.DirectorySeparatorChar;

            Tools.InputWindow window = new Tools.InputWindow("Create New Folder", "new Folder Name");
            await window.ShowDialog(Controller.GetMainWindow());

            if (window.Cancel) return;
            string folderName = window.InputText.Trim();

            System.IO.Directory.CreateDirectory(project.GetAbsolutePath(relativePath + folderName));

            UpdateFolder(node);
        }

        private async void menuItem_AddBlankFile_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            NavigatePanelNode? node = Controller.NavigatePanel.GetSelectedNode();
            if (node == null) return;
            Project project = GetProject();

            string relativePath = getRelativeFolderPath(node);
            if (!relativePath.EndsWith(System.IO.Path.DirectorySeparatorChar)) relativePath += System.IO.Path.DirectorySeparatorChar;

            Tools.InputWindow window = new Tools.InputWindow("Create Blank File", "new File Name");
            await window.ShowDialog(Controller.GetMainWindow());

            if (window.Cancel) return;
            string fileName = window.InputText.Trim();
            string path = project.GetAbsolutePath(relativePath + fileName);

            if (System.IO.File.Exists(path))
            {
                CodeEditor2.Controller.AppendLog("! already exist " + path, Avalonia.Media.Colors.Red);
            }
            else
            {
                try
                {
                    using (System.IO.StreamWriter sw = new System.IO.StreamWriter(path))
                    {
                    }
                }
                catch (Exception ex)
                {
                    Controller.AppendLog("** error : NavigatePanelMenu.addBlankFile", Avalonia.Media.Colors.Red);
                    Controller.AppendLog(ex.Message, Avalonia.Media.Colors.Red);
                }
            }

            CodeEditor2.Controller.NavigatePanel.UpdateFolder(node);
        }

        private async void menuItem_Delete_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            NavigatePanelNode? node = Controller.NavigatePanel.GetSelectedNode();
            if (node == null) return;
            Project project = GetProject();

            if (node is ProjectNode) return;

            if (node is FileNode)
            {
                FileNode fileNode = (FileNode)node;
                if (fileNode.FileItem == null) return;
                string relativePath = fileNode.FileItem.RelativePath;

                Tools.YesNoWindow window = new Tools.YesNoWindow("Delete File", "delete " + relativePath + " ?");
                await window.ShowDialog(Controller.GetMainWindow());

                if (!window.Yes) return;

                try
                {
                    System.IO.File.Delete(project.GetAbsolutePath(relativePath));
                }
                catch (System.Exception ex)
                {
                    CodeEditor2.Controller.AppendLog("failed to delete " + relativePath + ":" + ex.Message);
                }
                UpdateFolder(node);
                return;
            }

            if (node is FolderNode)
            {
                FolderNode folderNode = (FolderNode)node;
                if (folderNode.Folder == null) return;
                string relativePath = folderNode.Folder.RelativePath;

                Tools.YesNoWindow window = new Tools.YesNoWindow("Delete File", "delete " + relativePath + " ?");
                await window.ShowDialog(Controller.GetMainWindow());

                if (!window.Yes) return;

                try
                {
                    System.IO.Directory.Delete(project.GetAbsolutePath(relativePath));
                }
                catch (System.Exception ex)
                {
                    CodeEditor2.Controller.AppendLog("failed to delete " + relativePath + ":" + ex.Message);
                }

                NavigatePanelNode? parentNode = node.Parent as NavigatePanelNode;
                if (parentNode != null)
                {
                    UpdateFolder(parentNode);
                }

                return;
            }


        }


        private void menuItem_OpenInExplorer_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            NavigatePanelNode? node = Controller.NavigatePanel.GetSelectedNode();
            if (node == null) return;

            if(Controller.NavigatePanel.OpenInExploererClicked != null) Controller.NavigatePanel.OpenInExploererClicked(node);
        }

    }
}
