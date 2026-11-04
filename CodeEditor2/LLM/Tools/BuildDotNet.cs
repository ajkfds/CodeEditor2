using Microsoft.Extensions.AI;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
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
            Note: there is a size limit on the returned build log. If the log exceeds the limit,
            a warning is prepended and only the beginning (first part) of the log is returned;
            the tail is truncated. In that case, build only the affected project with -clp:ErrorsOnly
            (or run `dotnet build <project> -clp:ErrorsOnly`) to narrow the output.
            """)]
        public async Task<string> Run(CancellationToken cancellationToken)
        {
            try
            {
                if (project == null) return "Failed to execute tool. Cannot get current project.";

                string result = await RunDotnetBuildAsync(project.RootPath, cancellationToken);

                // 出力サイズ上限 (LLMTool.MaxOutputBytes) を適用。超過時はアラート + 前側のみ返す
                return LimitOutput(result, "build_dotnet");
            }
            catch (OperationCanceledException)
            {
                // Abort はキャンセルとして tool loop に伝播させる (ExecuteCommand と同一方針)
                throw;
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

        static async Task<string> RunDotnetBuildAsync(string projectPath, CancellationToken cancellationToken)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "build \"" + projectPath + "\"",
                RedirectStandardOutput = true,   // 標準出力をリダイレクト
                RedirectStandardError = true,    // 標準エラーもリダイレクト
                UseShellExecute = false,         // シェルを使用しない
                CreateNoWindow = true,           // ウィンドウを表示しない
                WorkingDirectory = Environment.CurrentDirectory
            };

            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();

                // stdout / stderr は同時に読み取る。片方を ReadToEnd で待ってから
                // もう片方を読むと、バッファが埋まった時点でデッドロックし得る。
                // また ReadToEndAsync 自体はキャンセル非対応のため、待機のみを
                // WaitAsync(token) でキャンセル可能にする。キャンセル時は finally で
                // プロセスツリーを Kill するので読み取りタスクも終了する。
                // (以前は CancellationToken を受け取らず無条件に待機していたため、
                //  Abort してもビルド終了まで tool call が返らなかった)
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();

                try
                {
                    await process.WaitForExitAsync(cancellationToken);

                    string output = await outputTask.WaitAsync(cancellationToken);
                    string error = await errorTask.WaitAsync(cancellationToken);

                    if (process.ExitCode != 0)
                    {
                        return $"ビルド失敗 (ExitCode: {process.ExitCode})\nError: {error}\nOutput: {output}";
                    }

                    return output;
                }
                finally
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(entireProcessTree: true);
                        }
                    }
                    catch
                    {
                        // 既に終了している場合は無視
                    }

                    // 未観測例外 (UnobservedTaskException) を残さないよう完了を待つ
                    try { await outputTask; } catch { /* Kill による中断は無視 */ }
                    try { await errorTask; } catch { /* Kill による中断は無視 */ }
                }
            }
        }

    }

}
