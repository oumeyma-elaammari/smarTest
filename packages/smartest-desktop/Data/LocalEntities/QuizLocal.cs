using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace smartest_desktop.Data.LocalEntities
{
    /// <summary>
    /// Quiz sauvegardé en base locale après validation par le professeur.
    /// Statut : "Brouillon" → "Validé" → "Publié"
    /// </summary>
    public class QuizLocal
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Titre { get; set; } = string.Empty;

        /// <summary>"Facile", "Moyen" ou "Difficile"</summary>
        public string Difficulte { get; set; } = "Moyen";

        public DateTime DateCreation { get; set; } = DateTime.Now;

        public int NombreQuestions { get; set; }

        /// <summary>FK vers CoursLocal (nullable — le cours peut avoir été supprimé)</summary>
        public int? CoursSourceId { get; set; }

        /// <summary>Titre du cours dénormalisé pour affichage rapide</summary>
        public string CoursSourceTitre { get; set; } = string.Empty;

        /// <summary>"Brouillon" | "Validé" | "Publié"</summary>
        public string Statut { get; set; } = "Brouillon";

        public List<QuestionLocale> Questions { get; set; } = new();

        public string Description { get; set; } = string.Empty;
        //public CoursLocal? Cours { get; set; }


        public List<CoursLocal> Cours { get; set; } = new();  // Correction : relation Many-to-Many

    }

}