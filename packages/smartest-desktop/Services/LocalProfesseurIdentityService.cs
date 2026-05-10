using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Models;

namespace smartest_desktop.Services
{
    /// <summary>
    /// Après réinscription avec le même email, le fichier SQLite local peut encore contenir d’anciennes données.
    /// On mémorise l’identifiant professeur côté serveur et on recrée la base locale si le compte ne correspond plus.
    /// </summary>
    public static class LocalProfesseurIdentityService
    {
        public const string CleProfesseurIdServeur = "professeur_id_serveur";

        /// <summary>
        /// À appeler après connexion professeur (ou restauration de session) une fois <see cref="App.LocalDb"/> prêt.
        /// Recrée la base au même chemin si l’id prof serveur ne correspond plus aux données locales.
        /// </summary>
        public static async Task ApresConnexionProfesseurAsync(
            AuthResponse auth,
            CancellationToken cancellationToken = default)
        {
            if (auth == null || string.IsNullOrWhiteSpace(auth.Token))
                return;
            if (!string.Equals(auth.Role, "PROFESSEUR", StringComparison.OrdinalIgnoreCase))
                return;

            var api = new QuizWebPublicationApiService();
            long serverId = await api.GetProfesseurIdAsync(auth.Token, cancellationToken);

            var db = App.LocalDb;
            var row = await db.AppSettings
                .FirstOrDefaultAsync(s => s.Cle == CleProfesseurIdServeur, cancellationToken);

            long? stored = null;
            if (row != null && long.TryParse(row.Valeur, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                stored = parsed;

            bool doitRecréer = false;
            if (stored.HasValue && stored.Value != serverId)
                doitRecréer = true;

            if (!doitRecréer && !stored.HasValue)
            {
                var triples = await db.Quiz
                    .OrderBy(q => q.Id)
                    .Select(q => new { q.BackendQuizIdPublicationWeb, q.BackendQuizIdQr, q.BackendQuizId })
                    .Take(64)
                    .ToListAsync(cancellationToken);

                var idsQuizServeur = triples
                    .SelectMany(t => new long?[] { t.BackendQuizIdPublicationWeb, t.BackendQuizIdQr, t.BackendQuizId })
                    .Where(id => id is long x && x > 0)
                    .Cast<long>()
                    .Distinct()
                    .Take(8)
                    .ToList();

                foreach (var bid in idsQuizServeur)
                {
                    var (found, auteurPid) = await api.TryGetQuizAuteurAsync(bid, auth.Token, cancellationToken);
                    if (!found)
                        continue;
                    if (auteurPid != serverId)
                    {
                        doitRecréer = true;
                        break;
                    }
                }
            }

            if (doitRecréer)
                App.RecréerBaseSqliteAuCheminActuel();

            db = App.LocalDb;
            var setting = await db.AppSettings
                .FirstOrDefaultAsync(s => s.Cle == CleProfesseurIdServeur, cancellationToken);
            var valeur = serverId.ToString(CultureInfo.InvariantCulture);
            if (setting == null)
                db.AppSettings.Add(new AppSetting { Cle = CleProfesseurIdServeur, Valeur = valeur });
            else
                setting.Valeur = valeur;
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
