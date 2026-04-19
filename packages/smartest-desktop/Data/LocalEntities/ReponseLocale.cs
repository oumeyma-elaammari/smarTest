using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace smartest_desktop.Data.LocalEntities
{
    [Table("reponse_locale")]
    public class ReponseLocale
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string Contenu { get; set; } = string.Empty;

        public bool Correcte { get; set; } = false;

        // ── Clé étrangère ────────────────────────────────────────
        public int QuestionId { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public QuestionLocale? Question { get; set; }

        public override string ToString() => Contenu;
    }
}