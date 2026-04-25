using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace smartest_desktop.Data.LocalEntities
{
    [Table("cours_local")]
    public class CoursLocal
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Titre { get; set; } = string.Empty;

        public string Contenu { get; set; } = string.Empty;

        public string CheminFichier { get; set; } = string.Empty;

        public DateTime DateImport { get; set; } = DateTime.Now;

        // ═══════════════════════════════════════════════════════
        //  PROPRIÉTÉS AJOUTÉES pour corriger les erreurs
        // ═══════════════════════════════════════════════════════

        [MaxLength(100)]
        public string Categorie { get; set; } = string.Empty;

        [MaxLength(50)]
        public string TypeFichier { get; set; } = string.Empty;

        public long TailleFichier { get; set; } = 0;

        public string NomFichier { get; set; } = string.Empty;



        [MaxLength(50)]
        public string Statut { get; set; } = "Actif";

        // ── Navigation ──────────────────────────────────────────
        public List<QuestionLocale> Questions { get; set; } = new();

        public override string ToString() => Titre;
    }
}