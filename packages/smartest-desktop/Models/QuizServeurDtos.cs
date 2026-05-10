using Newtonsoft.Json;

namespace smartest_desktop.Models
{
    public class ProfesseurProfilResponse
    {
        [JsonProperty("id")]
        public long Id { get; set; }
    }

    public class QuizServeurResponse
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("professeurId")]
        public long? ProfesseurId { get; set; }
    }

    /// <summary>Résumé quiz professeur (GET /api/professeur/mes-quiz).</summary>
    public sealed class QuizProfesseurListeItem
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("titre")]
        public string? Titre { get; set; }

        [JsonProperty("nombreQuestions")]
        public int? NombreQuestions { get; set; }
    }
}
