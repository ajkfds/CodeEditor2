﻿using Avalonia.Media;
using ExCSS;
using HarfBuzzSharp;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.NavigatePanel
{
    public class TextFileNode : FileNode
    {
        public override IImage? Image
        {
            get
            {
                return AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2/Assets/Icons/text.svg",
                    Avalonia.Media.Color.FromArgb(100, 200, 200, 200)
                    );
            }
        }

        public static Action<TextFileNode> TextFileNodeCreated;
        public TextFileNode(Data.TextFile textFile) : base(textFile as Data.File)
        {
            if (TextFileNodeCreated != null) TextFileNodeCreated(this);
        }

        public Data.TextFile TextFile
        {
            get { return Item as Data.TextFile; }
        }

        public override string Text
        {
            get { return FileItem.Name; }
        }

//        private static ajkControls.Primitive.IconImage icon = new ajkControls.Primitive.IconImage(Properties.Resources.text);
        //public override void DrawNode(Graphics graphics, int x, int y, Font font, Color color, Color backgroundColor, Color selectedColor, int lineHeight, bool selected)
        //{
        //    graphics.DrawImage(icon.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.White), new Point(x, y));
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
        //    if (TextFile != null && TextFile.Dirty)
        //    {
        //        graphics.DrawImage(Global.IconImages.NewBadge.GetImage(lineHeight, ajkControls.Primitive.IconImage.ColorStyle.Orange), new Point(x, y));
        //    }
        //}

        public override void OnSelected()
        {
            Global.mainView.CodeView.SetTextFile(TextFile);
//            Controller.CodeEditor.SetTextFile(TextFile);
        }

    }
}