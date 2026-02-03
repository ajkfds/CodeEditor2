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
        public LLMTool(Data.Project project)
        {
            this.project = project;
        }
        protected Data.Project project;
        public abstract AIFunction GetAIFunction();

        public virtual string XmlExample { get; } = "";
    }
}
