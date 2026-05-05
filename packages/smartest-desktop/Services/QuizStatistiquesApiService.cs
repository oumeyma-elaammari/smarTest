using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using smartest_desktop.Models;

namespace smartest_desktop.Services
{
    /// <summary>GET /api/statistiques/quiz/{id} — visible uniquement par le prof propriétaire (JWT).</summary>
    public sealed class QuizStatistiquesApiService
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

        /// <returns>Données ou message d’erreur.</returns>
        public async Task<(StatistiquesQuizResponseDto? Data, string? Error)> GetStatistiquesQuizAsync(
            string bearerToken,
            long backendQuizId)
        {
            if (string.IsNullOrWhiteSpace(bearerToken))
                return (null, "Non connecté au serveur.");

            using var http = CreateClient(bearerToken);
            var response = await http.GetAsync($"/api/statistiques/quiz/{backendQuizId}");
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                var msg = TryReadMessage(body) ?? $"{(int)response.StatusCode}";
                return (null, $"Statistiques : {msg}");
            }

            try
            {
                var dto = JsonConvert.DeserializeObject<StatistiquesQuizResponseDto>(body);
                return (dto, null);
            }
            catch (Exception ex)
            {
                return (null, $"Lecture des statistiques : {ex.Message}");
            }
        }

        private static string? TryReadMessage(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            try
            {
                dynamic? o = JsonConvert.DeserializeObject(json);
                if (o?.message != null)
                    return (string)o.message;
            }
            catch { /* ignore */ }

            return json.Length > 200 ? json.Substring(0, 200) + "…" : json;
        }
    }
}
