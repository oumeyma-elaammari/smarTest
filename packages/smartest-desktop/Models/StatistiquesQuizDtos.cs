using System.Collections.Generic;
using Newtonsoft.Json;

namespace smartest_desktop.Models
{
    public class StatistiquesQuizResponseDto
    {
        [JsonProperty("quizId")]
        public long QuizId { get; set; }

        [JsonProperty("quizTitre")]
        public string? QuizTitre { get; set; }

        [JsonProperty("nombreParticipants")]
        public int NombreParticipants { get; set; }

        [JsonProperty("moyenneGenerale")]
        public double MoyenneGenerale { get; set; }

        [JsonProperty("tauxReussiteGlobal")]
        public double TauxReussiteGlobal { get; set; }

        [JsonProperty("moyenneNoteSur20")]
        public double? MoyenneNoteSur20 { get; set; }

        [JsonProperty("repartitionParTranche")]
        public List<int>? RepartitionParTranche { get; set; }

        [JsonProperty("questionsAlerteCount")]
        public long QuestionsAlerteCount { get; set; }

        [JsonProperty("statistiquesParQuestion")]
        public List<StatistiqueQuestionResponseDto>? StatistiquesParQuestion { get; set; }
    }

    public class StatistiqueQuestionResponseDto
    {
        [JsonProperty("questionId")]
        public long QuestionId { get; set; }

        [JsonProperty("questionEnonce")]
        public string? QuestionEnonce { get; set; }

        [JsonProperty("pourcentageReussite")]
        public double PourcentageReussite { get; set; }

        [JsonProperty("pourcentageEchec")]
        public double PourcentageEchec { get; set; }

        [JsonProperty("nombreReponses")]
        public int NombreReponses { get; set; }

        [JsonProperty("alerteEchec")]
        public bool AlerteEchec { get; set; }
    }
}
