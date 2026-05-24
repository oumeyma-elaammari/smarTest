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

        /// <summary>Niveau(x) : « Moyen », « Facile + Difficile », etc.</summary>
        public string Difficulte { get; set; } = "Moyen";

        public DateTime DateCreation { get; set; } = DateTime.Now;

        public int NombreQuestions { get; set; }

        /// <summary>FK vers CoursLocal (nullable — le cours peut avoir été supprimé)</summary>
        public int? CoursSourceId { get; set; }

        /// <summary>Titre du cours dénormalisé pour affichage rapide</summary>
        public string CoursSourceTitre { get; set; } = string.Empty;

        /// <summary>"Brouillon" | "Validé" | "Publié"</summary>
        public string Statut { get; set; } = "Brouillon";

        /// <summary>
        /// Ancienne colonne unique : préférer <see cref="BackendQuizIdPublicationWeb"/> / <see cref="BackendQuizIdQr"/>.
        /// Conservée pour compatibilité ; reflète typiquement l’id publication web lorsque défini.
        /// </summary>
        public long? BackendQuizId { get; set; }

        /// <summary>Quiz MySQL utilisé pour la publication web (emails étudiants, passage web).</summary>
        public long? BackendQuizIdPublicationWeb { get; set; }

        /// <summary>Quiz MySQL « miroir » réservé au flux QR (contenu synchro, suppression indépendante du publié).</summary>
        public long? BackendQuizIdQr { get; set; }

        /// <summary>Dernier token UUID de session éphémère QR-live (pour DELETE avant nouvelle session).</summary>
        public string QrLiveSessionToken { get; set; }

        /// <summary>
        /// Indique que le quiz a probablement été créé ou utilisé sur l’API (QR, création pré-publication…),
        /// même si <see cref="BackendQuizId"/> a été perdu localement — pour éviter suppressions locales orphelines côté MySQL.
        /// </summary>
        public DateTime? ServeurOuQrToucheUtc { get; set; }

        /// <summary>JSON : tableau d'emails (minuscules) autorisés pour la publication web.</summary>
        public string EmailsPublicationWebJson { get; set; }

        public List<QuestionLocale> Questions { get; set; } = new();

        public string Description { get; set; } = string.Empty;
        //public CoursLocal? Cours { get; set; }


        public List<CoursLocal> Cours { get; set; } = new();  // Correction : relation Many-to-Many

    }

}