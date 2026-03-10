using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.CodeEditor.PopupMenu
{
    public class ToolItem
    {
        public ToolItem(string text)
        {
            this.text = text;
        }
        string text;

        public async void Apply()
        {
            try
            {
                await ApplyAsync();
            }catch(Exception ex)
            {
                CodeEditor2.Controller.AppendLog(ex.Message, Avalonia.Media.Colors.Red);
            }
        }
        public virtual Task ApplyAsync()
        {
            return Task.CompletedTask;
        }

        public string Text
        {
            get
            {
                return text;
            }
            set
            {
                text= value;
            }
        }

        public Avalonia.Media.IImage? IconImage { get; set; }

        public virtual PopupMenuItem CreatePopupMenuItem()
        {
            PopupMenuItem popupMenuItem = new PopupMenuItem(text);
            popupMenuItem.Selected += new Action(() => { Apply(); });
            popupMenuItem.Image = IconImage;
            return popupMenuItem;
        }

    }
}
