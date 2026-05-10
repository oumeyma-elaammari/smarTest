using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Exceptions;
using smartest_desktop.Helpers;

namespace smartest_desktop.Services
{
    /// <summary>Session QR éphémère : création API + init STOMP prof (sans UI bureau).</summary>
    public sealed class QuizQrService
    {
        private static readonly JsonSerializerSettings JsonApiSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore,
        };

        private static HttpClient CreerHttpClientAuthentifie(string bearerToken)
        {
            string baseAddr = SmartestBackendBaseUrl.Resolve();
            var client = new HttpClient { BaseAddress = new Uri(baseAddr) };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken.Trim());
            return client;
        }

        private static Uri ConstruireWebSocketUri(Uri baseHttpUri)
        {
            var wsScheme = baseHttpUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws";
            var b = new UriBuilder(baseHttpUri)
            {
                Scheme = wsScheme,
                Path = "/ws",
                Fragment = "",
            };
            b.Query = string.Empty;
            return b.Uri;
        }

        private static HttpContent JsonContent(object dto) =>
            new StringContent(
                JsonConvert.SerializeObject(dto, JsonApiSettings),
                Encoding.UTF8,
                "application/json");

        /// <summary>DELETE session mémoire (best-effort, ignore 404).</summary>
        public static async Task SupprimerSessionQrAsync(string bearerJwt, string sessionToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken)) return;
            try
            {
                using var http = CreerHttpClientAuthentifie(bearerJwt);
                var réponse = await http
                    .DeleteAsync("/api/qr-live/sessions/" + Uri.EscapeDataString(sessionToken.Trim()))
                    .ConfigureAwait(false);
                _ = réponse.Content.ReadAsStringAsync();
            }
            catch
            {
                /* best-effort */
            }
        }

        /// <summary>POST session, STOMP prof/init, fermeture WebSocket — retourne le token UUID.</summary>
        public static async Task<string> InitialiserSessionQrSansUiAsync(
            string bearerJwt,
            QuizLocal quiz,
            IList<QuestionLocale> questions)
        {
            string baseBackend = SmartestBackendBaseUrl.Resolve();
            Uri wsUri = ConstruireWebSocketUri(new Uri(baseBackend));

            using var httpInit = CreerHttpClientAuthentifie(bearerJwt);

            HttpResponseMessage created;
            try
            {
                created = await httpInit
                    .PostAsync("/api/qr-live/sessions", JsonContent(new { }), CancellationToken.None)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }

            string createdBody = await created.Content.ReadAsStringAsync(CancellationToken.None).ConfigureAwait(false);
            if (!created.IsSuccessStatusCode)
                throw SmartestApiException.FromHttpFailure(created.StatusCode, createdBody, "Création session QR");

            string? tokenJeton = JsonConvert.DeserializeObject<SessionTokenResponseDto>(createdBody)?.SessionToken?.Trim()
                              ?? throw new SmartestApiException(
                                  created.StatusCode, createdBody, "Réponse session QR invalide.", null, null);

            List<QuestionLocale> tri = questions.OrderBy(q => q.Numero).ToList();
            var dtoInit = new QuizQrEphemeralProfInitDto
            {
                SessionToken = tokenJeton,
                Titre = string.IsNullOrWhiteSpace(quiz?.Titre) ? "Quiz" : quiz!.Titre.Trim(),
                Questions = tri.Select(ToQuestionDto).Where(q => q != null).Cast<QuestionEphemDto>().ToList(),
            };

            if (dtoInit.Questions.Count == 0)
                throw new InvalidOperationException("Quiz sans questions envoyables.");

            var stomp = new StompWebSocketSession();
            try
            {
                await stomp.ConnecterAsync(wsUri, bearerJwt, CancellationToken.None).ConfigureAwait(false);
                await stomp
                    .EnvoyerJsonAsync("/app/quiz-qr/prof/init", dtoInit, bearerJwt, CancellationToken.None)
                    .ConfigureAwait(false);
            }
            finally
            {
                await stomp.DisposeAsync().ConfigureAwait(false);
            }

            return tokenJeton;
        }

        private static QuestionEphemDto? ToQuestionDto(QuestionLocale ql)
        {
            string lettre = string.IsNullOrWhiteSpace(ql.ReponseCorrecte)
                ? "A"
                : ql.ReponseCorrecte.Trim().ToUpperInvariant();
            if (lettre.Length != 1 || lettre[0] < 'A' || lettre[0] > 'D')
                lettre = "A";

            return new QuestionEphemDto
            {
                Enonce = ql.Enonce ?? string.Empty,
                OptionA = ql.OptionA ?? string.Empty,
                OptionB = ql.OptionB ?? string.Empty,
                OptionC = ql.OptionC ?? string.Empty,
                OptionD = ql.OptionD ?? string.Empty,
                ReponseCorrecte = lettre,
            };
        }

        private sealed class SessionTokenResponseDto
        {
            [JsonProperty("sessionToken")] public string? SessionToken { get; set; }
        }

        private sealed class QuizQrEphemeralProfInitDto
        {
            public string SessionToken { get; set; } = string.Empty;
            public string Titre { get; set; } = string.Empty;

            public List<QuestionEphemDto> Questions { get; set; } = new();
        }

        private sealed class QuestionEphemDto
        {
            public string Enonce { get; set; } = string.Empty;
            public string OptionA { get; set; } = string.Empty;
            public string OptionB { get; set; } = string.Empty;
            public string OptionC { get; set; } = string.Empty;
            public string OptionD { get; set; } = string.Empty;
            public string ReponseCorrecte { get; set; } = "A";
        }
    }
}
