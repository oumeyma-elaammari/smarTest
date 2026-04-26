using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace smartest_desktop.Data.LocalEntities
{
    public class ReponseLocale
    {
        [Key]
        public int Id { get; set; }

        public int QuestionId { get; set; }   // Correction : remplacer QuestionLocaleId par QuestionId

        [Required]
        public string Contenu { get; set; } = string.Empty;


        public bool EstCorrecte { get; set; }

        [ForeignKey(nameof(QuestionId))]
        public QuestionLocale? Question { get; set; }



    }
}
