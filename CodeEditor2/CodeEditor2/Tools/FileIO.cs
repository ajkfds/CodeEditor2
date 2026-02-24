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
using Tmds.DBus.Protocol;
using static System.Net.Mime.MediaTypeNames;

namespace CodeEditor2.Tools
{
    public static class FileIO
    {

        private static bool RestrictToSingleAccess = false;
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

        public static async IAsyncEnumerable<FileSystemInfo> EnumerateFilesAsync(string path)
        {
            var di = new DirectoryInfo(path);
            var options = new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = true };

            // 列挙自体は同期処理だが、Task.Run内で回すことで非同期ストリーム化
            var items = await Task.Run(() => di.EnumerateFileSystemInfos("*", options));

            foreach (var item in items)
            {
                // 1件ごとに呼び出し元へ戻す。
                // ここで必要に応じて Task.Yield() などを挟むとより細かく制御可能
                yield return item;
            }
        }


        public class FileNode
        {
            public void AddItem(List<string> path,FileSystemInfo fileSystemInfo)
            {
                if (!fileSystemInfo.Attributes.HasFlag(FileAttributes.Directory))
                {
                    if(path.Count == 1)
                    {
                        Nodes.Add(path[0], new FileNode() { Name = path[0], IsDirectory = false, FileSystemInfo = fileSystemInfo });
                    }
                    else if(Nodes.ContainsKey(path[0]))
                    {
                        FileNode nextNode = Nodes[path[0]];
                        path.RemoveAt(0);
                        nextNode.AddItem(path, fileSystemInfo);
                    }
                }
                else
                {
                    if (Nodes.ContainsKey(path[0]))
                    {
                        FileNode nextNode = Nodes[path[0]];
                        path.RemoveAt(0);
                        nextNode.AddItem(path, fileSystemInfo);
                    }
                    else
                    {
                        Nodes.Add(path[0], new FileNode() { Name = path[0], IsDirectory = true, FileSystemInfo = fileSystemInfo });
                        path.RemoveAt(0);
                        if (path.Count > 0)
                        {
                            Nodes[path[0]].AddItem(path, fileSystemInfo);
                        }
                    }
                }
            }
            public bool IsDirectory { get; set; } = false;
            public string Name { get; set; } = "";
            public FileSystemInfo? FileSystemInfo { get; set; }
            public Dictionary<string,FileNode> Nodes = new Dictionary<string, FileNode>();
        }


        public static async Task<FileNode> GetFileTree(string path)
        {
            FileNode rootNode = new FileNode() { IsDirectory = true };

            await foreach (var info in FileIO.EnumerateFilesHierarchyAsync(path))
            {
                if (!info.FullName.StartsWith(path)) continue;

                string relativePath = info.FullName.Substring(path.Length);

                List<string> paths = relativePath.Split(new char[] { System.IO.Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (paths.Count <= 0) continue;

                rootNode.AddItem(paths, info);
            }
            return rootNode;
        }

        public static async IAsyncEnumerable<FileSystemInfo> EnumerateFilesHierarchyAsync(string path)
        {
            var di = new DirectoryInfo(path);
            var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };

            // 列挙自体は同期処理だが、Task.Run内で回すことで非同期ストリーム化
            var items = await Task.Run(() => di.EnumerateFileSystemInfos("*", options));

            foreach (var item in items)
            {
                // 1件ごとに呼び出し元へ戻す。
                // ここで必要に応じて Task.Yield() などを挟むとより細かく制御可能
                yield return item;
            }
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
            // 重要なポイント: options に FileOptions.Asynchronous を指定する
            using (FileStream fs = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096*32,
                useAsync: true)) // または FileOptions.Asynchronous
            {
                using var sr = new StreamReader(fs, Encoding.UTF8, true);
                // ReadAsync で非同期読み込み
                return await sr.ReadToEndAsync();
            }
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
