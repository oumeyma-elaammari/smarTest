using smartest_desktop.Data;
using smartest_desktop.Data.LocalEntities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace smartest_desktop.Services
{
    public class LocalExamenService
    {
        private readonly LocalDbContext _db;

        public LocalExamenService(LocalDbContext db)
        {
            _db = db;
        }

        public async Task<List<ExamenLocal>> GetAllAsync()
        {
            return await _db.Examens
                .Include(e => e.Questions)
                .OrderByDescending(e => e.DateCreation)
                .ToListAsync();
        }

        public async Task<ExamenLocal?> GetByIdAsync(int id)
        {
            return await _db.Examens
                .Include(e => e.Cours)
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<ExamenLocal> AjouterAsync(ExamenLocal examen)
        {
            _db.Examens.Add(examen);
            await _db.SaveChangesAsync();
            return examen;
        }

        public async Task ModifierAsync(ExamenLocal examen)
        {
            _db.Examens.Update(examen);
            await _db.SaveChangesAsync();
        }

        public async Task SupprimerAsync(int id)
        {
            var examen = await _db.Examens.FindAsync(id);
            if (examen != null)
            {
                _db.Examens.Remove(examen);
                await _db.SaveChangesAsync();
            }
        }

        public async Task ChangerStatutAsync(int id, string statut)
        {
            var examen = await _db.Examens.FindAsync(id);
            if (examen != null)
            {
                examen.Statut = statut;
                await _db.SaveChangesAsync();
            }
        }

        public async Task MettreAJourPublicationWebLocaleAsync(
            int examenLocalId,
            long? backendId,
            string emailsPublicationWebJson,
            string statut)
        {
            var examen = await _db.Examens.FindAsync(examenLocalId);
            if (examen == null)
                throw new System.InvalidOperationException("Examen introuvable ou déjà supprimé.");

            if (backendId.HasValue)
                examen.BackendId = backendId.Value;
            examen.EmailsPublicationWebJson = emailsPublicationWebJson ?? string.Empty;
            examen.Statut = statut;
            await _db.SaveChangesAsync();
        }
    }
}