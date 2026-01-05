using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using CodeEditor2.Data;

namespace CodeEditor2.FileTypes
{
    public class FileType
    {
        public virtual string ID { get; protected set; }
        public virtual bool IsThisFileType(string relativeFilePath, Project project)
        {
            return false;
        }

        public bool Visible { get; set; } = true;
        public virtual File CreateFile(string relativeFilePath, Project project)
        {
            System.Diagnostics.Debugger.Break();
            return null;
        }

        public virtual void CreateNewFile(string relativeFilePath,Project project)
        {

        }

        public virtual IImage GetIconImage()
        {
            return AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                "CodeEditor2/Assets/Icons/questionDocument.svg",
                Avalonia.Media.Color.FromArgb(100, 200, 200, 200)
                );
        }

    }
}
