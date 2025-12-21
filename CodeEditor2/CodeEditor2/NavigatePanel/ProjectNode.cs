using Avalonia.Media;
using Avalonia.Threading;
using CodeEditor2.Data;
using CodeEditor2.Tools;
using HarfBuzzSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data = CodeEditor2.Data;


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
        public override async void OnSelected()
        {
            try
            {
                base.OnSelected();
                await UpdateAsync();
            }
            catch
            {
                if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
            }
        }
        public override void UpdateVisual()
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    _updateVisual();
                }
                catch (Exception ex)
                {
                    CodeEditor2.Controller.AppendLog("#Exception " + ex.Message, Colors.Red);
                    throw;
                }
            });
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
