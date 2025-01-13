using Avalonia.Media;
using HarfBuzzSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data = CodeEditor2.Data;
using CodeEditor2.Data;


namespace CodeEditor2.NavigatePanel
{
    public class ProjectNode : FolderNode
    {
        public ProjectNode(Project project) : base(project)
        {
            if (ProjectNodeCreated != null) ProjectNodeCreated(this);
        }
        public static Action<ProjectNode>? ProjectNodeCreated;

        public Project? Project
        {
            get
            {
                return Item as Project;
            }
        }

        public override string Text
        {
            get {
                Project? project = Project;
                if(project==null) return "null";
                return project.Name;
            }
        }

        public override IImage? Image
        {
            get
            {
                return AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2/Assets/Icons/home.svg",
                    Avalonia.Media.Color.FromArgb(100,255,100,100)
                    );
            }
        }


    }
}
