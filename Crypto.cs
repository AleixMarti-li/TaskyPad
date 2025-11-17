using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TaskyPad
{
    public static class Crypto
    {
        private static byte[] GetKey(string masterKey, byte[] salt)
        {
            var pbkdf2 = new Rfc2898DeriveBytes(masterKey, salt, 100000, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(32); // AES-256 key
        }

        public static bool Encrypt(string plainText, string masterKey, out string? result)
        {
            try
            {
                using var aes = Aes.Create();
                aes.GenerateIV();
                aes.GenerateKey();

                var salt = RandomNumberGenerator.GetBytes(16);
                var key = GetKey(masterKey, salt);

                aes.Key = key;

                using var encryptor = aes.CreateEncryptor();
                var bytes = Encoding.UTF8.GetBytes(plainText);
                var cipherBytes = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);

                // Guardamos: SALT + IV + DATA
                var final = salt.Concat(aes.IV).Concat(cipherBytes).ToArray();

                result = Convert.ToBase64String(final);
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }

        public static bool Decrypt(string encryptedText, string masterKey, out string? result)
        {
            try
            {
                var fullData = Convert.FromBase64String(encryptedText);

                var salt = fullData.Take(16).ToArray();
                var iv = fullData.Skip(16).Take(16).ToArray();
                var cipherData = fullData.Skip(32).ToArray();

                var key = GetKey(masterKey, salt);

                using var aes = Aes.Create();
                aes.Key = key;
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                var _result = decryptor.TransformFinalBlock(cipherData, 0, cipherData.Length);
                result = Encoding.UTF8.GetString(_result);
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }
    }
}
