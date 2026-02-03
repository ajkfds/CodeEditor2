using Avalonia.Controls.Documents;
using CodeEditor2.Data;
using CodeEditor2Plugin;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.LLM.Tools
{
    public class ReadFile : LLMTool
    {
        public ReadFile(Data.Project project) : base(project) { }
        /*
        ## read_file
        Description: Request to read the contents of a file at the specified path. Use this when you need to examine the contents of an existing file you do not know the contents of, for example to analyze code, review text files, or extract information from configuration files. Automatically extracts raw text from PDF and DOCX files. May not be suitable for other types of binary files, as it returns the raw content as a string.
        Parameters:
        - path: (required) The path of the file to read (relative to the current working directory ${cwd.toPosix()})
        ${focusChainSettings.enabled ? `- task_progress: (optional) A checklist showing task progress after this tool use is completed. (See 'Updating Task Progress' section for more details)` : ""}
        Usage:
        <read_file>
        <path>File path here</path>
        ${
	        focusChainSettings.enabled
		        ? `<task_progress>
        Checklist here (optional)
        </task_progress>`
		        : ""
        }
        </read_file>         
         */
        //public static AIFunction GetAIFunction()
        //{
        //    return AIFunctionFactory.Create(Run, "read_file");
        //}
        public override AIFunction GetAIFunction() { return AIFunctionFactory.Create(Run, "read_file"); }

        public override string XmlExample { get; } = """
            ```xml
            <read_file>
            <path>File path here</path>
            </read_file>         
            ```
            """;

        [Description("""
            Request to read the contents of a file at the specified path. 
            Use this when you need to examine the contents of an existing file you do not know the contents of, 
            for example to analyze code, review text files, or extract information from configuration files. 
            May not be suitable for other types of binary files, as it returns the raw content as a string.
            """)]
        public string Run(
            [Description("The path of the file to read (relative to the project root directory)")] string path)
        {
            try
            {
                if (project == null) return "Failed to execute tool. Cannot get current project.";

                // 1. パスの正規化と安全性のチェック
                string fullPath = project.GetAbsolutePath(path);
                
                if (!fullPath.StartsWith(project.RootPath, StringComparison.OrdinalIgnoreCase))
                {
                    return "Error: Permission denied. Cannot read files outside of the project root.";
                }

                // 2. ファイルの存在確認
                if (!System.IO.File.Exists(fullPath))
                {
                    return $"Error: File not found at path '{path}'.";
                }

                // 3. ファイルサイズのチェック (例: 1MBを超える場合は制限)
                var fileInfo = new FileInfo(fullPath);
                if (fileInfo.Length > 1024 * 1024)
                {
                    return "Error: File is too large to read (limit: 1MB). Please request a specific range if supported.";
                }

                // 4. ファイルの読み込み
                // UTF-8で読み込み。BOMの有無も自動判別します。
                return System.IO.File.ReadAllText(fullPath, Encoding.UTF8);
            }
            catch (UnauthorizedAccessException)
            {
                return "Error: Access to the path is denied.";
            }
            catch (IOException ex)
            {
                return $"Error: An I/O error occurred while reading the file: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error: An unexpected error occurred: {ex.Message}";
            }
        }
    }



}


