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
            ArgumentNullException.ThrowIfNull(texte);

            try
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
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("Échec du chiffrement des données locales.", ex);
            }
        }

        public static string Dechiffrer(string texteChiffre)
        {
            ArgumentNullException.ThrowIfNull(texteChiffre);

            byte[] donnees;
            try
            {
                donnees = Convert.FromBase64String(texteChiffre);
            }
            catch (FormatException ex)
            {
                throw new FormatException("Données chiffrées invalides (Base64 attendu).", ex);
            }

            if (donnees.Length <= 16)
                throw new CryptographicException("Données chiffrées trop courtes pour contenir un IV et un bloc valide.");

            try
            {
                using var aes = Aes.Create();
                aes.Key = Key;
                aes.IV = donnees.AsSpan(0, 16).ToArray();

                using var decryptor = aes.CreateDecryptor();
                var dechiffre = decryptor.TransformFinalBlock(donnees, 16, donnees.Length - 16);
                return Encoding.UTF8.GetString(dechiffre);
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("Impossible de déchiffrer (données corrompues ou ancienne clé machine).", ex);
            }
        }
    }
}
