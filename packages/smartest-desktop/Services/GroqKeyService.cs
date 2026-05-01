using Microsoft.EntityFrameworkCore;
using smartest_desktop.Data;
using smartest_desktop.Data.LocalEntities;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace smartest_desktop.Services
{
    public static class GroqKeyService
    {
        private const string CLE_NOM = "groq_api_key";
        private const string GROQ_MODELS_URL = "https://api.groq.com/openai/v1/models";

        private static readonly HttpClient _http = new() { Timeout = System.TimeSpan.FromSeconds(10) };

        private static void EnsureTable(LocalDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS app_setting (
                    Id     INTEGER PRIMARY KEY AUTOINCREMENT,
                    Cle    TEXT    NOT NULL UNIQUE,
                    Valeur TEXT    NOT NULL
                )");
        }

        public static string? LireCle(LocalDbContext db)
        {
            EnsureTable(db);
            var setting = db.AppSettings.FirstOrDefault(s => s.Cle == CLE_NOM);
            if (setting == null) return null;
            try { return CryptoService.Dechiffrer(setting.Valeur); }
            catch { return null; }
        }

        public static void SauvegarderCle(LocalDbContext db, string cle)
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

        public static void SupprimerCle(LocalDbContext db)
        {
            EnsureTable(db);
            var existing = db.AppSettings.FirstOrDefault(s => s.Cle == CLE_NOM);
            if (existing != null)
            {
                db.AppSettings.Remove(existing);
                db.SaveChanges();
            }
        }

        /// <summary>
        /// Vérifie la clé auprès de l'API Groq (GET /models — aucun token consommé).
        /// Retourne (true, message) si active, (false, message) sinon.
        /// </summary>
        public static async Task<(bool Valide, string Message)> TesterCleAsync(string apiKey)
        {
            if (!CleEstValide(apiKey))
                return (false, "Format de clé invalide (doit commencer par gsk_)");

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, GROQ_MODELS_URL);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                var response = await _http.SendAsync(request);

                return response.StatusCode switch
                {
                    HttpStatusCode.OK          => (true,  "Clé active et fonctionnelle"),
                    HttpStatusCode.Unauthorized => (false, "Clé révoquée ou supprimée sur console.groq.com"),
                    HttpStatusCode.TooManyRequests => (true, "Clé valide (quota temporairement atteint)"),
                    _ => (false, $"Réponse inattendue ({(int)response.StatusCode})")
                };
            }
            catch
            {
                return (false, "Impossible de contacter Groq — vérifiez votre connexion");
            }
        }

        public static bool CleEstValide(string cle) =>
            !string.IsNullOrWhiteSpace(cle) && cle.Length >= 20 && cle.StartsWith("gsk_");
    }
}
