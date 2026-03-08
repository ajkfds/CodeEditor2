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
                RedirectStandardOutput = true,   // 標準出力をリダイレクト
                RedirectStandardError = true,    // 標準エラーもリダイレクト
                UseShellExecute = false,         // シェルを使用しない
                CreateNoWindow = true,           // ウィンドウを表示しない
                WorkingDirectory = Environment.CurrentDirectory
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();

                // 非同期で最後まで読み取る
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    return $"ビルド失敗 (ExitCode: {process.ExitCode})\nError: {error}\nOutput: {output}";
                }

                return output;
            }
        }

    }

}
