using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using CodeEditor2.Data;

namespace CodeEditor2.NavigatePanel
{
    public class FolderNode : NavigatePanelNode
    {
        protected FolderNode() { }

        public FolderNode(Folder folder) : base(folder)
        {
            UpdateVisual();
            if (FolderNodeCreated != null) FolderNodeCreated(this);
        }
        public static Action<FolderNode>? FolderNodeCreated;

        //        private System.WeakReference<Data.Folder> folderRef;
        public virtual Folder? Folder
        {
            get
            {
                return Item as Folder;
            }
        }

        public override void UpdateVisual()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            }

            List<AjkAvaloniaLibs.Libs.Icons.OverrideIcon> overrideIcons = new List<AjkAvaloniaLibs.Libs.Icons.OverrideIcon>();

            if (Folder != null && Folder.Link)
            {
                overrideIcons.Add(new AjkAvaloniaLibs.Libs.Icons.OverrideIcon()
                {
                    SvgPath = "CodeEditor2/Assets/Icons/share.svg",
                    Color = Avalonia.Media.Color.FromArgb(255, 255, 255, 200),
                    OverridePosition = AjkAvaloniaLibs.Libs.Icons.OverridePosition.Fill
                });
            }

            Image = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2/Assets/Icons/folder.svg",
                    Color.FromArgb(100, 100, 150, 255),
                    overrideIcons
                    );
            string text = "null";
            Folder? folder = Folder;
            if (folder != null) text = folder.Name;
            Text = text;
        }

        public override void OnSelected()
        {
            base.OnSelected();

            try
            {
                Task.Run(UpdateAsync);
            }
            catch
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            }
        }
        public override async Task UpdateAsync()
        {
            Folder? folder = Folder;
            if (folder == null)
            {
                Image = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2/Assets/Icons/questionDocument.svg",
                    Avalonia.Media.Color.FromArgb(100, 200, 200, 200),
                    "CodeEditor2/Assets/Icons/questionDocument.svg",
                    Avalonia.Media.Color.FromArgb(255, 255, 255, 200)
                    );
                Nodes.Clear();
                return;
            }

            await folder.UpdateAsync();

            List<Item> addItems = new List<Item>();
            foreach (Item item in folder.Items.Values)
            {
                addItems.Add(item);
            }

            List<NavigatePanelNode> removeNodes = new List<NavigatePanelNode>();
            foreach (NavigatePanelNode node in Nodes)
            {
                removeNodes.Add(node);
            }

            foreach (Item item in Folder.Items.Values)
            {
                if (removeNodes.Contains(item.NavigatePanelNode))
                {
                    removeNodes.Remove(item.NavigatePanelNode);
                }
                if (Nodes.Contains(item.NavigatePanelNode))
                {
                    addItems.Remove(item);
                }
            }

            foreach (NavigatePanelNode nodes in removeNodes)
            {
                Nodes.Remove(nodes);
            }

            foreach (Item item in addItems)
            {
                if (item == null) continue;
                Nodes.Add(item.NavigatePanelNode);
            }
        }

    }
}
