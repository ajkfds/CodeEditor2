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
                useAsync: true)) // 縺ｾ縺溘・ FileOptions.Asynchronous
            {
                using var sr = new StreamReader(fs, Encoding.UTF8, true);
                // ReadAsync 縺ｧ髱槫酔譛溯ｪｭ縺ｿ霎ｼ縺ｿ
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
                // 繝輔か繝ｫ繝縺ｮ蝣ｴ蜷医・縲∝ｭ舌い繧､繝・Β繧ょ炎髯､縺輔ｌ縺溘→縺ｿ縺ｪ縺・                foreach (var child in folder.Items)
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
                // 繝輔ぃ繧､繝ｫ縺ｮ蝣ｴ蜷医・縲∬ｦｪ繝輔か繝ｫ繝縺ｮ蟄舌い繧､繝・Β縺九ｉ繧ょ炎髯､縺吶ｋ
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

            // 蛻玲嫌閾ｪ菴薙・蜷梧悄蜃ｦ逅・□縺後ゝask.Run蜀・〒蝗槭☆縺薙→縺ｧ髱槫酔譛溘せ繝医Μ繝ｼ繝蛹・            var infoList = await Task.Run(() => di.EnumerateFileSystemInfos("*", options));

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

        // 證怜捷蛹悶↓菴ｿ逕ｨ縺吶ｋ蜿榊ｾｩ蝗樊焚・亥､壹＞縺ｻ縺ｩ螳牙・縺ｧ縺吶′蜃ｦ逅・・驥阪￥縺ｪ繧翫∪縺呻ｼ・        private const int Iterations = 100000;
        // 繧ｭ繝ｼ縺ｮ繧ｵ繧､繧ｺ (AES-256)
        private const int KeySize = 256;

        /// <summary>
        /// 譁・ｭ怜・繧呈囓蜿ｷ蛹悶＠縺ｦ繝輔ぃ繧､繝ｫ縺ｫ菫晏ｭ倥☆繧・        /// </summary>
        private static async Task EncryptToFile(string content, string filePath, byte[] fileEncriptkey)
        {
            // 1. 繧ｽ繝ｫ繝茨ｼ医Λ繝ｳ繝繝縺ｪ蛟､・峨ｒ逕滓・
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            // 2. 繝代せ繝ｯ繝ｼ繝峨→繧ｽ繝ｫ繝医°繧蛾嵯繧呈ｴｾ逕溘＆縺帙ｋ
            using var deriveBytes = new Rfc2898DeriveBytes(fileEncriptkey, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] key = deriveBytes.GetBytes(KeySize / 8);

            using var aes = Aes.Create();
            aes.Key = key;
            // IV・亥・譛溷喧繝吶け繝医Ν・峨ｒ閾ｪ蜍慕函謌・            aes.GenerateIV();

            using FileStream fs = new FileStream(filePath, FileMode.Create);
            // 蠕後〒蠕ｩ蜿ｷ縺吶ｋ縺溘ａ縺ｫ縲√た繝ｫ繝医→IV繧偵ヵ繧｡繧､繝ｫ縺ｮ蜈磯ｭ縺ｫ譖ｸ縺崎ｾｼ繧
            fs.Write(salt, 0, salt.Length);
            fs.Write(aes.IV, 0, aes.IV.Length);

            using var encryptor = aes.CreateEncryptor();
            using var cryptoStream = new CryptoStream(fs, encryptor, CryptoStreamMode.Write);
            using var writer = new StreamWriter(cryptoStream);

            await writer.WriteAsync(content);
        }




        /// <summary>
        /// 證怜捷蛹悶＆繧後◆繝輔ぃ繧､繝ｫ繧定ｪｭ縺ｿ霎ｼ繧薙〒蠕ｩ蜿ｷ縺吶ｋ
        /// </summary>
        private static async Task<string> DecryptFromFile(string filePath, byte[] fileEncriptkey)
        {
            // FileShare.ReadWrite 繧呈欠螳壹☆繧九％縺ｨ縺ｧ縲∽ｻ悶・繝ｭ繧ｻ繧ｹ縺梧嶌縺崎ｾｼ縺ｿ荳ｭ縺ｧ繧りｪｭ縺ｿ蜿悶ｊ蜿ｯ閭ｽ縺ｫ縺ｪ繧翫∪縺・            using FileStream fs = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            // 1. 繝輔ぃ繧､繝ｫ縺九ｉ繧ｽ繝ｫ繝医ｒ隱ｭ縺ｿ蜃ｺ縺・            byte[] salt = new byte[16];
            await fs.ReadAsync(salt, 0, salt.Length); // 縺､縺・〒縺ｫ髱槫酔譛溘Γ繧ｽ繝・ラ縺ｫ菫ｮ豁｣

            // 2. 繝輔ぃ繧､繝ｫ縺九ｉIV繧定ｪｭ縺ｿ蜃ｺ縺・            byte[] iv = new byte[16];
            await fs.ReadAsync(iv, 0, iv.Length);

            // 3. 繝代せ繝ｯ繝ｼ繝峨→繧ｽ繝ｫ繝医°繧牙酔縺倬嵯繧堤函謌舌☆繧・            using var deriveBytes = new Rfc2898DeriveBytes(fileEncriptkey, salt, Iterations, HashAlgorithmName.SHA256);
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
