using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.FileTypes
{
    public class FileClassifyFile : CodeEditor2.FileTypes.FileType
    {
        public override string ID { get => "FileClassifyFile"; }

        public override bool IsThisFileType(string relativeFilePath, CodeEditor2.Data.Project project)
        {
            if (
                relativeFilePath==".fileClassify"
            )
            {
                return true;
            }
            return false;
        }

        public override CodeEditor2.Data.File CreateFile(string relativeFilePath, CodeEditor2.Data.Project project)
        {
            return Data.FileClassifyFile.Create(relativeFilePath, project);
        }
    }
}
