using Avalonia.Input;
using CodeEditor2.CodeEditor.PopupMenu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CodeEditor2.Controller;

namespace CodeEditor2.Snippets
{
    public class InteractiveSnippet : ToolItem
    {
        public InteractiveSnippet(string text) : base(text)
        {

        }

        public override void Apply()
        {
            Data.TextFile? file = CodeEditor2.Controller.CodeEditor.GetTextFile();
            if (file == null) return;

            document = file.CodeDocument;
            Controller.CodeEditor.StartInteractiveSnippet(this);
            Controller.CodeEditor.Refresh();
        }
        CodeEditor.CodeDocument document;

        public virtual void Aborted()
        {
            document = null;
        }
        public virtual void Cancel()
        {
        }
        public virtual void KeyDown(object? sender, KeyEventArgs e, Views.PopupMenuView popupMenuView)
        {

        }
        public virtual void BeforeKeyDown(object? sender, TextInputEventArgs e, Views.PopupMenuView popupMenuView)
        {

        }
        public virtual void AfterAutoCompleteHandled(Views.PopupMenuView popupMenuView)
        {

        }
        public virtual void AfterKeyDown(object? sender, TextInputEventArgs e, Views.PopupMenuView popupMenuView)
        {

        }


    }
}
