using Avalonia.Controls.Shapes;
using Avalonia.Remote.Protocol;
using CodeEditor2.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CodeEditor2.Data
{
    public static class DataAccess
    {

        public static async Task<string> GetFileTextAsync(Project project, string relativePath)
        {
            if (!project.LocalFileCasheEnable)
            {
                return await getFileTextAsync(project, relativePath, false); // directly read from file without using cache
            }
            else if(!await PasswordManager.CheckPassWord())
            { // failed to get password
                project.LocalFileCasheEnable = false;
                return await getFileTextAsync(project, relativePath, false); // directly read from file without using cache
            }

            Item? item = project.GetItem(relativePath);
            FileSystemInfo? fileSystemInfo = item?.FileSystemInfo;

            string cashePath = project.GetCahsePath(relativePath);
            if(System.IO.File.Exists(cashePath))
            {
                try
                {
                    FileSystemInfo casheFileSystemInfo = new FileInfo(cashePath);
                    if(fileSystemInfo != null && casheFileSystemInfo.LastWriteTime < fileSystemInfo.LastWriteTime) // cashe is older than original file
                    { // must reload
                        return await getFileTextAsync(project, relativePath, true); // directly read from file without using cache, and make a new cahche
                    }

                    if (Global.FileEncriptionKey == null) throw new Exception("FileEncriptionKey is null");
                    return await DecryptFromFile(cashePath, Global.FileEncriptionKey);
                }
                catch (Exception)
                {
                    return await getFileTextAsync(project, relativePath,true); // if decryption fails, read directly from file without using cache, and make a new cahche
                }
            }
            else
            {

                return await getFileTextAsync(project, relativePath, true);
            }
        }
        private static async Task<string> getFileTextAsync(Project project, string relativePath,bool casheFile)
        {
            string text;
            using (FileStream fs = new FileStream(
                project.GetAbsolutePath(relativePath),
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096 * 32,
                useAsync: true)) // または FileOptions.Asynchronous
            {
                using var sr = new StreamReader(fs, Encoding.UTF8, true);
                // ReadAsync で非同期読み込み
                text = await sr.ReadToEndAsync();
            }

            if (!casheFile) return text;
            string cashePath = project.GetCahsePath(relativePath);

            string? casheDirectoryPath = System.IO.Path.GetDirectoryName(cashePath);
            if (!string.IsNullOrEmpty(casheDirectoryPath))
            {
                Directory.CreateDirectory(casheDirectoryPath);
            }
            if (Global.FileEncriptionKey == null) throw new Exception("FileEncriptionKey is null");
            await EncryptToFile(text, cashePath, Global.FileEncriptionKey);

            return text;
        }

        public static async Task SaveFileAsync(Project project, string relativePath, string text)
        {
            if (!project.LocalFileCasheEnable)
            {
                await saveFileAsync(project, relativePath, text,false); // directly write to file without using cache
                return;
            }
            else if (!await PasswordManager.CheckPassWord())
            { // failed to get password
                project.LocalFileCasheEnable = false;
                await saveFileAsync(project, relativePath, text,false); // directly write to file without using cache
                return;
            }

            // directly write to file and make a new cache
            await saveFileAsync(project, relativePath, text,true);
        }

        private static async Task saveFileAsync(Project project, string relativePath, string text,bool casheFile)
        {
            using (FileStream fs = new FileStream(
                        project.GetAbsolutePath(relativePath),
                        FileMode.Create, FileAccess.Write, FileShare.Read,
                        bufferSize: 4096*32, useAsync: true))
            {
                byte[] encodedText = Encoding.UTF8.GetBytes(text);
                await fs.WriteAsync(encodedText, 0, encodedText.Length);
                await fs.FlushAsync();
            }

            if (!casheFile) return;
            string cashePath = project.GetCahsePath(relativePath);

            string? casheDirectoryPath = System.IO.Path.GetDirectoryName(cashePath);
            if (!string.IsNullOrEmpty(casheDirectoryPath))
            {
                Directory.CreateDirectory(casheDirectoryPath);
            }
            if(Global.FileEncriptionKey == null) throw new Exception("FileEncriptionKey is null");
            await EncryptToFile(text, cashePath, Global.FileEncriptionKey);
        }

        public static Task UpdateFieSystemInfoAsync(Project project, string relativePath)
        {
            Item? item = project.GetItem(relativePath);
            if (item == null) return Task.CompletedTask;
            try
            {
                FileSystemInfo fsi = new FileInfo(project.GetAbsolutePath(relativePath));
                item.FileSystemInfo = fsi;
            }
            catch(FileNotFoundException)
            {
                removeItemAsync(project, item);
            }
            catch(DirectoryNotFoundException)
            {
                removeItemAsync(project, item);
            }
            catch (Exception)
            {
                item.FileSystemInfo = null;
            }
            return Task.CompletedTask;
        }

        public static async Task UpdateFieSystemInfoAndSubItemAsync(Project project,string relativePath)
        {
            var di = new DirectoryInfo(project.RootPath);
            var options = new EnumerationOptions { RecurseSubdirectories = true,MaxRecursionDepth = 1, IgnoreInaccessible = true };
            var infos = await Task.Run(() => di.EnumerateFileSystemInfos("*", options));

            foreach (var info in infos)
            {
                Item? item = project.GetItem(project.GetRelativePath(info.FullName));
                if (item != null)
                {
                    item.FileSystemInfo = info;
                }
            }
        }

        private static void removeItemAsync(Project project,Item item)
        {
            item.FileSystemInfo = null;
            item.IsDeleted = true;

            if (item is Folder folder)
            {
                if (project.LocalFileCasheEnable)
                {
                    string cashePath = project.GetCahsePath(item.RelativePath);
                    if (System.IO.Directory.Exists(cashePath)) System.IO.Directory.Delete(cashePath);
                }
                // フォルダの場合は、子アイテムも削除されたとみなす
                foreach (var child in folder.Items)
                {
                    removeItemAsync(project,child);
                }
            }else if(item is File file)
            {
                if (project.LocalFileCasheEnable)
                {
                    string cashePath = project.GetCahsePath(item.RelativePath);
                    if (System.IO.File.Exists(cashePath)) System.IO.File.Delete(cashePath);
                }
                // ファイルの場合は、親フォルダの子アイテムからも削除する
                if (item.Parent != null)
                {
                    item.Parent.Items.TryRemove(item.Name);
                }
            }
        }
        public static async Task UpdateFieSystemInfoAsync(Project project)
        {
            var di = new DirectoryInfo(project.RootPath);
            var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
            var infos = await Task.Run(() => di.EnumerateFileSystemInfos("*", options));

            foreach (var info in infos)
            {
                Item? item = project.GetItem(project.GetRelativePath(info.FullName));
                if(item != null)
                {
                    item.FileSystemInfo = info;
                }
            }
        }

        public static async Task<(List<string> absoluteFilePaths, List<string> absoluteFolderPaths)> GetFolderContents(Project project,string relativePath)
        {
            if (!project.LocalFileCasheEnable)
            {
                return await getFolderContents(project, relativePath); // directly read from file without using cache
            }
            else if (!await PasswordManager.CheckPassWord())
            { // failed to get password
                project.LocalFileCasheEnable = false;
                return await getFolderContents(project, relativePath); // directly read from file without using cache
            }

            Item? item = project.GetItem(relativePath);
            FileSystemInfo? fileSystemInfo = item?.FileSystemInfo;

            List<string> absoluteFilePaths = new List<string>();
            List<string> absoluteFolderPaths = new List<string>();

            string cashePath = project.GetCahsePath(relativePath);
            if (System.IO.File.Exists(cashePath))
            {
                try
                {
                    FileSystemInfo casheFileSystemInfo = new FileInfo(cashePath);
                    if (fileSystemInfo != null && casheFileSystemInfo.LastWriteTime < fileSystemInfo.LastWriteTime) // cashe is older than original file
                    { // must reload
                        await UpdateFieSystemInfoAndSubItemAsync(project, relativePath);
                    }

                    var di = new DirectoryInfo(cashePath);
                    var options = new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = true };
                    var infoList = await Task.Run(() => di.EnumerateFileSystemInfos("*", options));

                    foreach (var info in infoList)
                    {
                        if (info.Attributes.HasFlag(FileAttributes.Directory))
                        {
                            absoluteFolderPaths.Add(info.FullName);
                        }
                        else
                        {
                            absoluteFilePaths.Add(info.FullName);
                        }
                    }
                    return (absoluteFilePaths, absoluteFolderPaths);
                }
                catch (Exception)
                {
                    return await getFolderContents(project, relativePath); // directly read from file without using cache
                }
            }
            else
            {
                return await getFolderContents(project, relativePath); // directly read from file without using cache
            }
        }
        private static async Task<(List<string> absoluteFilePaths, List<string> absoluteFolderPaths)> getFolderContents(Project project, string relativePath)
        {
            List<string> absoluteFilePaths = new List<string>();
            List<string> absoluteFolderPaths = new List<string>();
            var di = new DirectoryInfo(project.GetAbsolutePath(relativePath));
            var options = new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = true };

            // 列挙自体は同期処理だが、Task.Run内で回すことで非同期ストリーム化
            var infoList = await Task.Run(() => di.EnumerateFileSystemInfos("*", options));

            foreach (var info in infoList)
            {
                if (info.Attributes.HasFlag(FileAttributes.Directory))
                {
                    absoluteFolderPaths.Add(info.FullName);
                }
                else
                {
                    absoluteFilePaths.Add(info.FullName);
                }
            }
            return (absoluteFilePaths, absoluteFolderPaths);
        }

        // --------------------------------------------------------------------------------------------------

        // 暗号化に使用する反復回数（多いほど安全ですが処理は重くなります）
        private const int Iterations = 100000;
        // キーのサイズ (AES-256)
        private const int KeySize = 256;

        /// <summary>
        /// 文字列を暗号化してファイルに保存する
        /// </summary>
        private static async Task EncryptToFile(string content, string filePath, byte[] fileEncriptkey)
        {
            // 1. ソルト（ランダムな値）を生成
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            // 2. パスワードとソルトから鍵を派生させる
            using var deriveBytes = new Rfc2898DeriveBytes(fileEncriptkey, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] key = deriveBytes.GetBytes(KeySize / 8);

            using var aes = Aes.Create();
            aes.Key = key;
            // IV（初期化ベクトル）を自動生成
            aes.GenerateIV();

            using FileStream fs = new FileStream(filePath, FileMode.Create);
            // 後で復号するために、ソルトとIVをファイルの先頭に書き込む
            fs.Write(salt, 0, salt.Length);
            fs.Write(aes.IV, 0, aes.IV.Length);

            using var encryptor = aes.CreateEncryptor();
            using var cryptoStream = new CryptoStream(fs, encryptor, CryptoStreamMode.Write);
            using var writer = new StreamWriter(cryptoStream);

            await writer.WriteAsync(content);
        }




        /// <summary>
        /// 暗号化されたファイルを読み込んで復号する
        /// </summary>
        private static async Task<string> DecryptFromFile(string filePath, byte[] fileEncriptkey)
        {
            // FileShare.ReadWrite を指定することで、他プロセスが書き込み中でも読み取り可能になります
            using FileStream fs = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            // 1. ファイルからソルトを読み出す
            byte[] salt = new byte[16];
            await fs.ReadAsync(salt, 0, salt.Length); // ついでに非同期メソッドに修正

            // 2. ファイルからIVを読み出す
            byte[] iv = new byte[16];
            await fs.ReadAsync(iv, 0, iv.Length);

            // 3. パスワードとソルトから同じ鍵を生成する
            using var deriveBytes = new Rfc2898DeriveBytes(fileEncriptkey, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] key = deriveBytes.GetBytes(KeySize / 8);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            using var cryptoStream = new CryptoStream(fs, decryptor, CryptoStreamMode.Read);
            using var reader = new StreamReader(cryptoStream);

            return await reader.ReadToEndAsync();
        }
    }
}
