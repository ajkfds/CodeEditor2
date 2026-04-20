using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CodeEditor2.Data;
using CodeEditor2.Tools;
using HarfBuzzSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Data = CodeEditor2.Data;


namespace CodeEditor2.NavigatePanel
{
    public class ProjectNode : FolderNode
    {
        static ProjectNode()
        {
            CustomizeSpecificNodeContextMenu += ((m) => {
                ContextMenu menu = m;
                MenuItem menuItem_Agent = CodeEditor2.Global.CreateMenuItem(
                    "Save Cashe", "menuItem_SaveCashe",
                    "CodeEditor2/Assets/Icons/tag.svg",
                    Avalonia.Media.Colors.Blue
                    );
                menu.Items.Add(menuItem_Agent);
                menuItem_Agent.Click += MenuItem_Save_Cashe;
            });
        }
        public ProjectNode(Project project) : base(project)
        {
            UpdateVisual();
            if (ProjectNodeCreated != null) ProjectNodeCreated(this);

        }
        private static void MenuItem_Save_Cashe(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            NavigatePanel.NavigatePanelNode? navigatePanelNode = Controller.NavigatePanel.GetSelectedNode();
            if (navigatePanelNode == null) return;
            Project project = navigatePanelNode.GetProject();
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
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Invoke(() => { UpdateVisual(); });
                return;
            }

            string text = "null";
            Project? project = Project;
            if (project != null) text = project.Name;
            Text = text;

            Image = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2/Assets/Icons/home.svg",
                    Global.Color_Project
                    );
        }

        // Project Node ContextMenu Customization
        public static new Action<ContextMenu>? CustomizeSpecificNodeContextMenu;
        protected override Action<ContextMenu>? customizeSpecificNodeContextMenu => CustomizeSpecificNodeContextMenu;
        /*
        ## Example

        add new menu to Project Node

        CodeEditor2.NavigatePanel.ProjectNode.CustomizeSpecificNodeContextMenu += ((m) => {
            ContextMenu menu = m;
            MenuItem menuItem_Agent = CodeEditor2.Global.CreateMenuItem(
                "LLM Agent", "menuItem_Agent",
                "CodeEditor2/Assets/Icons/ai.svg",
                Avalonia.Media.Colors.YellowGreen
                );
            menu.Items.Add(menuItem_Agent);
            menuItem_Agent.Click += MenuItem_Agent_Click;
        });
        */

    }
}
