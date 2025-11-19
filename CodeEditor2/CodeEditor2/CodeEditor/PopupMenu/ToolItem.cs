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

        public virtual void Apply()
        {

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
