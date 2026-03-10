using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.LLM.Tools
{
    public class BuildDotNet : LLMTool
    {
        public BuildDotNet(Data.Project project) : base(project) { }
        public override AIFunction GetAIFunction() { return AIFunctionFactory.Create(Run, "build_dotnet"); }

        public override string XmlExample { get; } = """
            ```xml
            <build_dotnet>
            </build_dotnet>         
            ```
            """;

        [Description("""
            Request to get a definition of the library type name. 
            """)]
        public async Task<string> Run()
        {
            try
            {
                if (project == null) return "Failed to execute tool. Cannot get current project.";

                string result = await RunDotnetBuildAsync(project.RootPath);

                return result;
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

        static async Task<string> RunDotnetBuildAsync(string projectPath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build \""+projectPath+"\"",
                RedirectStandardOutput = true,   // 讓呎ｺ門・蜉帙ｒ繝ｪ繝繧､繝ｬ繧ｯ繝・
                RedirectStandardError = true,    // 讓呎ｺ悶お繝ｩ繝ｼ繧ゅΜ繝繧､繝ｬ繧ｯ繝・
                UseShellExecute = false,         // 繧ｷ繧ｧ繝ｫ繧剃ｽｿ逕ｨ縺励↑縺・
                CreateNoWindow = true,           // 繧ｦ繧｣繝ｳ繝峨え繧定｡ｨ遉ｺ縺励↑縺・
                WorkingDirectory = Environment.CurrentDirectory
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();

                // 髱槫酔譛溘〒譛蠕後∪縺ｧ隱ｭ縺ｿ蜿悶ｋ
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    return $"繝薙Ν繝牙､ｱ謨・(ExitCode: {process.ExitCode})\nError: {error}\nOutput: {output}";
                }

                return output;
            }
        }

    }

}
