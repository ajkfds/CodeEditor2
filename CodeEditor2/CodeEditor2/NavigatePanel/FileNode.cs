using Avalonia.Controls;
using Avalonia.Threading;
using CodeEditor2.Data;
using System;

namespace CodeEditor2.NavigatePanel
{
    public class FileNode : NavigatePanelNode
    {
        protected FileNode() { }
        public FileNode(File file) : base(file)
        {
            UpdateVisual();
            if (FileNodeCreated != null) FileNodeCreated(this);
        }
        public static Action<FileNode>? FileNodeCreated;

        public override void UpdateVisual()
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Invoke(() => { UpdateVisual(); });
                return;
            }

            Image = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2/Assets/Icons/questionDocument.svg",
                    CodeEditor2.Global.Color_Gray
                    );

            string text = "null";
            File? file = FileItem;
            if (file != null) text = file.Name;
            Text = text;
        }
        public virtual File? FileItem
        {
            get { return Item as File; }
        }

        public override void OnSelected()
        {
            base.OnSelected();

            File? file = FileItem;
            //if (file!= null) 
            //{
            //    Dispatcher.UIThread.Post( async() => { 
            //        await file.UpdateAsync();
            //        file.CheckStatus();
            //    });
            //}
        }

        public static new Action<ContextMenu>? CustomizeSpecificNodeContextMenu;
        protected override Action<ContextMenu>? customizeSpecificNodeContextMenu => CustomizeSpecificNodeContextMenu;


    }
}
