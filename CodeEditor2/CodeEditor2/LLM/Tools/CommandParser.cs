using CodeEditor2.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeEditor2.LLM.Tools
{
    public class CommandParser
    {
        public enum SeparatorType { None, Sync, And, Or } // ; , &&, ||

        public class CommandChain
        {
            public List<string> PipeCommands { get; set; } = new(); // ["ls -la", "grep .txt"]
            public SeparatorType NextSeparator { get; set; } = SeparatorType.None;
        }
        public async Task<string> RunCommandAsync(string fullCommand,string rootPath,HashSet<string> allowedCommands)
        {
            // 1. コマンドをパースして構造化する
            var chains = ParseFullCommandLine(fullCommand);
            var finalOutput = new StringBuilder();
            int lastExitCode = 0;

            foreach (var chain in chains)
            {
                // 前のコマンドの結果に基づく実行判定
                if (chain.NextSeparator == SeparatorType.And && lastExitCode != 0) break;
                // (必要に応じて SeparatorType.Or の実装も可能)

                // パイプラインを実行
                var (output, exitCode) = await ExecutePipeChainAsync(chain.PipeCommands,rootPath,allowedCommands);
                finalOutput.Append(output);
                lastExitCode = exitCode;

                if (chain.NextSeparator == SeparatorType.None) break;
            }

            return finalOutput.ToString();
        }

        private List<CommandChain> ParseFullCommandLine(string input)
        {
            var chains = new List<CommandChain>();
            // && や ; で分割する正規表現 (クォート内は無視)
            // 簡易版として、まずは単純な分割で実装例を示します
            var patterns = Regex.Split(input, @"(&&|;)");

            var currentChain = new CommandChain();
            foreach (var part in patterns)
            {
                var trimmed = part.Trim();
                if (trimmed == "&&")
                {
                    currentChain.NextSeparator = SeparatorType.And;
                    chains.Add(currentChain);
                    currentChain = new CommandChain();
                }
                else if (trimmed == ";")
                {
                    currentChain.NextSeparator = SeparatorType.Sync;
                    chains.Add(currentChain);
                    currentChain = new CommandChain();
                }
                else
                {
                    // パイプで分割して格納
                    currentChain.PipeCommands = trimmed.Split('|').Select(p => p.Trim()).ToList();
                }
            }
            chains.Add(currentChain);
            return chains;
        }

        private async Task<(string Output, int ExitCode)> ExecutePipeChainAsync(List<string> pipeParts,string rootPath, HashSet<string> allowedCommands)
        {
            var processes = new List<Process>();
            var outputBuilder = new StringBuilder();
            int lastExitCode = 0;

            try
            {
                Stream lastOutputStream = null;

                for (int i = 0; i < pipeParts.Count; i++)
                {
                    var tokens = ParseArguments(pipeParts[i]);
                    if (tokens.Count == 0) continue;

                    var cmd = tokens[0];
                    var args = string.Join(" ", tokens.Skip(1));

                    // セキュリティ：許可リスト照合
                    if (!allowedCommands.Contains(cmd.ToLower()))
                        return ($"Error: '{cmd}' is not allowed.", -1);

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = cmd,
                        Arguments = args,
                        WorkingDirectory = rootPath,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    var proc = Process.Start(startInfo);
                    processes.Add(proc);

                    // パイプの接続 (前のプロセスの出力を今のプロセスの入力へ)
                    if (lastOutputStream != null)
                    {
                        _ = CopyStreamAsync(lastOutputStream, proc.StandardInput.BaseStream);
                    }

                    lastOutputStream = proc.StandardOutput.BaseStream;
                }

                // 最後のプロセスの完了を待ち、出力をキャプチャ
                var lastProc = processes.Last();
                var outTask = lastProc.StandardOutput.ReadToEndAsync();
                var errTask = lastProc.StandardError.ReadToEndAsync();

                await Task.WhenAll(outTask, errTask);
                await lastProc.WaitForExitAsync();

                lastExitCode = lastProc.ExitCode;
                return (outTask.Result + errTask.Result, lastExitCode);
            }
            catch (Exception ex)
            {
                return ($"Execution Error: {ex.Message}\n", -1);
            }
            finally
            {
                foreach (var p in processes) p.Dispose();
            }
        }
        /// <summary>
        /// コマンドライン文字列をトークン（プログラム名と引数）に分解します。
        /// ダブルクォーテーション内のスペースを保持し、エスケープ文字にも対応します。
        /// </summary>
        private List<string> ParseArguments(string commandLine)
        {
            if (string.IsNullOrWhiteSpace(commandLine)) return new List<string>();

            var results = new List<string>();
            var current = new StringBuilder();
            bool inQuotes = false;
            bool isEscaped = false;

            for (int i = 0; i < commandLine.Length; i++)
            {
                char c = commandLine[i];

                // エスケープ文字 (\) の処理
                if (c == '\\' && !isEscaped)
                {
                    isEscaped = true;
                    continue;
                }

                if (isEscaped)
                {
                    current.Append(c);
                    isEscaped = false;
                    continue;
                }

                // クォーテーション (") の処理
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                    // クォーテーション自体はトークンに含めない場合は continue
                    // もし引数として含める必要がある場合は current.Append(c)
                    current.Append(c);
                    continue;
                }

                // スペースの処理（クォーテーション外のみ区切りとして扱う）
                if (char.IsWhiteSpace(c) && !inQuotes)
                {
                    if (current.Length > 0)
                    {
                        results.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            // 残った文字列を追加
            if (current.Length > 0)
            {
                results.Add(current.ToString());
            }

            return results;
        }
        private async Task CopyStreamAsync(Stream input, Stream output)
        {
            try
            {
                using (output) await input.CopyToAsync(output);
            }
            catch { /* パイプが閉じた場合の処理 */ }
        }
    }
}
