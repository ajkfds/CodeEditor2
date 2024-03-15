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
            if (FileNodeCreated != null) FileNodeCreated(this);
        }
        public static Action<FileNode> FileNodeCreated;

        public override IImage? Image
        {
            get
            {
                return AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap("CodeEditor2/Assets/Icons/questionPaper.svg");
            }
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
            //codeEditor.Controller.NavigatePanel.GetContextMenuStrip().Items["openWithExploererTsmi"].Visible = true;

        }
    }
}
