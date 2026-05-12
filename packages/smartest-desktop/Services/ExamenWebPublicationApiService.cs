using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using smartest_desktop.Models;
using smartest_desktop.Exceptions;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using smartest_desktop.Data.LocalEntities;

namespace smartest_desktop.Services
{
    /// <summary>Métadonnée minimale pour la liste « mes publications web » (prof ou élève).</summary>
    public sealed class ExamenMesPublicationItem
    {
        [JsonProperty("id")]
        public long Id { get; set; }

        [JsonProperty("titre")]
        public string Titre { get; set; } = "";
    }

    public sealed class ExamenWebPublicationApiService
    {
        private const string DefaultBaseUrl = "http://localhost:8081";
        private readonly Func<string, HttpClient>? _createClientOverride;
        private readonly string _baseUrl;

        public ExamenWebPublicationApiService(string baseUrl = DefaultBaseUrl, Func<string, HttpClient>? createClientOverride = null)
        {
            _baseUrl = string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.TrimEnd('/');
            _createClientOverride = createClientOverride;
        }

        private HttpClient CreateHttp(string bearerToken)
        {
            if (_createClientOverride != null) return _createClientOverride(bearerToken);
            var client = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            return client;
        }

        public async Task<long> GetProfesseurIdAsync(string bearerToken, CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken);
            try
            {
                var response = await http.GetAsync("/api/professeur/profil", cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Profil professeur");

                var dto = JsonConvert.DeserializeObject<ProfesseurProfilResponse>(body)
                          ?? throw new SmartestApiException(response.StatusCode, body, "Profil professeur : réponse serveur invalide.");
                if (dto.Id <= 0)
                    throw new SmartestApiException(response.StatusCode, body, "Profil professeur : réponse serveur invalide.");
                return dto.Id;
            }
            catch (SmartestApiException) { throw; }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
        }

        public async Task<long> CreateExamenAsync(string bearerToken, long professeurId, string titre, int duree, string description, DateTime dateDebut, DateTime dateFin, CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken);
            try
            {
                var query = new Dictionary<string, string>
                {
                    ["professeurId"] = professeurId.ToString(CultureInfo.InvariantCulture),
                    ["titre"] = titre,
                    ["duree"] = duree.ToString(CultureInfo.InvariantCulture),
                    ["description"] = description ?? string.Empty,
                    ["dateDebut"] = dateDebut.ToString("yyyy-MM-ddTHH:mm:ss"),
                    ["dateFin"] = dateFin.ToString("yyyy-MM-ddTHH:mm:ss")
                };

                var url = "/api/examens-publies?" + await new FormUrlEncodedContent(query).ReadAsStringAsync(cancellationToken);
                var response = await http.PostAsync(url, null, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Publication examen");

                var dto = JsonConvert.DeserializeObject<ExamenPublieResponse>(body)
                          ?? throw new SmartestApiException(response.StatusCode, body, "Publication examen : réponse serveur invalide.");
                if (dto.Id <= 0)
                    throw new SmartestApiException(response.StatusCode, body, "Publication examen : réponse serveur invalide.");
                return dto.Id;
            }
            catch (SmartestApiException) { throw; }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
        }

        /// <summary>
        /// Retire le suffixe « (2) », « (3) » ajouté localement quand plusieurs examens ont le même titre,
        /// pour comparer avec le titre stocké sur le serveur.
        /// </summary>
        public static string NormaliserTitreExamenPourCorrespondance(string? titre)
        {
            if (string.IsNullOrWhiteSpace(titre)) return "";
            var t = titre.Trim();
            return Regex.Replace(t, @"\s*\(\d+\)\s*$", "", RegexOptions.None).Trim();
        }

        /// <summary>Indique si le titre local et le titre côté API désignent le même examen affiché.</summary>
        public static bool TitresExamenCorrespondentPourSuppression(string? titreLocalOuAffiche, string? titreServeur)
        {
            var a = NormaliserTitreExamenPourCorrespondance(titreLocalOuAffiche);
            var b = NormaliserTitreExamenPourCorrespondance(titreServeur);
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Liste des examens publiés sur le web pour l’utilisateur connecté (prof : ses examens ;
        /// élève : ceux autorisés par email).
        /// </summary>
        public async Task<IReadOnlyList<ExamenMesPublicationItem>> GetMesPublicationsWebAsync(
            string bearerToken,
            CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken);
            try
            {
                var response = await http.GetAsync("/api/examens-publies/mes-publications-web", cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Liste examens web");

                var list = JsonConvert.DeserializeObject<List<ExamenMesPublicationItem>>(body);
                return list ?? new List<ExamenMesPublicationItem>();
            }
            catch (SmartestApiException) { throw; }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
        }

        /// <summary>Supprime l'examen publié sur le backend (professeur propriétaire uniquement).</summary>
        public async Task DeleteExamenPublieAsync(string bearerToken, long backendExamenId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var http = CreateHttp(bearerToken);
                var response = await http.DeleteAsync($"/api/examens-publies/{backendExamenId}", cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.StatusCode == HttpStatusCode.NotFound)
                    return;
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Suppression serveur examen");
            }
            catch (SmartestApiException) { throw; }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
        }

        /// <summary>Envoie les questions QCM au serveur (obligationaire pour supervision / élèves). Même schéma que la publication quiz web.</summary>
        public async Task SynchroniserQuestionsPublicationWebAsync(
            string bearerToken,
            long examenId,
            IReadOnlyList<QuestionLocale> questions,
            CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken);
            static int CompteOptions(QuestionLocale q) =>
                new[] { q.OptionA, q.OptionB, q.OptionC, q.OptionD }
                    .Count(s => !string.IsNullOrWhiteSpace(s));

            var ordered = (questions ?? Array.Empty<QuestionLocale>())
                .OrderBy(q => q.Numero)
                .Where(q => !string.IsNullOrWhiteSpace(q.Enonce) && CompteOptions(q) >= 2)
                .ToList();
            if (ordered.Count == 0)
                throw new SmartestApiException(
                    HttpStatusCode.BadRequest,
                    string.Empty,
                    "Publication web examen : aucune question exploitable (chaque item doit avoir un énoncé et au moins 2 propositions non vides parmi A–D).");

            var payload = new
            {
                questions = ordered                .Select(q => new
                {
                    enonce = q.Enonce ?? string.Empty,
                    optionA = q.OptionA ?? string.Empty,
                    optionB = q.OptionB ?? string.Empty,
                    optionC = q.OptionC ?? string.Empty,
                    optionD = q.OptionD ?? string.Empty,
                    reponseCorrecte = q.ReponseCorrecte ?? string.Empty,
                    explication = q.Explication ?? string.Empty,
                    difficulte = q.Difficulte ?? string.Empty,
                    dureeSecondesIndicative = Math.Clamp(q.DureeSecondesIndicative <= 0 ? 60 : q.DureeSecondesIndicative, 5, 7200),
                }).ToList()
            };

            try
            {
                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await http.PostAsync(
                    $"/api/examens-publies/{examenId}/publication-web/questions",
                    content,
                    cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Synchronisation questions examen web");
            }
            catch (SmartestApiException) { throw; }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
        }

        public async Task DefinirEmailsAutorisesAsync(string bearerToken, long examenId, IReadOnlyList<string> emails, CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken);
            try
            {
                var payload = new StringContent(
                    JsonConvert.SerializeObject(emails ?? Array.Empty<string>()),
                    Encoding.UTF8,
                    "application/json");
                var response = await http.PostAsync($"/api/examens-publies/{examenId}/publication-web/emails", payload, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Publication web examen (emails)");
            }
            catch (SmartestApiException) { throw; }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
        }

        public async Task<JObject> ControlerExamenAsync(string bearerToken, long examenId, string action, CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken);
            try
            {
                var response = await http.PatchAsync($"/api/examens-publies/{examenId}/controle/{action}", null, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(response.StatusCode, body, $"Contrôle examen ({action})");
                return string.IsNullOrWhiteSpace(body) ? new JObject() : JObject.Parse(body);
            }
            catch (SmartestApiException) { throw; }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
        }

        /// <summary>Passe à la question suivante (phase EN_COURS). Déclenche la diffusion WebSocket côté serveur.</summary>
        public async Task<JObject> QuestionSuivanteAsync(string bearerToken, long examenId, CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken);
            try
            {
                var response = await http.PatchAsync($"/api/examens-publies/{examenId}/controle/question/suivante", null, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Question suivante");
                return string.IsNullOrWhiteSpace(body) ? new JObject() : JObject.Parse(body);
            }
            catch (SmartestApiException) { throw; }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
        }

        public async Task<JObject> QuestionPrecedenteAsync(string bearerToken, long examenId, CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken);
            try
            {
                var response = await http.PatchAsync($"/api/examens-publies/{examenId}/controle/question/precedente", null, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Question précédente");
                return string.IsNullOrWhiteSpace(body) ? new JObject() : JObject.Parse(body);
            }
            catch (SmartestApiException) { throw; }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
        }

        public async Task<JObject> AjusterTempsAsync(string bearerToken, long examenId, int deltaMinutes, CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken);
            try
            {
                var url = $"/api/examens-publies/{examenId}/controle/temps?deltaMinutes={deltaMinutes.ToString(CultureInfo.InvariantCulture)}";
                var response = await http.PatchAsync(url, null, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Ajustement temps examen");
                return string.IsNullOrWhiteSpace(body) ? new JObject() : JObject.Parse(body);
            }
            catch (SmartestApiException) { throw; }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
        }

        public async Task<JObject> AjusterMinuteurQuestionAsync(string bearerToken, long examenId, int deltaSeconds, CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken);
            try
            {
                var url = $"/api/examens-publies/{examenId}/controle/minuteur-question?deltaSeconds={deltaSeconds.ToString(CultureInfo.InvariantCulture)}";
                var response = await http.PatchAsync(url, null, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Ajustement minuteur question");
                return string.IsNullOrWhiteSpace(body) ? new JObject() : JObject.Parse(body);
            }
            catch (SmartestApiException) { throw; }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
        }

        public async Task<JObject> ConfigurerModePassageAsync(string bearerToken, long examenId, string mode, int? questionDurationSeconds = null, CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken);
            try
            {
                var query = new Dictionary<string, string> { ["mode"] = mode };
                if (questionDurationSeconds.HasValue)
                    query["questionDurationSeconds"] = questionDurationSeconds.Value.ToString(CultureInfo.InvariantCulture);

                var url = $"/api/examens-publies/{examenId}/controle/mode-passage?" + await new FormUrlEncodedContent(query).ReadAsStringAsync(cancellationToken);
                var response = await http.PatchAsync(url, null, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Configuration mode passage examen");
                return string.IsNullOrWhiteSpace(body) ? new JObject() : JObject.Parse(body);
            }
            catch (SmartestApiException) { throw; }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
        }

        public async Task<JObject> GetSupervisionSnapshotAsync(string bearerToken, long examenId, CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken);
            try
            {
                var response = await http.GetAsync($"/api/examens-publies/{examenId}/supervision/snapshot", cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Snapshot supervision examen");
                return string.IsNullOrWhiteSpace(body) ? new JObject() : JObject.Parse(body);
            }
            catch (SmartestApiException) { throw; }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
        }

        /// <summary>Liste d’attente / présents pour la session web (professeur propriétaire).</summary>
        public async Task<JObject> GetSalleAttenteAsync(string bearerToken, long examenId, CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken);
            try
            {
                var response = await http.GetAsync($"/api/examens-publies/{examenId}/salle-attente", cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Salle d'attente examen");
                return string.IsNullOrWhiteSpace(body) ? new JObject() : JObject.Parse(body);
            }
            catch (SmartestApiException) { throw; }
            catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
            catch (HttpRequestException ex)
            {
                throw SmartestNetworkException.ServerUnreachable(ex);
            }
        }

        private sealed class ExamenPublieResponse
        {
            [JsonProperty("id")]
            public long Id { get; set; }
        }
    }
}
