using System;
using System.Security.Cryptography;
using System.Text;

namespace smartest_desktop.Services
{
    public static class CryptoService
    {
        private static readonly byte[] Key = SHA256.HashData(
            Encoding.UTF8.GetBytes(Environment.MachineName + "_SmarTest_2026")
        );

        public static string Chiffrer(string texte)
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var bytes = Encoding.UTF8.GetBytes(texte);
            var chiffre = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);

            var result = new byte[aes.IV.Length + chiffre.Length];
            aes.IV.CopyTo(result, 0);
            chiffre.CopyTo(result, aes.IV.Length);

            return Convert.ToBase64String(result);
        }

        public static string Dechiffrer(string texteChiffre)
        {
            var donnees = Convert.FromBase64String(texteChiffre);

            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = donnees[..16];

            using var decryptor = aes.CreateDecryptor();
            var dechiffre = decryptor.TransformFinalBlock(donnees, 16, donnees.Length - 16);
            return Encoding.UTF8.GetString(dechiffre);
        }
    }
}