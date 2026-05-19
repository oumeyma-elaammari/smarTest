using Microsoft.EntityFrameworkCore;
using smartest_desktop.Data;
using smartest_desktop.Data.LocalEntities;
using System;
using System.Security.Cryptography;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using smartest_desktop.Exceptions;

namespace smartest_desktop.Services
{
    public static class GroqKeyService
    {
        private const string CLE_NOM = "groq_api_key";
        private const string GROQ_MODELS_URL = "https://api.groq.com/openai/v1/models";

        private static readonly HttpClient _http = new() { Timeout = System.TimeSpan.FromSeconds(10) };

        private static void EnsureTable(LocalDbContext db)
        {
            try
            {
                db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS app_setting (
                    Id     INTEGER PRIMARY KEY AUTOINCREMENT,
                    Cle    TEXT    NOT NULL UNIQUE,
                    Valeur TEXT    NOT NULL
                )");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Impossible de préparer le stockage local de la clé Groq.", ex);
            }
        }

        /// <summary>Clé Groq locale valide pour l'envoi au serveur (correction rédaction des examens).</summary>
        public static bool TryObtenirCleValide(LocalDbContext db, out string cle)
        {
            cle = string.Empty;
            string? lue = LireCle(db);
            if (!CleEstValide(lue ?? "")) return false;
            cle = lue!.Trim();
            return true;
        }

        public static string? LireCle(LocalDbContext db)
        {
            try
            {
                EnsureTable(db);
                var setting = db.AppSettings.FirstOrDefault(s => s.Cle == CLE_NOM);
                if (setting == null) return null;
                try
                {
                    return CryptoService.Dechiffrer(setting.Valeur);
                }
                catch (Exception)
                {
                    return null;
                }
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException(
                    "Lecture de la clé Groq en base locale impossible.", ex);
            }
        }

        public static void SauvegarderCle(LocalDbContext db, string cle)
        {
            ArgumentNullException.ThrowIfNull(cle);
            try
            {
                EnsureTable(db);
                var existing = db.AppSettings.FirstOrDefault(s => s.Cle == CLE_NOM);
                string chiffree = CryptoService.Chiffrer(cle);
                if (existing != null)
                    existing.Valeur = chiffree;
                else
                    db.AppSettings.Add(new AppSetting { Cle = CLE_NOM, Valeur = chiffree });
                db.SaveChanges();
            }
            catch (CryptographicException ex)
            {
                throw new InvalidOperationException("Chiffrement de la clé Groq impossible.", ex);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException(
                    "Enregistrement de la clé Groq dans la base locale impossible.", ex);
            }
        }

        public static void SupprimerCle(LocalDbContext db)
        {
            try
            {
                EnsureTable(db);
                var existing = db.AppSettings.FirstOrDefault(s => s.Cle == CLE_NOM);
                if (existing != null)
                {
                    db.AppSettings.Remove(existing);
                    db.SaveChanges();
                }
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException(
                    "Suppression de la clé Groq en base locale impossible.", ex);
            }
        }

        /// <summary>
        /// Vérifie la clé auprès de l'API Groq (GET /models — aucun token consommé).
        /// Retourne (true, message) si active, (false, message) sinon.
        /// </summary>
        public static async Task<(bool Valide, string Message)> TesterCleAsync(
            string apiKey,
            CancellationToken cancellationToken = default)
        {
            if (!CleEstValide(apiKey))
                return (false, "Format de clé invalide (doit commencer par gsk_)");

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, GROQ_MODELS_URL);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                using var response = await _http.SendAsync(request, cancellationToken);

                return response.StatusCode switch
                {
                    HttpStatusCode.OK => (true, "Clé active et fonctionnelle"),
                    HttpStatusCode.Unauthorized => (false,
                        "Clé révoquée ou supprimée sur console.groq.com"),
                    HttpStatusCode.TooManyRequests => (true,
                        "Clé valide (quota temporairement atteint)"),
                    _ => (false, $"Réponse inattendue ({(int)response.StatusCode})")
                };
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw new OperationCanceledException("Vérification Groq annulée.", ex);
                return (false, $"{SmartestNetworkException.ServerUnreachable(ex).Message} (délai dépassé.)");
            }
            catch (HttpRequestException ex)
            {
                return (false, SmartestNetworkException.ServerUnreachable(ex).Message);
            }
        }

        public static bool CleEstValide(string cle) =>
            !string.IsNullOrWhiteSpace(cle) && cle.Length >= 20 && cle.StartsWith("gsk_");
    }
}
