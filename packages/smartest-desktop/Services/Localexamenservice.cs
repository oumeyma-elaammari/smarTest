using smartest_desktop.Data;
using smartest_desktop.Data.LocalEntities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace smartest_desktop.Services
{
    public class LocalExamenService
    {
        private readonly LocalDbContext _db;

        public LocalExamenService(LocalDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        private static async Task<T> InvokeDbAsync<T>(Func<Task<T>> action, string contexte)
        {
            try
            {
                return await action();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new InvalidOperationException(
                    $"{contexte} : données modifiées ou supprimées depuis le dernier chargement.", ex);
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException(
                    $"{contexte} : la base locale n’a pas pu enregistrer les changements.", ex);
            }
        }

        private static Task InvokeDbAsync(Func<Task> action, string contexte) =>
            InvokeDbAsync(async () =>
            {
                await action();
                return 0;
            }, contexte);

        public Task<List<ExamenLocal>> GetAllAsync(CancellationToken cancellationToken = default) =>
            InvokeDbAsync(() => _db.Examens
                .Include(e => e.Questions)
                .OrderByDescending(e => e.DateCreation)
                .ToListAsync(cancellationToken), "Liste des examens");

        public Task<ExamenLocal?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            InvokeDbAsync(() => _db.Examens
                .Include(e => e.Cours)
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.Id == id, cancellationToken), "Chargement de l’examen");

        public Task<ExamenLocal> AjouterAsync(ExamenLocal examen, CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                _db.Examens.Add(examen);
                await _db.SaveChangesAsync(cancellationToken);
                return examen;
            }, "Création de l’examen");

        public Task ModifierAsync(ExamenLocal examen, CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                _db.Examens.Update(examen);
                await _db.SaveChangesAsync(cancellationToken);
            }, "Mise à jour de l’examen");

        public Task SupprimerAsync(int id, CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                var examen = await _db.Examens.FindAsync(new object[] { id }, cancellationToken);
                if (examen != null)
                {
                    _db.Examens.Remove(examen);
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }, "Suppression de l’examen");

        public Task ChangerStatutAsync(int id, string statut, CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                var examen = await _db.Examens.FindAsync(new object[] { id }, cancellationToken);
                if (examen != null)
                {
                    examen.Statut = statut;
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }, "Changement de statut de l’examen");
    }
}
