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
using smartest_desktop.Helpers;
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

        [JsonProperty("statut")]
        public string Statut { get; set; } = "";

        [JsonProperty("dateDebut")]
        public DateTime? DateDebut { get; set; }
    }

    public sealed class ExamenEtudiantPassageItem
    {
        [JsonProperty("etudiantId")]
        public long EtudiantId { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; } = "";

        [JsonProperty("nom")]
        public string Nom { get; set; } = "";

        [JsonProperty("noteProposee")]
        public double? NoteProposee { get; set; }

        [JsonProperty("noteFinale")]
        public double? NoteFinale { get; set; }

        [JsonProperty("valideeParProf")]
        public bool ValideeParProf { get; set; }
    }

    public sealed class ExamenWebPublicationApiService
    {
        private readonly Func<string, HttpClient>? _createClientOverride;
        private readonly string _baseUrl;

        public ExamenWebPublicationApiService(string? baseUrl = null, Func<string, HttpClient>? createClientOverride = null)
        {
            _baseUrl = string.IsNullOrWhiteSpace(baseUrl)
                ? SmartestBackendBaseUrl.Resolve()
                : baseUrl.TrimEnd('/');
            _createClientOverride = createClientOverride;
        }

        private HttpClient CreateHttp(string bearerToken, bool inclureCleGroq = false)
        {
            bearerToken = DesktopSessionTokenHelper.Normaliser(bearerToken);
            if (_createClientOverride != null) return _createClientOverride(bearerToken);
            var client = new HttpClient { BaseAddress = new Uri(_baseUrl) };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            if (inclureCleGroq && GroqKeyService.TryObtenirCleValide(App.LocalDb, out string groqKey))
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-Groq-Api-Key", groqKey);
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

        /// <summary>Met à jour le créneau côté serveur (examen PLANIFIE publié sur le web).</summary>
        public async Task ModifierCreneauAsync(
            string bearerToken,
            long examenId,
            DateTime dateDebut,
            int dureeMinutes,
            CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken);
            try
            {
                var payload = JsonConvert.SerializeObject(new
                {
                    dateDebut = dateDebut.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
                    duree = Math.Max(1, dureeMinutes),
                });
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await http.PatchAsync($"/api/examens-publies/{examenId}/creneau", content, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Mise à jour du créneau examen");
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
            using var http = CreateHttp(bearerToken, inclureCleGroq: true);
            var ordered = (questions ?? Array.Empty<QuestionLocale>())
                .OrderBy(q => q.Numero)
                .Where(q => !string.IsNullOrWhiteSpace(q.Enonce) && EstQuestionPubliablePourWeb(q))
                .ToList();
            if (ordered.Count == 0)
                throw new SmartestApiException(
                    HttpStatusCode.BadRequest,
                    string.Empty,
                    "Publication web examen : aucune question exploitable (énoncé requis ; QCM/VF/cases à cocher : au moins 2 propositions non vides parmi A–D ; rédaction : pas d’options obligatoires).");

            var payload = new
            {
                questions = ordered.Select(q => new
                {
                    type = string.IsNullOrWhiteSpace(q.Type) ? "QCM" : q.Type.Trim(),
                    enonce = q.Enonce ?? string.Empty,
                    optionA = q.OptionA ?? string.Empty,
                    optionB = q.OptionB ?? string.Empty,
                    optionC = q.OptionC ?? string.Empty,
                    optionD = q.OptionD ?? string.Empty,
                    reponseCorrecte = q.ReponseCorrecte ?? string.Empty,
                    explication = q.Explication ?? string.Empty,
                    difficulte = q.Difficulte ?? string.Empty,
                    dureeSecondesIndicative = Math.Clamp(q.DureeSecondesIndicative <= 0 ? 60 : q.DureeSecondesIndicative, 5, 7200),
                    baremePoints = q.BaremePoints > 0 ? q.BaremePoints : (double?)null,
                    reponsesCorrectesJson = string.IsNullOrWhiteSpace(q.ReponsesCorrectesJson) ? "[]" : q.ReponsesCorrectesJson,
                    reponseModele = q.ReponseModele ?? string.Empty,
                    imageBase64 = string.IsNullOrWhiteSpace(q.ImageBase64) ? null : q.ImageBase64.Trim(),
                    imageType = string.IsNullOrWhiteSpace(q.ImageType) ? null : q.ImageType.Trim(),
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

        private static bool EstQuestionPubliablePourWeb(QuestionLocale q)
        {
            var t = (q.Type ?? "QCM").Trim().ToUpperInvariant();
            if (t is "REDACTION" or "DISSERTATION" or "ESSAY" or "LIBRE")
                return true;
            int n = new[] { q.OptionA, q.OptionB, q.OptionC, q.OptionD }
                .Count(s => !string.IsNullOrWhiteSpace(s));
            return n >= 2;
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

        public async Task<JObject> ControlerExamenAsync(
            string bearerToken,
            long examenId,
            string action,
            string? groqApiKey = null,
            CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken, inclureCleGroq: string.IsNullOrWhiteSpace(groqApiKey));
            if (!string.IsNullOrWhiteSpace(groqApiKey))
                http.DefaultRequestHeaders.TryAddWithoutValidation("X-Groq-Api-Key", groqApiKey.Trim());
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

        public async Task<bool> ForcerConsolidationAsync(
            string bearerToken,
            long examenId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var http = CreateHttp(bearerToken);
                var response = await http.PostAsync(
                    $"/api/examens-publies/{examenId}/supervision/forcer-consolidation",
                    null,
                    cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<IReadOnlyList<ExamenEtudiantPassageItem>> GetEtudiantsPassagesAsync(
            string bearerToken,
            long examenId,
            CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken);
            var endpointAtteint = false;

            foreach (var path in new[]
                     {
                         $"/api/examens-publies/{examenId}/supervision/etudiants-passages",
                         $"/api/examens-publies/{examenId}/supervision/participants-soumis",
                     })
            {
                var response = await http.GetAsync(path, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.StatusCode == HttpStatusCode.NotFound)
                    continue;
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Liste étudiants examen");
                endpointAtteint = true;
                if (string.IsNullOrWhiteSpace(body))
                    break;
                var parsed = JsonConvert.DeserializeObject<List<ExamenEtudiantPassageItem>>(body);
                if (parsed != null && parsed.Count > 0)
                    return parsed;
            }

            var repli = await ChargerParticipantsDepuisSourcesConnuesAsync(bearerToken, examenId, cancellationToken);
            if (repli.Count > 0)
                return repli;
            if (endpointAtteint)
                return Array.Empty<ExamenEtudiantPassageItem>();

            return repli;
        }

        /// <summary>Lit un identifiant Long depuis du JSON Spring/Jackson (entier, décimal ou chaîne).</summary>
        private static long? TryReadLong(JToken? tok)
        {
            if (tok == null || tok.Type == JTokenType.Null) return null;
            switch (tok.Type)
            {
                case JTokenType.Integer:
                    return tok.Value<long>();
                case JTokenType.Float:
                    return (long)Math.Round(tok.Value<double>(), MidpointRounding.AwayFromZero);
                case JTokenType.String:
                    return long.TryParse(tok.Value<string>(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)
                        ? l
                        : null;
                default:
                    return null;
            }
        }

        /// <summary>
        /// Repli si le backend n'a pas encore l'endpoint dédié : salle d'attente + résultats en attente + détection des copies.
        /// </summary>
        private async Task<IReadOnlyList<ExamenEtudiantPassageItem>> ChargerParticipantsDepuisSourcesConnuesAsync(
            string bearerToken,
            long examenId,
            CancellationToken cancellationToken)
        {
            var map = new Dictionary<long, ExamenEtudiantPassageItem>();

            void Ajouter(ExamenEtudiantPassageItem item)
            {
                if (item == null || item.EtudiantId <= 0) return;
                if (!map.ContainsKey(item.EtudiantId))
                    map[item.EtudiantId] = item;
            }

            try
            {
                var attente = await GetResultatsEnAttenteAsync(bearerToken, examenId, cancellationToken);
                foreach (var tok in attente)
                {
                    if (tok is not JObject row) continue;
                    long? eid = TryReadLong(row["etudiantId"]);
                    if (eid == null || eid <= 0) continue;
                    var npTok = row["noteProposee"];
                    double? np = npTok != null && (npTok.Type == JTokenType.Float || npTok.Type == JTokenType.Integer)
                        ? npTok.Value<double>()
                        : null;
                    Ajouter(new ExamenEtudiantPassageItem
                    {
                        EtudiantId = eid.Value,
                        NoteProposee = np,
                    });
                }
            }
            catch (SmartestApiException)
            {
                /* ignoré : on continue avec la salle d'attente */
            }

            try
            {
                var salle = await GetSalleAttenteAsync(bearerToken, examenId, cancellationToken);
                if (salle["participantsSoumis"] is JArray soumis)
                    FusionnerParticipantsJson(map, soumis);

                if (salle["connectes"] is JArray connectes)
                {
                    foreach (var tok in connectes)
                    {
                        if (tok is not JObject row) continue;
                        long? eid = TryReadLong(row["etudiantId"]);
                        if (eid == null || eid <= 0 || map.ContainsKey(eid.Value)) continue;
                        string email = row["email"]?.Value<string>() ?? "";
                        if (await EtudiantPossedeCorrectionsAsync(bearerToken, examenId, eid.Value, cancellationToken))
                        {
                            Ajouter(new ExamenEtudiantPassageItem { EtudiantId = eid.Value, Email = email });
                        }
                    }
                }
            }
            catch (SmartestApiException)
            {
                /* dernier recours : map déjà rempli par resultats-en-attente */
            }

            return map.Values
                .OrderBy(x => x.Nom, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Email, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.EtudiantId)
                .ToList();
        }

        private static void FusionnerParticipantsJson(Dictionary<long, ExamenEtudiantPassageItem> map, JArray array)
        {
            foreach (var tok in array)
            {
                if (tok is not JObject row) continue;
                long? eid = TryReadLong(row["etudiantId"]);
                if (eid == null || eid <= 0) continue;
                var npTok = row["noteProposee"];
                double? np = npTok != null && (npTok.Type == JTokenType.Float || npTok.Type == JTokenType.Integer)
                    ? npTok.Value<double>()
                    : null;
                map[eid.Value] = new ExamenEtudiantPassageItem
                {
                    EtudiantId = eid.Value,
                    Email = row["email"]?.Value<string>() ?? "",
                    Nom = row["nom"]?.Value<string>() ?? "",
                    NoteProposee = np,
                    NoteFinale = row["noteFinale"]?.Type == JTokenType.Float || row["noteFinale"]?.Type == JTokenType.Integer
                        ? row["noteFinale"]!.Value<double>()
                        : null,
                    ValideeParProf = row["valideeParProf"]?.Value<bool>() ?? false,
                };
            }
        }

        private async Task<bool> EtudiantPossedeCorrectionsAsync(
            string bearerToken,
            long examenId,
            long etudiantId,
            CancellationToken cancellationToken)
        {
            try
            {
                await GetCorrectionsEtudiantAsync(bearerToken, examenId, etudiantId, cancellationToken);
                return true;
            }
            catch (SmartestApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async Task<JArray> GetResultatsEnAttenteAsync(string bearerToken, long examenId, CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken);
            var response = await http.GetAsync($"/api/examens-publies/{examenId}/supervision/resultats-en-attente", cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Résultats en attente");
            return string.IsNullOrWhiteSpace(body) ? new JArray() : JArray.Parse(body);
        }

        public async Task<JObject> GetCorrectionsEtudiantAsync(string bearerToken, long examenId, long etudiantId, CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken);
            var response = await http.GetAsync($"/api/examens-publies/{examenId}/supervision/etudiants/{etudiantId}/corrections", cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Corrections étudiant");
            return string.IsNullOrWhiteSpace(body) ? new JObject() : JObject.Parse(body);
        }

        public async Task ValiderCorrectionsDetailAsync(string bearerToken, long examenId, long etudiantId, string jsonBody, CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken);
            using var payload = new StringContent(jsonBody ?? "{}", System.Text.Encoding.UTF8, "application/json");
            var response = await http.PostAsync($"/api/examens-publies/{examenId}/supervision/etudiants/{etudiantId}/valider-corrections-detail", payload, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Valider corrections");
        }

        public async Task SynchroniserNoteWorkbenchAsync(string bearerToken, long examenId, long etudiantId, CancellationToken cancellationToken = default)
        {
            using var http = CreateHttp(bearerToken);
            var response = await http.PostAsync($"/api/examens-publies/{examenId}/supervision/etudiants/{etudiantId}/synchroniser-note-workbench", null, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Synchro note Workbench");
        }

        private sealed class ExamenPublieResponse
        {
            [JsonProperty("id")]
            public long Id { get; set; }
        }
    }
}
