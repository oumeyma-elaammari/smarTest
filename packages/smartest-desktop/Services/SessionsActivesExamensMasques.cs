using Microsoft.EntityFrameworkCore;
using smartest_desktop.Data;
using smartest_desktop.Data.LocalEntities;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace smartest_desktop.Services
{
    /// <summary>
    /// Examens retirés de « Sessions examens » sans suppression web (choix bureau uniquement).
    /// </summary>
    public static class SessionsActivesExamensMasques
    {
        private const string Cle = "sessions_actives.examens_masques.v1";

        private static void EnsureTable(LocalDbContext db)
        {
            db.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS app_setting (
                    Id     INTEGER PRIMARY KEY AUTOINCREMENT,
                    Cle    TEXT    NOT NULL UNIQUE,
                    Valeur TEXT    NOT NULL
                )");
        }

        public static HashSet<long> LireIds(LocalDbContext db)
        {
            var set = new HashSet<long>();
            if (db == null)
                return set;
            try
            {
                EnsureTable(db);
                string? json = db.AppSettings.FirstOrDefault(s => s.Cle == Cle)?.Valeur;
                if (string.IsNullOrWhiteSpace(json))
                    return set;
                var ids = JsonSerializer.Deserialize<List<long>>(json);
                if (ids == null)
                    return set;
                foreach (long id in ids)
                {
                    if (id > 0)
                        set.Add(id);
                }
            }
            catch
            {
                /* ignore */
            }
            return set;
        }

        public static void Masquer(LocalDbContext db, IEnumerable<long> backendIds)
        {
            if (db == null || backendIds == null)
                return;
            var set = LireIds(db);
            bool changed = false;
            foreach (long id in backendIds)
            {
                if (id > 0 && set.Add(id))
                    changed = true;
            }
            if (changed)
                Sauvegarder(db, set);
        }

        public static void Demasquer(LocalDbContext db, IEnumerable<long> backendIds)
        {
            if (db == null || backendIds == null)
                return;
            var set = LireIds(db);
            bool changed = false;
            foreach (long id in backendIds)
            {
                if (id > 0 && set.Remove(id))
                    changed = true;
            }
            if (changed)
                Sauvegarder(db, set);
        }

        private static void Sauvegarder(LocalDbContext db, HashSet<long> ids)
        {
            EnsureTable(db);
            string json = JsonSerializer.Serialize(ids.OrderBy(x => x).ToList());
            var existing = db.AppSettings.FirstOrDefault(s => s.Cle == Cle);
            if (existing != null)
                existing.Valeur = json;
            else
                db.AppSettings.Add(new AppSetting { Cle = Cle, Valeur = json });
            db.SaveChanges();
        }
    }
}
