using AjkAvaloniaLibs.Contorls;
using Avalonia.Controls;
using CodeEditor2.Data;
using CodeEditor2.NavigatePanel;

namespace CodeEditor2.Views
{
    public partial class NavigateView : UserControl
    {
        public NavigateView()
        {
            InitializeComponent();

            Global.navigateView = this;

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
    }
}
