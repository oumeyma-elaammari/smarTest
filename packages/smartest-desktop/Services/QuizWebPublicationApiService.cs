using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using smartest_desktop.Models;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Exceptions;

namespace smartest_desktop.Services
{
    /// <summary>Appels API pour la publication web des quiz (liste d'emails sur le serveur).</summary>
    public sealed class QuizWebPublicationApiService
    {
        private const string DefaultBaseUrl = "http://localhost:8081";

        private readonly Func<string, HttpClient>? _createClientOverride;
        private readonly string _baseUrl;

        public QuizWebPublicationApiService(string baseUrl = DefaultBaseUrl, Func<string, HttpClient>? createClientOverride = null)
        {
            _baseUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.TrimEnd('/');
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

        public async Task<long> CreateQuizAsync(
            string bearerToken,
            string titre,
            int dureeMinutes,
            long professeurId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var http = CreateHttp(bearerToken);
                var payload = new { titre, duree = dureeMinutes, professeurId };
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
                            difficulte = q.Difficulte
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

        /// <summary>Supprime le quiz sur le backend (professeur propriétaire uniquement).</summary>
        public async Task DeleteQuizAsync(string bearerToken, long backendQuizId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var http = CreateHttp(bearerToken);
                var response = await http.DeleteAsync($"/api/quizs/{backendQuizId}", cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
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
