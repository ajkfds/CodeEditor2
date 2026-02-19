using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.FileTypes
{
    public class TestResultFile : FileType
    {
        public override string ID { get { return "TestResultFile"; } }

        public override bool IsThisFileType(string relativeFilePath, Data.Project project)
        {
            if (
                relativeFilePath.ToLower().EndsWith(".testresult")
            )
            {
                return true;
            }
            return false;
        }

        public override async Task<Data.File?> CreateFile(string relativeFilePath, Data.Project project)
        {
            return await Data.TestResultFile.CreateAsync(relativeFilePath, project);
        }

        public override IImage GetIconImage()
        {
            return AjkAvaloniaLibs.Libs.Icons.GetSvgBitmap(
                "CodeEditor2/Assets/Icons/text.svg",
                Avalonia.Media.Color.FromArgb(100, 200, 200, 200)
                );
        }

    }
}
