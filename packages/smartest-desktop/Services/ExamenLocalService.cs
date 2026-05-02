using smartest_desktop.Data;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace smartest_desktop.Services
{
    public class ExamenLocalService
    {
        private readonly LocalDbContext _db;

        public ExamenLocalService(LocalDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Persiste l'examen et ses questions en base locale.
        /// Retourne l'Id de l'examen créé.
        /// </summary>
        public async Task<int> SauvegarderAsync(
            ExamenLocal examen,
            List<QuestionExamen> questions,
            string coursTitre = "")
        {
            examen.Cours.Clear();
            if (!string.IsNullOrWhiteSpace(coursTitre))
            {
                var titreTrim = coursTitre.Trim();
                var cours = await _db.Cours.FirstOrDefaultAsync(c =>
                    c.Titre != null &&
                    string.Equals(c.Titre.Trim(), titreTrim, StringComparison.OrdinalIgnoreCase));
                if (cours != null)
                    examen.Cours.Add(cours);
            }

            _db.Examens.Add(examen);
            await _db.SaveChangesAsync();

            await AjouterQuestionsAsync(examen.Id, questions);
            return examen.Id;
        }

        /// <summary>Remplace le contenu (métadonnées + questions) d'un examen déjà enregistré.</summary>
        public async Task MettreAJourContenuAsync(int examenId, string titre, int duree, List<QuestionExamen> questions)
        {
            var examen = await _db.Examens
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.Id == examenId);
            if (examen == null)
                throw new InvalidOperationException("Examen introuvable ou déjà supprimé.");

            examen.Titre = titre;
            examen.Duree = duree;

            var anciennes = examen.Questions.ToList();
            if (anciennes.Count > 0)
                _db.Questions.RemoveRange(anciennes);

            await AjouterQuestionsAsync(examenId, questions);
        }

        private async Task AjouterQuestionsAsync(int examenId, List<QuestionExamen> questions)
        {
            int numero = 1;
            foreach (var q in questions)
            {
                _db.Questions.Add(CreerQuestionLocale(q, examenId, numero++));
            }

            await _db.SaveChangesAsync();
        }

        private static QuestionLocale CreerQuestionLocale(QuestionExamen q, int examenId, int numero)
        {
            return new QuestionLocale
            {
                ExamenLocalId = examenId,
                Numero = numero,
                Enonce = q.Enonce,
                Type = q.Type,
                Difficulte = q.Difficulte,
                Explication = q.Explication,
                OptionA = q.OptionA,
                OptionB = q.OptionB,
                OptionC = q.OptionC,
                OptionD = q.OptionD,
                ReponseCorrecte = q.ReponseCorrecte,
                ReponseModele = q.ReponseModele,
                ReponsesCorrectesJson = q.IsCheckbox
                    ? JsonSerializer.Serialize(q.ReponsesCorrectes)
                    : string.Empty,
                ImageBase64 = q.ImageBase64,
                ImageType = q.ImageType,
                ImageNom = q.ImageNom,
            };
        }

        public async Task<List<ExamenLocal>> ChargerTousAsync()
        {
            return await Task.Run(() =>
                _db.Examens
                   .Include(e => e.Cours)
                   .Include(e => e.Questions)
                   .OrderByDescending(e => e.DateCreation)
                   .ToList());
        }
    }
}
