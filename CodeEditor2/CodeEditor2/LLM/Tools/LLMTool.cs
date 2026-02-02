using CodeEditor2.Data;
using CodeEditor2Plugin;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.LLM.Tools
{
    public abstract class LLMTool
    {
        public abstract AIFunction GetAIFunction();

        public Data.Project? GetProject()
        {
            var node = CodeEditor2.Controller.NavigatePanel.GetSelectedNode();
            if (node == null) return null;

            CodeEditor2.Data.Project? project = null;
            if (node is CodeEditor2.NavigatePanel.FileNode)
            {
                project = ((CodeEditor2.NavigatePanel.FileNode)node).FileItem?.Project;
            }
            if (project == null) project = node.GetProject();
            return project;
        }
    }
}
