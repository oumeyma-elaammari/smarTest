using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using smartest_desktop.Exceptions;
using smartest_desktop.Helpers;
using smartest_desktop.Models;

namespace smartest_desktop.Services
{
    /// <summary>GET /api/statistiques/quiz|examen/{id} — visible uniquement par le prof propriétaire (JWT).</summary>
    public sealed class QuizStatistiquesApiService
    {
        private readonly Func<string, HttpClient>? _createClientOverride;
        private readonly string _baseUrl;

        /// <remarks>Injection optionnelle pour les tests (<paramref name="createClient"/> retourne un client déjà configuré).</remarks>
        public QuizStatistiquesApiService(Func<string, HttpClient>? createClient = null, string? baseUrl = null)
        {
            _createClientOverride = createClient;
            _baseUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? SmartestBackendBaseUrl.Resolve()
                : baseUrl.TrimEnd('/');
        }

        private HttpClient CreateHttp(string bearerToken)
        {
            if (_createClientOverride != null)
                return _createClientOverride(bearerToken);

            var client = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken);
            return client;
        }

        /// <returns>Données ou message d’erreur.</returns>
        public async Task<(StatistiquesQuizResponseDto? Data, string? Error)> GetStatistiquesQuizAsync(
            string bearerToken,
            long backendQuizId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(bearerToken))
                return (null, "Non connecté au serveur.");

            try
            {
                using var http = CreateHttp(bearerToken);
                var response = await http.GetAsync(
                    $"/api/statistiques/quiz/{backendQuizId}",
                    cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var ex = SmartestApiException.FromHttpFailure(response.StatusCode, body, "Statistiques");
                    return (null, UserErrorMessage.FromText(ex.Message, "Impossible de charger les statistiques pour le moment."));
                }

                try
                {
                    var dto = JsonConvert.DeserializeObject<StatistiquesQuizResponseDto>(body);
                    if (dto == null)
                        return (null, "Statistiques : réponse vide ou illisible.");
                    return (dto, null);
                }
                catch (JsonException)
                {
                    return (null, "Les statistiques recues sont invalides. Reessayez plus tard.");
                }
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                var net = SmartestNetworkException.ServerUnreachable(ex);
                return (null, net.Message);
            }
            catch (HttpRequestException ex)
            {
                var net = SmartestNetworkException.ServerUnreachable(ex);
                return (null, net.Message);
            }
            catch (InvalidOperationException)
            {
                return (null, "Impossible de charger les statistiques pour le moment.");
            }
        }

        public async Task<(StatistiquesQuizResponseDto? Data, string? Error)> GetStatistiquesExamenAsync(
            string bearerToken,
            long backendExamenId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(bearerToken))
                return (null, "Non connecté au serveur.");

            try
            {
                using var http = CreateHttp(bearerToken);
                var response = await http.GetAsync(
                    $"/api/statistiques/examen/{backendExamenId}",
                    cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var ex = SmartestApiException.FromHttpFailure(response.StatusCode, body, "Statistiques examen");
                    return (null, UserErrorMessage.FromText(ex.Message, "Impossible de charger les statistiques de l'examen pour le moment."));
                }

                try
                {
                    var dto = JsonConvert.DeserializeObject<StatistiquesQuizResponseDto>(body);
                    if (dto == null)
                        return (null, "Statistiques : réponse vide ou illisible.");
                    return (dto, null);
                }
                catch (JsonException)
                {
                    return (null, "Les statistiques recues sont invalides. Reessayez plus tard.");
                }
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                var net = SmartestNetworkException.ServerUnreachable(ex);
                return (null, net.Message);
            }
            catch (HttpRequestException ex)
            {
                var net = SmartestNetworkException.ServerUnreachable(ex);
                return (null, net.Message);
            }
            catch (InvalidOperationException)
            {
                return (null, "Impossible de charger les statistiques pour le moment.");
            }
        }
    }
}
