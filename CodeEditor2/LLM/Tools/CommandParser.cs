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
        public async Task<string> RunCommandAsync(string fullCommand, string rootPath, HashSet<string> allowedCommands)
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
                var (output, exitCode) = await ExecutePipeChainAsync(chain.PipeCommands, rootPath, allowedCommands);
                finalOutput.Append(output);
                lastExitCode = exitCode;

                if (chain.NextSeparator == SeparatorType.None) break;
            }

            finalOutput.AppendLine();
            finalOutput.AppendLine("command complete, ExitCode:" + lastExitCode.ToString());

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


        private async Task<(string Output, int ExitCode)> ExecutePipeChainAsync(List<string> pipeParts, string rootPath, HashSet<string> allowedCommands)
        {
            // 事前チェック：起動前にすべてのコマンドが許可されているか検証（途中終了によるリーク防止）
            var parsedCmds = new List<(string Cmd, string Args)>();
            foreach (var part in pipeParts)
            {
                var tokens = ParseArguments(part);
                if (tokens.Count == 0) continue;

                var cmd = tokens[0];
                if (!allowedCommands.Contains(cmd.ToLower()))
                    return ($"Error: '{cmd}' is not allowed.", -1);

                var args = string.Join(" ", tokens.Skip(1));
                parsedCmds.Add((cmd, args));
            }

            if (parsedCmds.Count == 0)
                return (string.Empty, 0);

            var processes = new List<Process>();
            var backgroundTasks = new List<Task>();
            // 順序を保つため、ConcurrentQueue を使用
            var errorLogs = new System.Collections.Concurrent.ConcurrentQueue<string>();

            try
            {
                Stream previousOutputStream = null;

                foreach (var (cmd, args) in parsedCmds)
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = cmd,
                        Arguments = args,
                        WorkingDirectory = rootPath,
                        UseShellExecute = false,
                        RedirectStandardInput = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        StandardOutputEncoding = Encoding.UTF8,
                        StandardErrorEncoding = Encoding.UTF8,
                        CreateNoWindow = true
                    };

                    var proc = Process.Start(startInfo);
                    processes.Add(proc);

                    if (previousOutputStream == null)
                    {
                        proc.StandardInput.Close();
                    }

                    // エラー出力収集
                    backgroundTasks.Add(Task.Run(async () =>
                    {
                        using var reader = proc.StandardError;
                        string line;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            errorLogs.Enqueue(line);
                        }
                    }));

                    // パイプ転送
                    if (previousOutputStream != null)
                    {
                        var inputWriter = proc.StandardInput.BaseStream;
                        var sourceStream = previousOutputStream;

                        backgroundTasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                await sourceStream.CopyToAsync(inputWriter);
                            }
                            catch
                            {
                                // パイプ断絶（前段・後段プロセスの異常終了）時は無視
                            }
                            finally
                            {
                                inputWriter.Close();
                            }
                        }));
                    }

                    previousOutputStream = proc.StandardOutput.BaseStream;
                }

                var lastProc = processes.Last();

                // 最後のプロセスの stdout 読み込みとプロセス終了を並行して実行
                string finalStdout = await lastProc.StandardOutput.ReadToEndAsync();

                foreach (var proc in processes)
                {
                    await proc.WaitForExitAsync();
                }

                await Task.WhenAll(backgroundTasks);

                var exitCode = lastProc.ExitCode;
                var combinedError = string.Join("\n", errorLogs);
                var resultText = string.IsNullOrEmpty(combinedError)
                    ? finalStdout
                    : $"{finalStdout}\n{combinedError}";

                return (resultText, exitCode);
            }
            finally
            {
                foreach (var proc in processes)
                {
                    try
                    {
                        if (!proc.HasExited)
                        {
                            proc.Kill(entireProcessTree: true);
                        }
                    }
                    catch
                    {
                        // 無視
                    }
                    finally
                    {
                        proc.Dispose();
                    }
                }
            }
        }

        //private async Task<(string Output, int ExitCode)> ExecutePipeChainAsync(List<string> pipeParts, string rootPath, HashSet<string> allowedCommands)
        //{








        //    var processes = new List<Process>();
        //    var outputBuilder = new StringBuilder();
        //    int lastExitCode = 0;








        //    try
        //    {
        //        Stream lastOutputStream = null;

        //        var errorBuffers = new System.Collections.Concurrent.ConcurrentBag<string>();
        //        var streamCopyTasks = new List<Task>();
        //        var readTasks = new List<Task>();

        //        for (int i = 0; i < pipeParts.Count; i++)
        //        {
        //            var tokens = ParseArguments(pipeParts[i]);
        //            if (tokens.Count == 0) continue;

        //            var cmd = tokens[0];
        //            var args = string.Join(" ", tokens.Skip(1));

        //            if (!allowedCommands.Contains(cmd.ToLower()))
        //                return ($"Error: '{cmd}' is not allowed.", -1);

        //            var startInfo = new ProcessStartInfo
        //            {
        //                FileName = cmd,
        //                Arguments = args,
        //                WorkingDirectory = rootPath,
        //                UseShellExecute = false,
        //                RedirectStandardInput = true,
        //                RedirectStandardOutput = true,
        //                RedirectStandardError = true,
        //                StandardOutputEncoding = Encoding.UTF8,
        //                StandardErrorEncoding = Encoding.UTF8,
        //                CreateNoWindow = true
        //            };

        //            var proc = Process.Start(startInfo);
        //            processes.Add(proc);

        //            // 1. 各プロセスの StandardError を吸い出す（バッファ詰まり防止）
        //            readTasks.Add(Task.Run(async () =>
        //            {
        //                try
        //                {
        //                    var err = await proc.StandardError.ReadToEndAsync();
        //                    if (!string.IsNullOrEmpty(err))
        //                    {
        //                        errorBuffers.Add(err);
        //                    }
        //                }
        //                catch { /* キャンセル・破棄例外の無視 */ }
        //            }));

        //            // 2. パイプの転送処理（独立して並行実行）
        //            if (lastOutputStream != null)
        //            {
        //                var currentInput = proc.StandardInput.BaseStream;
        //                var previousOutput = lastOutputStream;

        //                streamCopyTasks.Add(Task.Run(async () =>
        //                {
        //                    try
        //                    {
        //                        await previousOutput.CopyToAsync(currentInput);
        //                    }
        //                    catch
        //                    {
        //                        // 先頭コマンドが異常終了してパイプが破棄された場合などの保護
        //                    }
        //                    finally
        //                    {
        //                        currentInput.Close(); // ★次プロセスへ EOF 送信
        //                    }
        //                }));
        //            }

        //            lastOutputStream = proc.StandardOutput.BaseStream;
        //        }

        //        var lastProc = processes.Last();

        //        // 3. 最後のプロセスの標準出力を取得
        //        var outTask = lastProc.StandardOutput.ReadToEndAsync();
        //        readTasks.Add(outTask);

        //        // 4. まず全プロセスの終了を最優先で待機する
        //        // (streamCopyTasks はプロセスの進行に伴って自然に完了・解体される)
        //        await Task.WhenAll(processes.Select(p => p.WaitForExitAsync()));

        //        // 5. ストリーム読み出しタスクとコピー処理の完了を回収
        //        await Task.WhenAll(readTasks);
        //        await Task.WhenAll(streamCopyTasks);

        //        lastExitCode = lastProc.ExitCode;

        //        // リソース解放
        //        foreach (var p in processes)
        //        {
        //            p.Dispose();
        //        }

        //        var allErrors = string.Join("\n", errorBuffers);
        //        var finalOutput = outTask.Result;
        //        if (!string.IsNullOrEmpty(allErrors))
        //        {
        //            finalOutput += "\n" + allErrors;
        //        }

        //        return (finalOutput, lastExitCode);
        //    }
        //    catch (Exception ex)
        //    {
        //        return ($"Execution Error: {ex.Message}\n", -1);
        //    }
        //    finally
        //    {
        //        foreach (var p in processes) p.Dispose();
        //    }
        //}
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
