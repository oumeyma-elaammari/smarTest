using Microsoft.EntityFrameworkCore;
using smartest_desktop.Data;
using smartest_desktop.Data.LocalEntities;
using System.Linq;

namespace smartest_desktop.Services
{
    /// <summary>Cache SQLite du dernier GET participants pour Sessions examens (affichage prof).</summary>
    public static class SessionsActivesPassagesCache
    {
        private static string Cle(long backendExamenId) => $"sessions_actives.passages.v1:{backendExamenId}";

        private static void EnsureTable(LocalDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS app_setting (
                    Id     INTEGER PRIMARY KEY AUTOINCREMENT,
                    Cle    TEXT    NOT NULL UNIQUE,
                    Valeur TEXT    NOT NULL
                )");
        }

        public static void Sauvegarder(LocalDbContext db, long backendExamenId, string json)
        {
            if (db == null || backendExamenId <= 0 || string.IsNullOrWhiteSpace(json))
                return;
            EnsureTable(db);
            string cle = Cle(backendExamenId);
            var existing = db.AppSettings.FirstOrDefault(s => s.Cle == cle);
            if (existing != null)
                existing.Valeur = json;
            else
                db.AppSettings.Add(new AppSetting { Cle = cle, Valeur = json });
            db.SaveChanges();
        }

        public static string? EssayerCharger(LocalDbContext db, long backendExamenId)
        {
            if (db == null || backendExamenId <= 0)
                return null;
            try
            {
                EnsureTable(db);
                return db.AppSettings.FirstOrDefault(s => s.Cle == Cle(backendExamenId))?.Valeur;
            }
            catch
            {
                return null;
            }
        }

        public static void Supprimer(LocalDbContext db, long backendExamenId)
        {
            if (db == null || backendExamenId <= 0)
                return;
            try
            {
                EnsureTable(db);
                string cle = Cle(backendExamenId);
                var existing = db.AppSettings.FirstOrDefault(s => s.Cle == cle);
                if (existing == null)
                    return;
                db.AppSettings.Remove(existing);
                db.SaveChanges();
            }
            catch
            {
                /* cache optionnel */
            }
        }
    }
}
