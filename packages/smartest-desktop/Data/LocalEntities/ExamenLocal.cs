using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace smartest_desktop.Data.LocalEntities
{
    [Table("examen_local")]
    public class ExamenLocal
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Titre { get; set; } = string.Empty;

        public int Duree { get; set; } = 90;

        public string Description { get; set; } = string.Empty;

        public DateTime? DatePrevue { get; set; }

        /// <summary>BROUILLON | PUBLIE | EN_COURS | TERMINE</summary>
        [MaxLength(20)]
        public string Statut { get; set; } = "BROUILLON";

        public DateTime DateCreation { get; set; } = DateTime.Now;

        /// <summary>
        /// ID de l'examen côté backend après publication.
        /// Les questions QCM (énoncé + options) sont synchronisées avec le serveur lors du « Publier sur le web ».
        /// null = pas encore publié.
        /// </summary>
        public long? BackendId { get; set; }

        /// <summary>JSON : tableau d'emails autorisés pour la publication web (comme pour les quiz).</summary>
        public string EmailsPublicationWebJson { get; set; }

        // ── Relation Many-to-Many avec CoursLocal ────────────────
        public List<CoursLocal> Cours { get; set; } = new();

        // ── Relation One-to-Many avec QuestionLocale ─────────────
        public List<QuestionLocale> Questions { get; set; } = new();

        public override string ToString() => Titre;
    }
}