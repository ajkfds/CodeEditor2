using CodeEditor2.Data;
using System.Threading.Tasks;

namespace CodeEditor2.FileTypes
{
    /// <summary>
    /// .parseRule ファイルのFileType
    /// </summary>
    public class ParseRuleFile : CodeEditor2.FileTypes.FileType
    {
        public override string ID { get => "ParseRuleFile"; }

        public override bool IsThisFileType(string relativeFilePath, Project project)
        {
            if (
                relativeFilePath == ".parseRule" ||
                relativeFilePath == "parseRuleDefinitions.yaml"
            )
            {
                return true;
            }
            return false;
        }

        public override async Task<File> CreateFile(string relativeFilePath, Project project)
        {
            if (relativeFilePath == ".parseRule")
            {
                return await Data.ParseRuleFile.CreateAsync(relativeFilePath, project);
            }
            else if (relativeFilePath == "parseRuleDefinitions.yaml")
            {
                return await Data.YamlFile.CreateAsync(relativeFilePath, project);
            }
            // Default case - should not reach here since IsThisFileType returns false for other paths
            throw new System.NotSupportedException($"Cannot create file for: {relativeFilePath}");
        }
    }
}
