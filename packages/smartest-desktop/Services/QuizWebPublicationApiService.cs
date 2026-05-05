using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using smartest_desktop.Models;
using smartest_desktop.Data.LocalEntities;

namespace smartest_desktop.Services
{
    /// <summary>Appels API pour la publication web des quiz (liste d'emails sur le serveur).</summary>
    public sealed class QuizWebPublicationApiService
    {
        private const string BaseUrl = "http://localhost:8081";

        private static HttpClient CreateClient(string bearerToken)
        {
            var client = new HttpClient { BaseAddress = new Uri(BaseUrl) };
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken);
            return client;
        }

        private static StringContent ToJson(object data) =>
            new StringContent(
                JsonConvert.SerializeObject(data),
                Encoding.UTF8,
                "application/json");

        public async Task<(long id, string? error)> GetProfesseurIdAsync(string bearerToken)
        {
            using var http = CreateClient(bearerToken);
            var response = await http.GetAsync("/api/professeur/profil");
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                return (0, $"Profil professeur : {(int)response.StatusCode} {body}");

            var dto = JsonConvert.DeserializeObject<ProfesseurProfilResponse>(body);
            if (dto == null || dto.Id <= 0)
                return (0, "Réponse profil invalide.");
            return (dto.Id, null);
        }

        public async Task<(long quizId, string? error)> CreateQuizAsync(
            string bearerToken,
            string titre,
            int dureeMinutes,
            long professeurId)
        {
            using var http = CreateClient(bearerToken);
            var payload = new { titre, duree = dureeMinutes, professeurId };
            var response = await http.PostAsync("/api/quizs", ToJson(payload));
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                return (0, $"Création quiz : {(int)response.StatusCode} {body}");

            var dto = JsonConvert.DeserializeObject<QuizServeurResponse>(body);
            if (dto == null || dto.Id <= 0)
                return (0, "Réponse création quiz invalide.");
            return (dto.Id, null);
        }

        public async Task<string?> PostPublicationWebAsync(
            string bearerToken,
            long backendQuizId,
            IReadOnlyList<string> emails,
            IReadOnlyList<QuestionLocale>? questions = null)
        {
            using var http = CreateClient(bearerToken);
            var payload = new
            {
                emails,
                questions = (questions ?? new List<QuestionLocale>())
                    .Select(q => new
                    {
                        enonce = q.Enonce,
                        optionA = q.OptionA,
                        optionB = q.OptionB,
                        optionC = q.OptionC,
                        optionD = q.OptionD,
                        reponseCorrecte = q.ReponseCorrecte,
                        explication = q.Explication,
                        difficulte = q.Difficulte
                    })
                    .ToList()
            };
            var response = await http.PostAsync(
                $"/api/quizs/{backendQuizId}/publication-web",
                ToJson(payload));
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                return null;

            return $"Publication web : {(int)response.StatusCode}\n{body}";
        }

        /// <summary>Supprime le quiz sur le backend (professeur propriétaire uniquement).</summary>
        public async Task<string?> DeleteQuizAsync(string bearerToken, long backendQuizId)
        {
            using var http = CreateClient(bearerToken);
            var response = await http.DeleteAsync($"/api/quizs/{backendQuizId}");
            var body = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                return null;

            return $"Suppression serveur : {(int)response.StatusCode}\n{body}";
        }
    }
}
