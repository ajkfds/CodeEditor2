using Microsoft.Extensions.AI;

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
