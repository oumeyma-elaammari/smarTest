using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace smartest_desktop.Services
{
    public class OllamaService
    {
        private readonly HttpClient _http = new HttpClient();

        public async Task<string> GenererQCM(string cours, int nbQuestions, string difficulte)
        {
            var prompt = $@"
Tu es un professeur expert.

Génère {nbQuestions} questions QCM basées sur ce cours.

Difficulté : {difficulte}

Format STRICT JSON :
[
  {{
    ""question"": ""..."",
    ""choix"": [""A"", ""B"", ""C"", ""D""],
    ""reponse"": ""A""
  }}
]

Cours :
{cours}
";

            var request = new
            {
                model = "mistral",
                prompt = prompt,
                stream = false
            };

            var json = JsonSerializer.Serialize(request);

            var response = await _http.PostAsync(
                "http://localhost:11434/api/generate",
                new StringContent(json, Encoding.UTF8, "application/json")
            );

            var result = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(result);
            return doc.RootElement.GetProperty("response").GetString();
        }
    }
}