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
    }
}
