using Microsoft.EntityFrameworkCore;
using smartest_desktop.Data;
using smartest_desktop.Data.LocalEntities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace smartest_desktop.Services
{
    public class LocalCoursService
    {
        private readonly LocalDbContext _db;

        public LocalCoursService(LocalDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public LocalCoursService()
            : this(App.LocalDb)
        {
        }

        private static T InvokeDb<T>(Func<T> action, string contexte)
        {
            try
            {
                return action();
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

        private static void InvokeDb(Action action, string contexte) =>
            InvokeDb(() =>
            {
                action();
                return 0;
            }, contexte);

        /// <summary>
        /// Récupère tous les cours AVEC leurs questions.
        /// Utilisé quand on a besoin du détail complet.
        /// </summary>
        public List<CoursLocal> GetAll() =>
            InvokeDb(() => _db.Cours
                .Include(c => c.Questions)
                .OrderByDescending(c => c.DateImport)
                .ToList(), "Lecture des cours");

        /// <summary>
        /// Récupère tous les cours SANS charger les questions.
        /// Plus rapide — utilisé pour afficher la liste dans CoursWindow.
        /// Le nombre de questions est calculé séparément via NombreQuestions().
        /// </summary>
        public List<CoursLocal> GetAllSansQuestions() =>
            InvokeDb(() => _db.Cours
                .OrderByDescending(c => c.DateImport)
                .ToList(), "Liste des cours");

        /// <summary>
        /// Récupère un cours par son ID avec ses questions ET réponses.
        /// Utilisé pour visualiser le contenu complet.
        /// </summary>
        public CoursLocal? GetById(int id) =>
            InvokeDb(() => _db.Cours
                .Include(c => c.Questions)
                .ThenInclude(q => q.Reponses)
                .FirstOrDefault(c => c.Id == id), "Chargement du cours");

        /// <summary>
        /// Recherche des cours par titre (insensible à la casse).
        /// </summary>
        public List<CoursLocal> Rechercher(string terme)
        {
            if (string.IsNullOrWhiteSpace(terme))
                return GetAllSansQuestions();

            return InvokeDb(() => _db.Cours
                .Where(c => c.Titre.ToLower().Contains(terme.ToLower()))
                .OrderByDescending(c => c.DateImport)
                .ToList(), "Recherche de cours");
        }

        /// <summary>
        /// Retourne les cours qui ont au moins 1 question QCM ou VF.
        /// (Quiz = QCM/VF uniquement — pas de rédaction)
        /// </summary>
        public List<CoursLocal> GetCoursDisponiblesPourQuiz() =>
            InvokeDb(() => _db.Cours
                .Include(c => c.Questions)
                .Where(c => c.Questions.Any(q =>
                    q.Type == "QCM" || q.Type == "VF"))
                .OrderBy(c => c.Titre)
                .ToList(), "Cours disponibles pour quiz");

        /// <summary>
        /// Retourne les cours qui ont au moins 1 question (tous types).
        /// Utilisé dans ExamenWindow pour sélectionner les cours disponibles.
        /// (Examen = QCM + VF + REDACTION + DEVELOPPEMENT)
        /// </summary>
        public List<CoursLocal> GetCoursDisponiblesPourExamen() =>
            InvokeDb(() => _db.Cours
                .Include(c => c.Questions)
                .Where(c => c.Questions.Any())
                .OrderBy(c => c.Titre)
                .ToList(), "Cours disponibles pour examen");

        /// <summary>
        /// Ajoute un nouveau cours dans SQLite.
        /// Retourne le cours avec son ID généré.
        /// </summary>
        public CoursLocal Ajouter(string titre, string contenu, string cheminFichier = "")
        {
            if (string.IsNullOrWhiteSpace(titre))
                throw new ArgumentException("Le titre du cours est obligatoire.");

            if (string.IsNullOrWhiteSpace(contenu))
                throw new ArgumentException("Le contenu du cours est vide.");

            var cours = new CoursLocal
            {
                Titre = titre.Trim(),
                Contenu = contenu,
                CheminFichier = cheminFichier,
                DateImport = DateTime.Now
            };

            return InvokeDb(() =>
            {
                _db.Cours.Add(cours);
                _db.SaveChanges();
                return cours;
            }, "Ajout du cours");
        }

        /// <summary>
        /// Met à jour le titre et/ou le contenu d'un cours.
        /// </summary>
        public CoursLocal Modifier(int id, string nouveauTitre, string? nouveauContenu = null)
        {
            if (string.IsNullOrWhiteSpace(nouveauTitre))
                throw new ArgumentException("Le titre est obligatoire.");

            return InvokeDb(() =>
            {
                var cours = _db.Cours.Find(id)
                    ?? throw new KeyNotFoundException($"Cours introuvable (ID={id}).");

                cours.Titre = nouveauTitre.Trim();

                if (nouveauContenu != null)
                    cours.Contenu = nouveauContenu;

                _db.SaveChanges();
                return cours;
            }, "Mise à jour du cours");
        }

        /// <summary>
        /// Supprime un cours et toutes ses questions/réponses (cascade).
        /// </summary>
        public void Supprimer(int id) =>
            InvokeDb(() =>
            {
                var cours = _db.Cours
                        .Include(c => c.Questions)
                        .ThenInclude(q => q.Reponses)
                        .FirstOrDefault(c => c.Id == id)
                    ?? throw new KeyNotFoundException($"Cours introuvable (ID={id}).");

                _db.Cours.Remove(cours);
                _db.SaveChanges();
            }, "Suppression du cours");

        /// <summary>Nombre total de cours.</summary>
        public int Count() => InvokeDb(() => _db.Cours.Count(), "Comptage des cours");

        /// <summary>Vérifie si un cours avec ce titre existe déjà.</summary>
        public bool ExisteDejaParTitre(string titre) =>
            InvokeDb(() => _db.Cours.Any(c =>
                c.Titre.ToLower() == titre.ToLower().Trim()), "Vérification de titre");

        /// <summary>Nombre de questions associées à un cours.</summary>
        public int NombreQuestions(int coursId) =>
            InvokeDb(() => _db.Questions.Count(q => q.CoursId == coursId),
                "Nombre de questions du cours");

        /// <summary>
        /// Nombre de questions par type pour un cours.
        /// ex: { "QCM": 5, "VF": 3, "REDACTION": 2 }
        /// </summary>
        public Dictionary<string, int> NombreQuestionsParType(int coursId) =>
            InvokeDb(() => _db.Questions
                    .Where(q => q.CoursId == coursId)
                    .GroupBy(q => q.Type)
                    .ToDictionary(g => g.Key, g => g.Count()),
                "Répartition des types de questions");
    }
}
