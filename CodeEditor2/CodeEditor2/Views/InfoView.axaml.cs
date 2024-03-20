using Avalonia.Controls;
using CodeEditor2.MessageView;
using System.Security.Cryptography.X509Certificates;

namespace CodeEditor2.Views
{
    public partial class InfoView : UserControl
    {
        public InfoView()
        {
            InitializeComponent();
            Global.infoView = this;

            //MessageView.NodeClicked += MessageView_NodeClicked;
        }

        //public void MessageView_NodeClicked(AjkAvaloniaLibs.Contorls.TreeNode node)
        //{
        //    MessageNode _node = node as MessageNode;
        //    if (node == null) return;
        //    node.OnSelected();
        //}

        public void UpdateMessages(CodeEditor.ParsedDocument parsedDocument)
        {
            MessageView.Nodes.Clear();
            if (parsedDocument != null)
            {
                foreach (CodeEditor.ParsedDocument.Message message in parsedDocument.Messages)
                {
                    MessageView.Nodes.Add(message.CreateMessageNode());
                }
            }

            MessageView.InvalidateVisual();
        }

    }
}
