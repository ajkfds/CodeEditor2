﻿using Avalonia.Media;
using HarfBuzzSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data = CodeEditor2.Data;
using Avalonia.Media;
using CodeEditor2.Data;


namespace CodeEditor2.NavigatePanel
{
    public class ProjectNode : FolderNode
    {
        public ProjectNode(Project project) : base(project)
        {
            if (ProjectNodeCreated != null) ProjectNodeCreated(this);
        }
        public static Action<ProjectNode> ProjectNodeCreated;

        public Project Project
        {
            get
            {
                return Item as Project;
            }
        }

        public override string Text
        {
            get { return Project.Name; }
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

        //private static ajkControls.Primitive.IconImage openFolder = new ajkControls.Primitive.IconImage(Properties.Resources.openFolder);
        //private static ajkControls.Primitive.IconImage folder = new ajkControls.Primitive.IconImage(Properties.Resources.folder);

        //public override void Selected()
        //{
        //    Controller.NavigatePanel.GetContextMenuStrip().Items["gitLogTsmi"].Visible = true;
        //    base.Selected();
        //}

        //public override void DrawNode(Graphics graphics, int x, int y, Font font, Color color, Color backgroundColor, Color selectedColor, int lineHeight, bool selected)
        //{
        //    if (IsExpanded)
        //    {
        //        graphics.DrawImage(openFolder.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Red), new Point(x, y));
        //    }
        //    else
        //    {
        //        graphics.DrawImage(folder.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Red), new Point(x, y));
        //    }
        //    Color bgColor = backgroundColor;
        //    if (selected) bgColor = selectedColor;
        //    System.Windows.Forms.TextRenderer.DrawText(
        //        graphics,
        //        Text,
        //        font,
        //        new Point(x + lineHeight + (lineHeight >> 2), y),
        //        color,
        //        bgColor,
        //        System.Windows.Forms.TextFormatFlags.NoPadding
        //        );
        //}

        //public override void ShowProperyForm()
        //{
        //    using(Tools.ProjectPropertyForm pf = new Tools.ProjectPropertyForm(Project)){
        //        Controller.ShowDialogForm(pf);
        //    }
        //}

    }
}
