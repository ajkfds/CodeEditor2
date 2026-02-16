using AjkAvaloniaLibs.Controls;
using Avalonia.Controls.Shapes;
using CodeEditor2.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace CodeEditor2.Tools
{
    public static class FileIO
    {

        private static bool RestrictToSingleAccess = true;
        private static int TimeoutSeconds = 20;
        private static readonly SemaphoreSlim _fileSemaphore = new SemaphoreSlim(1, 1);

        public static async Task AppendFileLists(string path, EnumerationOptions options, string[] ExcludedDirectories,Data.Project project, StringBuilder sb)
        {
            await WithTimeout(Task.Run(() =>
            {
                if (!System.IO.File.Exists(path)) throw new FileNotFoundException();

                // フィルタリングしながらリストを作成
                var entries = Directory.EnumerateFileSystemEntries(path, "*", options)
                    .Where(entry => !ExcludedDirectories.Any(ex => entry.Contains(System.IO.Path.DirectorySeparatorChar + ex + System.IO.Path.DirectorySeparatorChar) || entry.EndsWith(System.IO.Path.DirectorySeparatorChar + ex)));

                foreach (var entry in entries)
                {
                    // ルートディレクトリからの相対パスに変換して表示
                    string relativePath = System.IO.Path.GetRelativePath(project.RootPath, entry);
                    bool isDir = Directory.Exists(entry);
                    sb.AppendLine($"{(isDir ? "[DIR] " : "[FILE]")} {relativePath}");
                }

                return true;
            }), TimeSpan.FromSeconds(TimeoutSeconds));
        }
        public static async Task<FileInfo> GetFileInfo(string path)
        {
            return await WithTimeout(Task.Run(() =>
            {
                if (!System.IO.File.Exists(path)) throw new FileNotFoundException();
                return GetFileInfo(path);
            }), TimeSpan.FromSeconds(TimeoutSeconds));
        }
        public static async Task<string[]> GetFiles(string path)
        {
            return await WithTimeout(Task.Run(() =>
            {
                if (!System.IO.Directory.Exists(path)) throw new FileNotFoundException();
                return System.IO.Directory.GetFiles(path);
            }), TimeSpan.FromSeconds(TimeoutSeconds));
        }

        public static async Task<string[]> GetDirectories(string path)
        {
            return await WithTimeout(Task.Run(() =>
            {
                if (!System.IO.Directory.Exists(path)) throw new FileNotFoundException();

                return System.IO.Directory.GetDirectories(path);
            }), TimeSpan.FromSeconds(TimeoutSeconds));
        }
        public static async Task<string> GetFileText(string path)
        {
            return await WithTimeout(Task.Run(() =>
            {
                if (!System.IO.File.Exists(path)) throw new FileNotFoundException();

                using var fs = new FileStream(
                    path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete, 4096, FileOptions.SequentialScan);
                using var sr = new StreamReader(fs, Encoding.UTF8, true);

                string text = sr.ReadToEnd();
                return text;
            }), TimeSpan.FromSeconds(TimeoutSeconds));
        }


        public static async Task<bool> FileExists(string path)
        {
            try
            {
                return await WithTimeout(Task.Run(() =>
                {
                    return System.IO.File.Exists(path);
                }), TimeSpan.FromSeconds(TimeoutSeconds));
            }
            catch (TimeoutException)
            {
                return false;
            }
            catch (Exception)
            {
                // その他のエラー（権限不足など）
                return false;
            }
        }

        public static async Task SaveFile(string path, string text)
        {
            await WithTimeout(Task.Run(async () =>
            {
                using (FileStream fs = new FileStream(
                            path,
                            FileMode.Create, FileAccess.Write, FileShare.Read,
                            bufferSize: 4096, useAsync: true))
                {
                    byte[] encodedText = Encoding.UTF8.GetBytes(text);
                    await fs.WriteAsync(encodedText, 0, encodedText.Length);
                    await fs.FlushAsync();
                }
                return true;
            }), TimeSpan.FromSeconds(TimeoutSeconds));
        }
 
        private static async Task<T> WithTimeout<T>(Task<T> task, TimeSpan timeout)
        {
            if (RestrictToSingleAccess)
            {
                await _fileSemaphore.WaitAsync();
                try
                {
                    using (var cts = new CancellationTokenSource())
                    {
                        var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
                        if (completedTask == task)
                        {
                            cts.Cancel(); // Delayタスクをキャンセル
                            return await task;
                        }
                        else
                        {
                            throw new TimeoutException("access timed out");
                        }
                    }
                }
                finally
                {
                    _fileSemaphore.Release();
                }
            }
            else
            {
                using (var cts = new CancellationTokenSource())
                {
                    var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));
                    if (completedTask == task)
                    {
                        cts.Cancel(); // Delayタスクをキャンセル
                        return await task;
                    }
                    else
                    {
                        throw new TimeoutException("access timed out");
                    }
                }
            }
        }


    }
}
