using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace smartest_desktop.Data.LocalEntities
{
    [Table("quiz_local")]
    public class QuizLocal
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Titre { get; set; } = string.Empty;

        public int Duree { get; set; } = 30;

        public string Description { get; set; } = string.Empty;

        /// <summary>BROUILLON | PUBLIE</summary>
        [MaxLength(20)]
        public string Statut { get; set; } = "BROUILLON";

        public DateTime DateCreation { get; set; } = DateTime.Now;

        /// <summary>
        /// ID du quiz côté backend après publication.
        /// null = pas encore publié.
        /// </summary>
        public long? BackendId { get; set; }

        // ── Relation Many-to-Many avec CoursLocal ────────────────
        // Un quiz peut couvrir PLUSIEURS cours
        // EF Core crée automatiquement la table :
        //   quiz_local_cours (QuizLocalId, CoursLocalId)
        public List<CoursLocal> Cours { get; set; } = new();

        public override string ToString() => Titre;
    }
}