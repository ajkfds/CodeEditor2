using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System.Diagnostics;

namespace CodeEditor2.Tools
{
    public class PasswordManager
    {

        public static async Task<bool> CheckPassWord()
        {
            if (Global.FileEncriptionKey != null) return true;
            if(Global.Setup.PasswordHash =="")
            { //  set new paddword
                Tools.InputWindow inputWindow = new InputWindow("new password", "input new password");
                inputWindow.PassWordMode = true;
                await inputWindow.ShowDialog(Controller.GetMainWindow());
                if (inputWindow.Cancel) return false;
                string passWord = inputWindow.InputText;
                if (passWord == "") return false;
                string passwordHash, passwordSalt, derivedSalt;
                HashPassword(passWord,out passwordHash,out passwordSalt,out derivedSalt);
                Global.Setup.PasswordHash = passwordHash;
                Global.Setup.PasswordSalt = passwordSalt;
                Global.Setup.DerivedSalt = derivedSalt;
                Global.Setup.SaveSetup();
                return true;
            }
            else
            { // input and check password
                while(Global.FileEncriptionKey == null)
                {
                    Tools.InputWindow inputWindow = new InputWindow("input password", "input password");
                    inputWindow.PassWordMode = true;
                    await inputWindow.ShowDialog(Controller.GetMainWindow());
                    if (inputWindow.Cancel) return false;
                    string passWord = inputWindow.InputText;
                    byte[]? derivedKey;
                    if (VerifyPasswordAndGetDerivedKey(
                        passWord, 
                        Global.Setup.PasswordHash,
                        Global.Setup.PasswordSalt,
                        Global.Setup.DerivedSalt,
                        out derivedKey))
                    {
                        if (derivedKey == null) continue;
                        Global.FileEncriptionKey = derivedKey;
                        return true;
                    }
                    CodeEditor2.Controller.AppendLog("failed to check password.", Avalonia.Media.Colors.Red);
                }
            }
            return false;
        }

        // 繧ｻ繧ｭ繝･繝ｪ繝・ぅ險ｭ螳夲ｼ・026蟷ｴ迴ｾ蝨ｨ縺ｮ謗ｨ螂ｨ蛟､・・
        private const int Iterations = 600000; // 險育ｮ苓ｲ闕ｷ繧剃ｸ翫￡縺ｦ邱丞ｽ薙◆繧頑判謦・ｒ髦ｲ縺・
        private const int KeySizeByte = 32;    // 256繝薙ャ繝・(AES-256逕ｨ)
        private const int SaltSizeByte = 16;   // 128繝薙ャ繝井ｻ･荳翫・繧ｽ繝ｫ繝・

        private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

        /// <summary>
        /// 繝代せ繝ｯ繝ｼ繝峨ｒ繝上ャ繧ｷ繝･蛹悶☆繧具ｼ井ｿ晏ｭ倡畑・・
        /// </summary>
        /// <returns>繧ｽ繝ｫ繝医→繝上ャ繧ｷ繝･繧堤ｵ仙粋縺励◆譁・ｭ怜・</returns>
        public static void HashPassword(string password,out string passwordHash,out string passwordSalt,out string derivedSalt)
        {
            // 1. 繝ｩ繝ｳ繝繝縺ｪ繧ｽ繝ｫ繝医ｒ逕滓・
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeByte);

            // 2. 繝代せ繝ｯ繝ｼ繝峨ｒ繝上ャ繧ｷ繝･蛹・
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                HashAlgorithm,
                KeySizeByte);

            passwordHash = Convert.ToBase64String(hash);
            passwordSalt = Convert.ToBase64String(salt);

            derivedSalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(SaltSizeByte));
        }

        public static bool VerifyPasswordAndGetDerivedKey(
            string password,
            string passwordHash,
            string passwordSalt,
            string derivedSalt,
            out byte[]? derivedKey)
        {
            derivedKey = null;

            // 繝代せ繝ｯ繝ｼ繝峨ｒ荳譎ら噪縺ｪ繝舌う繝磯・蛻励↓螟画鋤
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            try
            {
                // 1. 菫晏ｭ倥＆繧後◆繝・・繧ｿ縺ｮ繝・さ繝ｼ繝・
                byte[] expectedHash = Convert.FromBase64String(passwordHash);
                byte[] salt = Convert.FromBase64String(passwordSalt);
                byte[] dSalt = Convert.FromBase64String(derivedSalt);

                // 2. 辣ｧ蜷育畑繝上ャ繧ｷ繝･縺ｮ險育ｮ・(PBKDF2)
                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    passwordBytes,
                    salt,
                    Iterations,
                    HashAlgorithm,
                    KeySizeByte);

                // 3. 螳牙・縺ｪ豈碑ｼ・
                if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
                {
                    return false;
                }

                // 4. 證怜捷蛹也畑縺ｮ骰ｵ繧呈ｴｾ逕・
                // 譁ｰ縺励＞ .NET 縺ｧ縺ｯ Rfc2898DeriveBytes.Pbkdf2 (static) 繧剃ｽｿ縺・婿縺檎ｰ｡貎斐〒縺・
                derivedKey = Rfc2898DeriveBytes.Pbkdf2(
                    passwordBytes,
                    dSalt,
                    Iterations + 1,
                    HashAlgorithm,
                    KeySizeByte);

                return true;
            }
            finally
            {
                // 繝｡繝｢繝ｪ荳翫・逕溘ヱ繧ｹ繝ｯ繝ｼ繝会ｼ医ヰ繧､繝磯・蛻暦ｼ峨ｒ遒ｺ螳溘↓豸亥悉
                CryptographicOperations.ZeroMemory(passwordBytes);
            }
        }
    }
}