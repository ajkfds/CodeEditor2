using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CodeEditor2.LLM.Tools
{
    public class ExecuteCommand : LLMTool
    {
        public ExecuteCommand(Data.Project project) : base(project) { }
        public override AIFunction GetAIFunction() { return AIFunctionFactory.Create(Run, "execute_command"); }

        public override string XmlExample { get; } = """
            ```xml
            <execute_command>
            <command>Your command here</command>
            </execute_command>         
            ```
            """;

        private static readonly HashSet<string> AllowedCommands = new() { "dotnet", "git", "ls", "dir" };


        [Description("""
            Request to execute a CLI command on the system. 
            Use this when you need to perform system operations or run specific commands to accomplish any step in the user's task. 
            You must tailor your command to the user's system and provide a clear explanation of what the command does. 
            For command chaining, use the appropriate chaining syntax for the user's shell. 
            Prefer to execute complex CLI commands over creating executable scripts, as they are more flexible and easier to run. 
            Commands will be executed in the current project root directory.
            
            `,
            """)]
        public async Task<string> Run(
            [Description("""
            The CLI command to execute.
            This should be valid for the current operating system.
            Ensure the command is properly formatted and does not contain any harmful instructions.
            一度に単一のコマンドの実行しか許可されません。&&やパイプ等を使って複数のコマンドを同時に実行しないでください。
            
            """)]
            string command
            )
        {
            try
            {
                if (project == null) return "Failed to execute tool. Cannot get current project.";

                bool isSafe;
                string reason;

                (isSafe,reason) = ValidateCommand(command);
                if (!isSafe)
                {
                    return reason;
                }

                string mainCommand = command.Split(' ')[0].ToLower();
                if (!AllowedCommands.Contains(mainCommand))
                {
                    return $"Error: The command '{mainCommand}' is not in the allowlist.";
                }


                mainCommand = command.Split(' ')[0];
                string argument = command.Substring(mainCommand.Length).TrimStart();


                CodeEditor2.Tools.YesNoWindow yesNoWindow = new CodeEditor2.Tools.YesNoWindow("execure_command request",$"Do you want to execute the following command? :{command}");
                await yesNoWindow.ShowDialog(CodeEditor2.Controller.GetMainWindow());
                if(!yesNoWindow.Yes) return "command_execute rejected by user";

                CommandParser parser = new CommandParser();

                string result = await parser.RunCommandAsync(command, project.RootPath, AllowedCommands);

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



        public static (bool IsSafe, string Reason) ValidateCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return (false, "Command is empty.");

            // 1. 絶対に禁止するキーワード (システム破壊系)
            var blackList = new[]
            {
                "rm -rf", "rd /s", "format", "mkfs",
                "chmod -R 777", "chown",
                "del /f /s /q", "shutdown", "reboot"
            };

            foreach (var forbidden in blackList)
            {
                if (command.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                    return (false, $"Forbidden keyword detected: {forbidden}");
            }

            // 2. 特権昇格の禁止
            var sudoList = new[] { "sudo", "runas", "su " };
            if (sudoList.Any(s => command.StartsWith(s, StringComparison.OrdinalIgnoreCase)))
                return (false, "Elevation of privilege (sudo/runas) is not allowed.");

            // 3. 環境変数の読み取り/書き込み (OSによって記法が異なるため注意)
            if (command.Contains("printenv") || command.Contains("set ") || command.Contains("export "))
                return (false, "Direct environment variable manipulation is restricted.");

            // 4. パイプやリダイレクトによる外部ファイルへの書き込み/読み込みの制限
            // プロジェクト外の重要ファイル (passwd, shadows, win.ini等) へのアクセスを簡易チェック
            var sensitivePaths = new[] { "/etc/", "/dev/", "C:\\Windows\\", "%WINDIR%" };
            foreach (var path in sensitivePaths)
            {
                if (command.Contains(path, StringComparison.OrdinalIgnoreCase))
                    return (false, $"Access to system sensitive path detected: {path}");
            }

            // 5. ネットワーク経由のダウンロード/実行 (外部スクリプトの実行防止)
            var downloadTools = new[] { "curl", "wget", "powershell iwr", "bitsadmin" };
            if (downloadTools.Any(tool => command.Contains(tool, StringComparison.OrdinalIgnoreCase)))
            {
                // 許可するドメインがある場合はここでホワイトリスト判定を行う
                return (false, "Network download commands are restricted.");
            }

            return (true, "Success");
        }

    }

}
