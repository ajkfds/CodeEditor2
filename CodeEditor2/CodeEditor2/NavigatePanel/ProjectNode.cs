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
using Avalonia.Threading;


namespace CodeEditor2.NavigatePanel
{
    public class ProjectNode : FolderNode
    {
        public ProjectNode(Project project) : base(project)
        {
            UpdateVisual();
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
        public override void UpdateVisual()
        {
            if (Dispatcher.UIThread.CheckAccess())
            {
                _updateVisual();
            }
            else
            {
                Dispatcher.UIThread.Post(() =>
                {
                    _updateVisual();
                });
            }
        }
        private void _updateVisual()
        {
            string text = "null";
            Project? project = Project;
            if (project != null) text = project.Name;
            Text = text;

            Image = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2/Assets/Icons/home.svg",
                    Avalonia.Media.Color.FromArgb(100, 255, 100, 100)
                    );
        }


    }
}
