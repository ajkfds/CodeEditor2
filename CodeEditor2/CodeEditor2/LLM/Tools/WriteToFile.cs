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
        public WriteToFile(Data.Project project) : base(project) { }
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
            蜃ｺ蜉帙さ繝ｳ繝・く繧ｹ繝医し繧､繧ｺ縺悟ｰ上＆縺・◆繧√｜uilding block縺ｮ螳夂ｾｩ縺縺代ｒ蜃ｺ蜉帙＠縲√◎縺ｮ蠕後〉eplace_in_file縺ｧ譖ｴ譁ｰ縺吶ｋ縺薙→縲・
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
                if (project == null) return "Failed to execute tool. Cannot get current project.";

                // 1. 繝代せ縺ｮ螳牙・諤ｧ繧堤｢ｺ隱・
                // 1. 繝代せ縺ｮ豁｣隕丞喧縺ｨ螳牙・諤ｧ縺ｮ繝√ぉ繝・け
                string fullPath = project.GetAbsolutePath(path);

                if (!fullPath.StartsWith(project.RootPath, StringComparison.OrdinalIgnoreCase))
                {
                    return "Error: Permission denied. Cannot read files outside of the project root.";
                }

                // 2. 繝・ぅ繝ｬ繧ｯ繝医Μ縺ｮ蟄伜惠遒ｺ隱阪→閾ｪ蜍慕函謌・
                string? directoryPath = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }


                // 3. 繝輔ぃ繧､繝ｫ縺ｮ譖ｸ縺崎ｾｼ縺ｿ (UTF-8)
                // LLM縺九ｉ縺ｮ蜃ｺ蜉帙・諢丞峙縺帙★荳榊ｮ悟・縺ｪ蝣ｴ蜷医′縺ゅｋ縺溘ａ縲∽ｸ頑嶌縺阪・諷朱㍾縺ｫ陦後ｏ繧後∪縺・
                content = content.Replace("\r\n", "\n");
                if (!content.EndsWith("\n")) content = content + "\n";

                File.WriteAllText(fullPath, content);

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
