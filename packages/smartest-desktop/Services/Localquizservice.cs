using smartest_desktop.Data;
using smartest_desktop.Data.LocalEntities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
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
    }
}