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
    public class LocalQuizService
    {
        private readonly LocalDbContext _db;

        public LocalQuizService(LocalDbContext db)
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

        private static async Task InvokeDbAsync(Func<Task> action, string contexte) =>
            await InvokeDbAsync(async () =>
            {
                await action();
                return 0;
            }, contexte);

        public Task<List<QuizLocal>> GetAllAsync(CancellationToken cancellationToken = default) =>
            InvokeDbAsync(() => _db.Quiz
                .OrderByDescending(q => q.DateCreation)
                .ToListAsync(cancellationToken), "Liste des quiz");

        public Task<QuizLocal?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            InvokeDbAsync(() => _db.Quiz
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == id, cancellationToken), "Chargement du quiz");

        public Task<QuizLocal> AjouterAsync(QuizLocal quiz, CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                _db.Quiz.Add(quiz);
                await _db.SaveChangesAsync(cancellationToken);
                return quiz;
            }, "Création du quiz");

        public Task ModifierAsync(QuizLocal quiz, CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                _db.Quiz.Update(quiz);
                await _db.SaveChangesAsync(cancellationToken);
            }, "Mise à jour du quiz");

        public Task SupprimerAsync(int id, CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                var quiz = await _db.Quiz.FindAsync(new object[] { id }, cancellationToken);
                if (quiz != null)
                {
                    _db.Quiz.Remove(quiz);
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }, "Suppression du quiz");

        public Task ChangerStatutAsync(int id, string statut, CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                var quiz = await _db.Quiz.FindAsync(new object[] { id }, cancellationToken);
                if (quiz != null)
                {
                    quiz.Statut = statut;
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }, "Changement de statut du quiz");

        public Task MettreAJourPublicationWebLocaleAsync(
            int quizLocalId,
            long? backendQuizId,
            string emailsPublicationWebJson,
            string statut,
            CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                var quiz = await _db.Quiz.FindAsync(new object[] { quizLocalId }, cancellationToken);
                if (quiz == null)
                    throw new InvalidOperationException("Quiz introuvable ou déjà supprimé.");

                if (backendQuizId.HasValue)
                    quiz.BackendQuizId = backendQuizId.Value;
                quiz.EmailsPublicationWebJson = emailsPublicationWebJson ?? string.Empty;
                quiz.Statut = statut;
                await _db.SaveChangesAsync(cancellationToken);
            }, "Mise à jour publication web locale");

        /// <summary>Met à jour un quiz existant : métadonnées et remplace toutes les questions liées.</summary>
        /// <param name="emailsPublicationWebJson">Si non null, remplace la liste JSON des emails publication web.</param>
        public Task MettreAJourContenuAsync(
            int quizId,
            string titre,
            string difficulte,
            string coursTitre,
            string statut,
            IReadOnlyList<QuestionLocale> nouvellesQuestions,
            string? emailsPublicationWebJson = null,
            CancellationToken cancellationToken = default) =>
            InvokeDbAsync(async () =>
            {
                var quiz = await _db.Quiz
                    .Include(q => q.Questions)
                    .FirstOrDefaultAsync(q => q.Id == quizId, cancellationToken);
                if (quiz == null)
                    throw new InvalidOperationException("Quiz introuvable ou déjà supprimé.");

                quiz.Titre = titre;
                quiz.Difficulte = difficulte;
                quiz.CoursSourceTitre = coursTitre ?? string.Empty;
                quiz.Statut = statut;
                quiz.NombreQuestions = nouvellesQuestions.Count;

                if (emailsPublicationWebJson != null)
                    quiz.EmailsPublicationWebJson = emailsPublicationWebJson;

                var anciennes = quiz.Questions.ToList();
                if (anciennes.Count > 0)
                    _db.Questions.RemoveRange(anciennes);

                foreach (var q in nouvellesQuestions)
                {
                    q.Id = 0;
                    q.QuizLocalId = quizId;
                    _db.Questions.Add(q);
                }

                await _db.SaveChangesAsync(cancellationToken);
            }, "Mise à jour du contenu du quiz");
    }
}
