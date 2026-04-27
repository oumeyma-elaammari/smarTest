using smartest_desktop.Data;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.ViewModels;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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
            _db.Examens.Add(examen);
            await _db.SaveChangesAsync();

            // Persister les questions liées à l'examen
            int numero = 1;
            foreach (var q in questions)
            {
                var entity = new QuestionLocale
                {
                    ExamenLocalId        = examen.Id,
                    Numero               = numero++,
                    Enonce               = q.Enonce,
                    Type                 = q.Type,
                    Difficulte           = q.Difficulte,
                    Explication          = q.Explication,
                    OptionA              = q.OptionA,
                    OptionB              = q.OptionB,
                    OptionC              = q.OptionC,
                    OptionD              = q.OptionD,
                    ReponseCorrecte      = q.ReponseCorrecte,
                    ReponseModele        = q.ReponseModele,
                    ReponsesCorrectesJson = q.IsCheckbox
                        ? JsonSerializer.Serialize(q.ReponsesCorrectes)
                        : string.Empty,
                    ImageBase64 = q.ImageBase64,
                    ImageType   = q.ImageType,
                    ImageNom    = q.ImageNom,
                };
                _db.Questions.Add(entity);
            }

            await _db.SaveChangesAsync();
            return examen.Id;
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
