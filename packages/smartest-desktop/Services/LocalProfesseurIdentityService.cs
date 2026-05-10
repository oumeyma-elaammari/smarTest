using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Models;

namespace smartest_desktop.Services
{
    /// <summary>
    /// Enregistre l’identifiant professeur côté serveur dans SQLite (diagnostic / cohérence).
    /// Ne doit plus déclencher de suppression ou recréation de la base : un changement de mot de passe
    /// ou une variation d’API ne doit pas faire disparaître les données locales pour l’interface.
    /// </summary>
    public static class LocalProfesseurIdentityService
    {
        public const string CleProfesseurIdServeur = "professeur_id_serveur";

        /// <summary>
        /// À appeler après connexion professeur (ou restauration de session) une fois <see cref="App.LocalDb"/> prêt.
        /// Met à jour <see cref="CleProfesseurIdServeur"/> à partir du profil serveur sans toucher aux tables métier.
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
