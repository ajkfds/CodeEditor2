﻿using Avalonia.Media;
using Avalonia.Threading;
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
        public static Action<TextFileNode>? TextFileNodeCreated;
        public TextFileNode(Data.TextFile textFile) : base(textFile as Data.File)
        {
            if (TextFileNodeCreated != null) TextFileNodeCreated(this);
            UpdateVisual();
        }

        public Data.TextFile? TextFile
        {
            get { return Item as Data.TextFile; }
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

        public void _updateVisual()
        {
            string text = "null";
            Data.TextFile? textFile = TextFile;
            if (textFile != null) text = textFile.Name;
            Text = text;

            if (textFile==null)
            {
                Image = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2/Assets/Icons/questionDocument.svg",
                    Avalonia.Media.Color.FromArgb(100, 200, 200, 200),
                    "CodeEditor2/Assets/Icons/questionDocument.svg",
                    Avalonia.Media.Color.FromArgb(255, 255, 255, 200)
                    );
                Nodes.Clear();
                return;
            }

            if (textFile.CodeDocument.IsDirty)
            {
                Image = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2/Assets/Icons/text.svg",
                    Avalonia.Media.Color.FromArgb(100, 200, 200, 200),
                    "CodeEditor2/Assets/Icons/shine.svg",
                    Avalonia.Media.Color.FromArgb(255, 255, 255, 200)
                    );
            }
            else
            {
                Image = AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                    "CodeEditor2/Assets/Icons/text.svg",
                    Avalonia.Media.Color.FromArgb(100, 200, 200, 200)
                    );
            }
            Controller.NavigatePanel.UpdateVisual();
        }

        public override void OnSelected()
        {
            base.OnSelected();
            if(TextFile != null) Controller.CodeEditor.SetTextFile(TextFile);
        }

    }
}