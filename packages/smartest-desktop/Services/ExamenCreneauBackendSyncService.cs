using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using smartest_desktop.Data;
using smartest_desktop.Helpers;
using smartest_desktop.Services;

namespace smartest_desktop.Services
{
    /// <summary>
    /// Propage la date prévue locale vers le backend lorsqu'un examen est déjà publié sur le web.
    /// </summary>
    public sealed class ExamenCreneauBackendSyncService
    {
        private readonly LocalDbContext _db;

        public ExamenCreneauBackendSyncService(LocalDbContext db)
        {
            _db = db;
        }

        public async Task SynchroniserSiPublieAsync(
            int examenLocalId,
            DateTime? datePrevue,
            int dureeMinutes,
            CancellationToken cancellationToken = default)
        {
            if (!datePrevue.HasValue || examenLocalId <= 0)
                return;

            var examen = await _db.Examens
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == examenLocalId, cancellationToken);
            if (examen?.BackendId is not long backendId || backendId <= 0)
                return;

            var statut = (examen.Statut ?? string.Empty).Trim();
            if (!string.Equals(statut, "PUBLIE", StringComparison.OrdinalIgnoreCase))
                return;

            if (!DesktopSessionTokenHelper.TryObtenir(out var token))
                return;

            var debut = datePrevue.Value;
            if (debut.Kind == DateTimeKind.Utc)
                debut = debut.ToLocalTime();
            else if (debut.Kind == DateTimeKind.Unspecified)
                debut = DateTime.SpecifyKind(debut, DateTimeKind.Local);

            var api = new ExamenWebPublicationApiService();
            await api.ModifierCreneauAsync(
                token,
                backendId,
                debut,
                Math.Max(1, dureeMinutes),
                cancellationToken);
        }
    }
}
