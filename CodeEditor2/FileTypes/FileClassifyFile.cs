using System.Threading.Tasks;

namespace CodeEditor2.FileTypes
{
    /// <summary>
    /// project rootに配置した".fileClassify"ファイルはファイル種別判定をオーバーライドするために使用できる。
    /// </summary>
    public class FileClassifyFile : CodeEditor2.FileTypes.FileType
    {
        public override string ID { get => "FileClassifyFile"; }

        public override bool IsThisFileType(string relativeFilePath, CodeEditor2.Data.Project project)
        {
            if (
                relativeFilePath == ".fileClassify"
            )
            {
                return true;
            }
            return false;
        }

        public override async Task<CodeEditor2.Data.File> CreateFile(string relativeFilePath, CodeEditor2.Data.Project project)
        {
            return await Data.FileClassifyFile.CreateAsync(relativeFilePath, project);
        }
    }
}
