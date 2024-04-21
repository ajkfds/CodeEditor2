using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using CodeEditor2.Data;
using Avalonia.Media;

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
        public static Action<FileNode> FileNodeCreated;

        public override void UpdateVisual()
        {
            Image = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2/Assets/Icons/questionDocument.svg",
                    Avalonia.Media.Color.FromArgb(100, 100, 100, 100)
                    );
        }
        public virtual File FileItem
        {
            get { return Item as File; }
        }

        public override string Text
        {
            get { return FileItem.Name; }
        }

        public override void OnSelected()
        {
            //Data.ITextFile textFile = FileItem as Data.ITextFile;
            //CodeEditor2.Controller.NavigatePanel.GetContextMenuStrip().Items["openWithExploererTsmi"].Visible = true;

        }
    }
}
