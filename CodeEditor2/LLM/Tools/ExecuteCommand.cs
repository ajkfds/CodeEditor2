using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        private static readonly HashSet<string> AllowedCommands = new() { "dotnet", "git", "dir" };

        // 出力サイズ上限の既定値 (256KB)
        public const int DefaultMaxOutputBytes = 256 * 1024;

        private int maxOutputBytes = DefaultMaxOutputBytes;

        /// <summary>
        /// 1 回の execute_command で返す出力のサイズ上限 (バイト、UTF-8 換算)。
        /// 上限を超える場合はアラート (警告メッセージ) を先頭に付けたうえで、
        /// 出力の「前側 (先頭から上限バイトまで)」のみを返す (後側は切り捨てる)。
        /// ReadFile の MaxFileBytes と同じ考え方だが、ログは先頭が重要なことが多いため
        /// 内容を捨てずに切り詰める点が異なる。
        /// 0 以下を設定した場合は既定値 (<see cref="DefaultMaxOutputBytes"/> = 256KB) に戻す。
        /// </summary>
        public int MaxOutputBytes
        {
            get => maxOutputBytes;
            set => maxOutputBytes = value > 0 ? value : DefaultMaxOutputBytes;
        }


        [Description("""
            Request to execute a CLI command on the system. 
            Use this when you need to perform system operations or run specific commands to accomplish any step in the user's task. 
            You must tailor your command to the user's system and provide a clear explanation of what the command does. 
            For command chaining, use the appropriate chaining syntax for the user's shell. 
            Prefer to execute complex CLI commands over creating executable scripts, as they are more flexible and easier to run. 
            Commands will be executed in the current project root directory.
            Note: there is a size limit on the returned output. If the output exceeds the limit,
            a warning is prepended and only the beginning (first part) of the output is returned;
            the tail is truncated. In that case, narrow the output with a more specific command
            (e.g. build only the affected project with -clp:ErrorsOnly, or use git log -n <count>).
            
            `,
            """)]
        public async Task<string> Run(
            [Description("""
            The CLI command to execute.
            This should be valid for the current operating system.
            Ensure the command is properly formatted and does not contain any harmful instructions.
            
            """)]
            //            一度に単一のコマンドの実行しか許可されません。&&やパイプ等を使って複数のコマンドを同時に実行しないでください。
            string command,
            CancellationToken cancellationToken
            )
        {
            try
            {
                if (project == null) return "Failed to execute tool. Cannot get current project.";

                bool isSafe;
                string reason;

                (isSafe, reason) = ValidateCommand(command);
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

                // user acceptance
                if (!Global.AutoAcceptLLMCommandExecution)
                {
                    CodeEditor2.Tools.YesNoWindow yesNoWindow = new CodeEditor2.Tools.YesNoWindow("execure_command request", $"Do you want to execute the following command? :{command}");
                    await CodeEditor2.Controller.ShowDialogAsync(yesNoWindow);
                    if (!yesNoWindow.Yes) return "command_execute rejected by user";
                }

                CommandParser parser = new CommandParser();

                string result = await parser.RunCommandAsync(command, project.RootPath, AllowedCommands,cancellationToken);

                return LimitOutput(result);
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



        /// <summary>
        /// 出力が <see cref="MaxOutputBytes"/> を超えている場合、アラート (警告) を先頭に付け、
        /// 出力の前側 (先頭から上限バイトまで) のみを返す。超えていなければそのまま返す。
        /// ReadFile は上限超過時に内容を一切返さないが、execute_command の出力 (ビルドログ等) は
        /// 先頭に重要な情報が集まるため、内容を捨てずに切り詰めて返す方針とする。
        /// </summary>
        private string LimitOutput(string output)
        {
            if (string.IsNullOrEmpty(output)) return output;

            long totalBytes = Encoding.UTF8.GetByteCount(output);
            if (totalBytes <= MaxOutputBytes) return output;

            int cutChars = GetCharCountForByteBudget(output, MaxOutputBytes);
            string head = output.Substring(0, cutChars);

            return BuildTooLargeAlert(totalBytes, cutChars, head);
        }

        /// <summary>
        /// 出力先頭から UTF-8 で最大 <paramref name="byteBudget"/> バイトに収まる文字数を返す。
        /// サロゲートペア (絵文字等) を分断しないよう、途中で切れた場合は 1 文字戻す。
        /// </summary>
        private static int GetCharCountForByteBudget(string text, int byteBudget)
        {
            int bytes = 0;
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                // サロゲートペアは 2 char で 1 コードポイント (UTF-8 で 4 バイト)
                bool isSurrogatePair = char.IsHighSurrogate(c)
                    && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1]);
                int charCount = isSurrogatePair ? 2 : 1;
                int byteCount = Encoding.UTF8.GetByteCount(text.AsSpan(i, charCount));

                if (bytes + byteCount > byteBudget) break;

                bytes += byteCount;
                i += charCount;
            }

            // 念のためサロゲート分断を回避 (i はコードポイント境界なので通常は不要)
            if (i > 0 && i < text.Length && char.IsHighSurrogate(text[i - 1])) i--;
            return i;
        }

        /// <summary>
        /// 上限超過時のアラート (警告) + 出力前側を組み立てる。
        /// </summary>
        private string BuildTooLargeAlert(long totalBytes, int returnedChars, string head)
        {
            string sizeText = FormatBytes(totalBytes);
            string limitText = FormatBytes(MaxOutputBytes);
            string returnedText = FormatBytes(Encoding.UTF8.GetByteCount(head));

            var sb = new StringBuilder();
            sb.Append("[WARNING] execute_command: the output is too large and was truncated. ")
              .Append($"total={sizeText}, returned={returnedText} (head only), limit={limitText} ({MaxOutputBytes} bytes), ")
              .Append($"returnedChars={returnedChars}. The tail of the output was dropped. ")
              .Append("Narrow the output by a more specific command (e.g. build only the affected project with -clp:ErrorsOnly, ")
              .Append("or use git log -n <count> / git diff --stat) if you need the dropped part.")
              .Append('\n')
              .Append("--- output (head) ---\n")
              .Append(head)
              .Append("\n--- end of truncated output ---\n");

            return sb.ToString();
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} bytes";
            if (bytes < 1024L * 1024L) return $"{bytes / 1024.0:0.#} KB";
            if (bytes < 1024L * 1024L * 1024L) return $"{bytes / (1024.0 * 1024.0):0.#} MB";
            return $"{bytes / (1024.0 * 1024.0 * 1024.0):0.#} GB";
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
