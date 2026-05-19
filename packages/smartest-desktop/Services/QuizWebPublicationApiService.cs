using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using smartest_desktop.Models;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Exceptions;
using smartest_desktop.Helpers;

namespace smartest_desktop.Services
{
    /// <summary>Appels API pour la publication web des quiz (liste d'emails sur le serveur).</summary>
    public sealed class QuizWebPublicationApiService
    {
        private readonly Func<string, HttpClient>? _createClientOverride;
        private readonly string _baseUrl;

        /// <remarks>
        /// Sans <paramref name="baseUrl"/>, utilise <see cref="SmartestBackendBaseUrl.Resolve"/> (vars d’environnement
        /// SMARTEST_BACKEND_URL / SMARTEST_API_URL, défaut développement localhost:8081).
        /// </remarks>
        public QuizWebPublicationApiService(string? baseUrl = null, Func<string, HttpClient>? createClientOverride = null)
        {
            _baseUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? SmartestBackendBaseUrl.Resolve()
                : baseUrl.TrimEnd('/');
            _createClientOverride = createClientOverride;
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

        private static StringContent ToJson(object data) =>
            new StringContent(
                JsonConvert.SerializeObject(data),
                Encoding.UTF8,
                "application/json");

        public async Task<long> GetProfesseurIdAsync(string bearerToken, CancellationToken cancellationToken = default)
        {
            try
            {
                using var http = CreateHttp(bearerToken);
                var response = await http.GetAsync("/api/professeur/profil", cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(
                        response.StatusCode, body, "Profil professeur");

                ProfesseurProfilResponse dto;
                try
                {
                    dto = JsonConvert.DeserializeObject<ProfesseurProfilResponse>(body)
                         ?? throw new JsonException();
                }
                catch (JsonException jex)
                {
                    throw new SmartestApiException(
                        response.StatusCode,
                        body,
                        "Profil professeur : réponse serveur invalide.",
                        validationErrors: null,
                        innerException: jex);
                }

                if (dto.Id <= 0)
                    throw new SmartestApiException(
                        response.StatusCode,
                        body,
                        "Profil professeur : réponse serveur invalide.");

                return dto.Id;
            }
            catch (SmartestApiException)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
        }

        /// <summary>
        /// Lecture du quiz côté serveur pour connaître le professeur propriétaire (cohérence base locale).
        /// </summary>
        public async Task<(bool Found, long ProfesseurId)> TryGetQuizAuteurAsync(
            long quizId,
            string bearerToken,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var http = CreateHttp(bearerToken);
                var response = await http.GetAsync($"/api/quizs/{quizId}", cancellationToken);
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return (false, 0);
                if (!response.IsSuccessStatusCode)
                    return (false, 0);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var dto = JsonConvert.DeserializeObject<QuizServeurResponse>(body);
                if (dto?.ProfesseurId is long pid && pid > 0)
                    return (true, pid);
                return (false, 0);
            }
            catch (HttpRequestException)
            {
                return (false, 0);
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                return (false, 0);
            }
        }

        public async Task<long> CreateQuizAsync(
            string bearerToken,
            string titre,
            long professeurId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var http = CreateHttp(bearerToken);
                var payload = new { titre, professeurId };
                var response = await http.PostAsync("/api/quizs", ToJson(payload), cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(
                        response.StatusCode, body, "Création quiz");

                QuizServeurResponse dto;
                try
                {
                    dto = JsonConvert.DeserializeObject<QuizServeurResponse>(body)
                         ?? throw new JsonException();
                }
                catch (JsonException jex)
                {
                    throw new SmartestApiException(
                        response.StatusCode,
                        body,
                        "Création quiz : réponse serveur invalide.",
                        null,
                        jex);
                }

                if (dto.Id <= 0)
                    throw new SmartestApiException(
                        response.StatusCode,
                        body,
                        "Création quiz : réponse serveur invalide.");

                return dto.Id;
            }
            catch (SmartestApiException)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
        }

        /// <summary>
        /// Quiz du professeur sur le serveur (table quiz), même ordre que l’API.
        /// </summary>
        public async Task<List<QuizProfesseurListeItem>> GetMesQuizProfesseurListeAsync(
            string bearerToken,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var http = CreateHttp(bearerToken);
                var response = await http.GetAsync("/api/professeur/mes-quiz", cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(
                        response.StatusCode, body, "Liste des quiz");

                List<QuizProfesseurListeItem>? liste =
                    JsonConvert.DeserializeObject<List<QuizProfesseurListeItem>>(body);
                return liste ?? new List<QuizProfesseurListeItem>();
            }
            catch (SmartestApiException)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (JsonException jex)
            {
                throw new SmartestApiException(
                    HttpStatusCode.InternalServerError,
                    string.Empty,
                    "Liste des quiz : réponse serveur invalide.",
                    null,
                    jex);
            }
        }

        /// <summary>
        /// Synchronise uniquement les questions sur un quiz serveur (ex. copie miroir QR), sans emails ni statut publié.
        /// </summary>
        public async Task SyncQuestionsProfAsync(
            string bearerToken,
            long backendQuizId,
            IReadOnlyList<QuestionLocale>? questions,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var http = CreateHttp(bearerToken);
                var payload = new
                {
                    questions = (questions ?? Array.Empty<QuestionLocale>())
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
                    $"/api/quizs/{backendQuizId}/sync-questions-prof",
                    ToJson(payload),
                    cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(
                        response.StatusCode, body, "Synchronisation questions (prof)");
            }
            catch (SmartestApiException)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
        }

        public async Task PostPublicationWebAsync(
            string bearerToken,
            long backendQuizId,
            IReadOnlyList<string> emails,
            IReadOnlyList<QuestionLocale>? questions = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var http = CreateHttp(bearerToken);
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
                            difficulte = q.Difficulte,
                            imageBase64 = string.IsNullOrWhiteSpace(q.ImageBase64) ? null : q.ImageBase64.Trim(),
                            imageType = string.IsNullOrWhiteSpace(q.ImageType) ? null : q.ImageType.Trim(),
                        })
                        .ToList()
                };
                var response = await http.PostAsync(
                    $"/api/quizs/{backendQuizId}/publication-web",
                    ToJson(payload),
                    cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(
                        response.StatusCode, body, "Publication web");
            }
            catch (SmartestApiException)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
        }

        /// <summary>
        /// Supprime le quiz sur le backend (professeur propriétaire). Le serveur enlève aussi résultats,
        /// statistiques, réponses étudiants, etc. (même traitement que DELETE /api/quizs côté Spring).
        /// </summary>
        /// <remarks>Si le quiz n’existe déjà plus (404), l’appel réussit — la suppression locale peut continuer.</remarks>
        public async Task DeleteQuizAsync(string bearerToken, long backendQuizId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var http = CreateHttp(bearerToken);
                var response = await http.DeleteAsync($"/api/quizs/{backendQuizId}", cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return;
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(
                        response.StatusCode, body, "Suppression serveur");
            }
            catch (SmartestApiException)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
        }
    }
}
