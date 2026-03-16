using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
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

        // セキュリティ設定（2026年現在の推奨値）
        private const int Iterations = 600000; // 計算負荷を上げて総当たり攻撃を防ぐ
        private const int KeySizeByte = 32;    // 256ビット (AES-256用)
        private const int SaltSizeByte = 16;   // 128ビット以上のソルト

        private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

        /// <summary>
        /// パスワードをハッシュ化する（保存用）
        /// </summary>
        /// <returns>ソルトとハッシュを結合した文字列</returns>
        public static void HashPassword(string password,out string passwordHash,out string passwordSalt,out string derivedSalt)
        {
            // 1. ランダムなソルトを生成
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSizeByte);

            // 2. パスワードをハッシュ化
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

            // パスワードを一時的なバイト配列に変換
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            try
            {
                // 1. 保存されたデータのデコード
                byte[] expectedHash = Convert.FromBase64String(passwordHash);
                byte[] salt = Convert.FromBase64String(passwordSalt);
                byte[] dSalt = Convert.FromBase64String(derivedSalt);

                // 2. 照合用ハッシュの計算 (PBKDF2)
                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    passwordBytes,
                    salt,
                    Iterations,
                    HashAlgorithm,
                    KeySizeByte);

                // 3. 安全な比較
                if (!CryptographicOperations.FixedTimeEquals(actualHash, expectedHash))
                {
                    return false;
                }

                // 4. 暗号化用の鍵を派生
                // 新しい .NET では Rfc2898DeriveBytes.Pbkdf2 (static) を使う方が簡潔です
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
                // メモリ上の生パスワード（バイト配列）を確実に消去
                CryptographicOperations.ZeroMemory(passwordBytes);
            }
        }
    }
}
