using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace smartest_desktop.Data.LocalEntities
{
	/// <summary>
	/// Question QCM appartenant à un QuizLocal.
	/// </summary>
	public class QuestionLocale
	{
		[Key]
		public int Id { get; set; }

		public int QuizLocalId { get; set; }

		public int Numero { get; set; }

		[Required]
		public string Enonce { get; set; } = string.Empty;

		// Options de réponse
		public string OptionA { get; set; } = string.Empty;
		public string OptionB { get; set; } = string.Empty;
		public string OptionC { get; set; } = string.Empty;
		public string OptionD { get; set; } = string.Empty;

		/// <summary>"A", "B", "C" ou "D"</summary>
		public string ReponseCorrecte { get; set; } = string.Empty;

		public string Explication { get; set; } = string.Empty;

		// Propriétés supplémentaires attendues par ton code
		public List<ReponseLocale> Reponses { get; set; } = new();


		public int? CoursId { get; set; }
		public string Type { get; set; } = string.Empty;
		public string Difficulte { get; set; } = "Moyen";
		public string ReponseModele { get; set; } = string.Empty;
		public string ImageBase64 { get; set; } = string.Empty;
		public string ImageType { get; set; } = string.Empty;
		public string ImageNom { get; set; } = string.Empty;

		// Navigation properties
		[ForeignKey(nameof(QuizLocalId))]
		public QuizLocal? Quiz { get; set; }

		public CoursLocal? Cours { get; set; }
	}
}
