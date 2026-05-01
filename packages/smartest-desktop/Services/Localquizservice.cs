using smartest_desktop.Data;
using smartest_desktop.Data.LocalEntities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace smartest_desktop.Services
{
    public class LocalQuizService
    {
        private readonly LocalDbContext _db;

        public LocalQuizService(LocalDbContext db)
        {
            _db = db;
        }

        public async Task<List<QuizLocal>> GetAllAsync()
        {
            return await _db.Quiz
                .OrderByDescending(q => q.DateCreation)
                .ToListAsync();
        }

        public async Task<QuizLocal?> GetByIdAsync(int id)
        {
            return await _db.Quiz
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<QuizLocal> AjouterAsync(QuizLocal quiz)
        {
            _db.Quiz.Add(quiz);
            await _db.SaveChangesAsync();
            return quiz;
        }

        public async Task ModifierAsync(QuizLocal quiz)
        {
            _db.Quiz.Update(quiz);
            await _db.SaveChangesAsync();
        }

        public async Task SupprimerAsync(int id)
        {
            var quiz = await _db.Quiz.FindAsync(id);
            if (quiz != null)
            {
                _db.Quiz.Remove(quiz);
                await _db.SaveChangesAsync();
            }
        }

        public async Task ChangerStatutAsync(int id, string statut)
        {
            var quiz = await _db.Quiz.FindAsync(id);
            if (quiz != null)
            {
                quiz.Statut = statut;
                await _db.SaveChangesAsync();
            }
        }

        /// <summary>Met à jour un quiz existant : métadonnées et remplace toutes les questions liées.</summary>
        public async Task MettreAJourContenuAsync(
            int quizId,
            string titre,
            string difficulte,
            string coursTitre,
            string statut,
            IReadOnlyList<QuestionLocale> nouvellesQuestions)
        {
            var quiz = await _db.Quiz
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == quizId);
            if (quiz == null)
                throw new InvalidOperationException("Quiz introuvable ou déjà supprimé.");

            quiz.Titre = titre;
            quiz.Difficulte = difficulte;
            quiz.CoursSourceTitre = coursTitre ?? string.Empty;
            quiz.Statut = statut;
            quiz.NombreQuestions = nouvellesQuestions.Count;

            var anciennes = quiz.Questions.ToList();
            if (anciennes.Count > 0)
                _db.Questions.RemoveRange(anciennes);

            foreach (var q in nouvellesQuestions)
            {
                q.Id = 0;
                q.QuizLocalId = quizId;
                _db.Questions.Add(q);
            }

            await _db.SaveChangesAsync();
        }
    }
}