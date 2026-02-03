using Avalonia.Controls.Documents;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.LLM.Tools
{
    public class WriteToFile:LLMTool
    {
        /*
        ## write_to_file
        Description: Request to write content to a file at the specified path. If the file exists, it will be overwritten with the provided content. If the file doesn't exist, it will be created. This tool will automatically create any directories needed to write the file.
        Parameters:
        - path: (required) The path of the file to write to (relative to the current working directory ${cwd.toPosix()})
        - content: (required) The content to write to the file. ALWAYS provide the COMPLETE intended content of the file, without any truncation or omissions. You MUST include ALL parts of the file, even if they haven't been modified.
        Usage:
        <write_to_file>
        <path>File path here</path>
        <content>
        Your file content here
        </content>
        ${
	        focusChainSettings.enabled
		        ? `<task_progress>
        Checklist here (optional)
        </task_progress>`
		        : ""
        }
        </write_to_file>
         */
        public override string XmlExample { get; } = """
            ```xml
            <write_to_file>
            <path>File path here</path>
            <content>
            Your file content here
            </content>
            </write_to_file>
            ```
            """;

        public override AIFunction GetAIFunction() { return AIFunctionFactory.Create(Run, "write_to_file"); }

        [Description("""
            Request to write content to a file at the specified path. If the file exists, it will be overwritten with the provided content. 
            If the file doesn't exist, it will be created. This tool will automatically create any directories needed to write the file.
            出力コンテキストサイズが小さいため、一度に出力するファイルサイズは100行以内とする。それを超える場合はいったん一部を出力した後、replace_in_fileで複数回に分けて更新すること。
            """)]
            
        public string Run(
        [Description("The path of the file to write to (relative to the project root directory)")]
        string path,
        [Description("""
            The content to write to the file. 
            ALWAYS provide the COMPLETE intended content of the file, without any truncation or omissions. 
            You MUST include ALL parts of the file, even if they haven't been modified.
            """)]
        string content
        ){
            try
            {
                CodeEditor2.Data.Project? project = GetProject();
                if (project == null) return "Failed to execute tool. Cannot get current project.";

                // 1. パスの安全性を確認
                // 1. パスの正規化と安全性のチェック
                string fullPath = project.GetAbsolutePath(path);

                if (!fullPath.StartsWith(project.RootPath, StringComparison.OrdinalIgnoreCase))
                {
                    return "Error: Permission denied. Cannot read files outside of the project root.";
                }

                // 2. ディレクトリの存在確認と自動生成
                string? directoryPath = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // 3. ファイルの書き込み (UTF-8)
                // LLMからの出力は意図せず不完全な場合があるため、上書きは慎重に行われます
                File.WriteAllText(fullPath, content, Encoding.UTF8);

                return $"Success: Content successfully written to '{path}'.";
            }
            catch (UnauthorizedAccessException)
            {
                return "Error: Access to the path is denied or the file is read-only.";
            }
            catch (IOException ex)
            {
                return $"Error: An I/O error occurred while writing to the file: {ex.Message}";
            }
            catch (Exception ex)
            {
                return $"Error: An unexpected error occurred: {ex.Message}";
            }
        }
    }
}
