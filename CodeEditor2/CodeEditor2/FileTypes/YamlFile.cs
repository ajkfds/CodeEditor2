using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.FileTypes
{
    public class YamlFile : CodeEditor2.FileTypes.FileType
    {
        public override string ID { get => "YamlFile"; }

        public override bool IsThisFileType(string relativeFilePath, CodeEditor2.Data.Project project)
        {
            if (
                relativeFilePath.EndsWith(".yaml")
            )
            {
                return true;
            }
            return false;
        }

        public override async Task<CodeEditor2.Data.File> CreateFile(string relativeFilePath, CodeEditor2.Data.Project project)
        {
            return await Data.YamlFile.CreateAsync(relativeFilePath, project);
        }
    }
}
