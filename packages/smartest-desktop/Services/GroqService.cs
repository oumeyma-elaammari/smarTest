#nullable enable
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace smartest_desktop.Services
{
   
    public class GroqService
    {
        oujours par "gsk_"
        private const string GROQ_API_KEY = "diro key dyalkoum";

        private const string MODELE = "llama-3.1-8b-instant";

        private const int QUESTIONS_PAR_LOT = 4;

        private const int DELAI_ENTRE_LOTS_MS = 1500;

        private const int TAILLE_CONTEXTE_PAR_LOT = 2000;

        // ══════════════════════════════════════════════════════════════════════

        private const string GROQ_URL = "https://api.groq.com/openai/v1/chat/completions";

        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(45)
        };

        // ── Vérification de la clé API ────────────────────────────────────────

        public static void VerifierConfiguration()
        {
            if (string.IsNullOrWhiteSpace(GROQ_API_KEY) ||
                GROQ_API_KEY.Length < 20 ||
                !GROQ_API_KEY.StartsWith("gsk_"))
            {
                throw new InvalidOperationException(
                    "Clé API Groq non configurée.\n\n" +
                    "1. Allez sur console.groq.com\n" +
                    "2. Créez un compte gratuit\n" +
                    "3. Cliquez 'API Keys' → 'Create API Key'\n" +
                    "4. Collez la clé dans GroqService.cs (ligne GROQ_API_KEY)");
            }
        }

        
        public async Task<(string Texte, TimeSpan Duree)> GenererAsync(
            string prompt,
            CancellationToken ct = default)
        {
            VerifierConfiguration();

            var sw = Stopwatch.StartNew();

            var requestBody = new
            {
                model = MODELE,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You are a JSON quiz generator. You ONLY output valid JSON arrays. " +
                                  "Never add markdown, never add text before or after the JSON array. " +
                                  "Your entire response must start with [ and end with ]."
                    },
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                temperature = 0.1,
                max_tokens = 1500,
                stream = false
            };

            var texte = await EnvoyerRequeteAsync(requestBody, ct);
            sw.Stop();

            return (texte, sw.Elapsed);
        }

        // ── Génération par lots ───────────────────────────────────────────────

        /// <summary>
        /// Génère un grand nombre de questions en les découpant en plusieurs petits lots.
        ///
        /// FONCTIONNEMENT :
        ///   1. Découpe les N questions demandées en lots de QUESTIONS_PAR_LOT
        ///   2. Pour chaque lot, envoie une requête Groq avec un sous-ensemble du cours
        ///   3. Attend DELAI_ENTRE_LOTS_MS entre chaque lot (évite le rate limit)
        ///   4. Fusionne tous les résultats en un seul tableau JSON
        ///
        /// EXEMPLE : 15 questions QCM → 4 lots de 4 + 1 lot de 3 (en 4 × ~2s = ~8s)
        /// </summary>
        /// <param name="prompt">Fonction qui génère le prompt à partir du nb de questions</param>
        /// <param name="totalQuestions">Nombre total de questions à générer</param>
        /// <param name="onProgres">Callback de progression (lot actuel, total lots)</param>
        /// <param name="ct">Token d'annulation</param>
        public async Task<(string JsonFusionne, TimeSpan DureeTotal)> GenererParLotsAsync(
            Func<int, int, string> buildPromptPourLot,
            int totalQuestions,
            Action<int, int>? onProgres = null,
            CancellationToken ct = default)
        {
            VerifierConfiguration();

            var swTotal = Stopwatch.StartNew();
            var tousResultats = new List<string>();

            // Calculer le nombre de lots nécessaires
            int nbLots = (int)Math.Ceiling((double)totalQuestions / QUESTIONS_PAR_LOT);

            Debug.WriteLine($"[GroqService] Batching : {totalQuestions} questions → {nbLots} lots de {QUESTIONS_PAR_LOT}");

            for (int lot = 0; lot < nbLots; lot++)
            {
                ct.ThrowIfCancellationRequested();

                // Calculer combien de questions dans ce lot
                int debut = lot * QUESTIONS_PAR_LOT;
                int nbDansLot = Math.Min(QUESTIONS_PAR_LOT, totalQuestions - debut);

                onProgres?.Invoke(lot + 1, nbLots);

                Debug.WriteLine($"[GroqService] Lot {lot + 1}/{nbLots} : {nbDansLot} questions");

                // Construire le prompt pour ce lot
                string prompt = buildPromptPourLot(nbDansLot, debut + 1);

                // Retry automatique si rate limit (erreur 429)
                string texte = await GenererAvecRetryAsync(prompt, ct);

                // Extraire les items JSON du tableau retourné
                string jsonItems = ExtraireItemsTableau(texte);
                if (!string.IsNullOrEmpty(jsonItems))
                    tousResultats.Add(jsonItems);

                // Pause entre les lots (sauf après le dernier)
                if (lot < nbLots - 1)
                {
                    Debug.WriteLine($"[GroqService] Pause {DELAI_ENTRE_LOTS_MS} ms avant lot suivant...");
                    await Task.Delay(DELAI_ENTRE_LOTS_MS, ct);
                }
            }

            swTotal.Stop();

            // Fusionner tous les lots en un seul tableau JSON
            string jsonFusionne = "[" + string.Join(",", tousResultats) + "]";
            Debug.WriteLine($"[GroqService] Batching terminé en {swTotal.Elapsed.TotalSeconds:F1}s — {tousResultats.Count} lots fusionnés");

            return (jsonFusionne, swTotal.Elapsed);
        }

        // ── Retry automatique sur rate limit ──────────────────────────────────

        /// <summary>
        /// Envoie un prompt avec retry automatique en cas d'erreur 429 (rate limit).
        /// Attend 15 secondes avant de réessayer (jusqu'à 3 tentatives).
        /// </summary>
        private async Task<string> GenererAvecRetryAsync(string prompt, CancellationToken ct)
        {
            const int MAX_TENTATIVES = 3;
            const int ATTENTE_RETRY_MS = 15_000; // 15 s = la fenêtre rate-limit se renouvelle

            for (int tentative = 1; tentative <= MAX_TENTATIVES; tentative++)
            {
                try
                {
                    var (texte, _) = await GenererAsync(prompt, ct);
                    return texte;
                }
                catch (HttpRequestException ex) when (ex.Message.Contains("429") && tentative < MAX_TENTATIVES)
                {
                    Debug.WriteLine($"[GroqService] Rate limit (429) — attente {ATTENTE_RETRY_MS / 1000}s avant tentative {tentative + 1}/{MAX_TENTATIVES}");
                    await Task.Delay(ATTENTE_RETRY_MS, ct);
                }
            }

            // Dernière tentative sans catch
            var (resultat, _) = await GenererAsync(prompt, ct);
            return resultat;
        }

        // ── Appel HTTP bas niveau ─────────────────────────────────────────────

        private async Task<string> EnvoyerRequeteAsync(object requestBody, CancellationToken ct)
        {
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, GROQ_URL)
            {
                Content = content
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", GROQ_API_KEY);

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(request, ct);
            }
            catch (TaskCanceledException)
            {
                throw new TimeoutException(
                    "Groq n'a pas répondu en 45 secondes.\n" +
                    "Vérifiez votre connexion internet.");
            }
            catch (HttpRequestException ex)
            {
                throw new HttpRequestException(
                    $"Impossible de contacter Groq.\nVérifiez votre connexion internet.\n\nDétail : {ex.Message}");
            }

            var responseJson = await response.Content.ReadAsStringAsync(ct);

            Debug.WriteLine($"[GroqService] status={response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                string messageErreur = ExtraireMessageErreur(responseJson, (int)response.StatusCode);
                throw new HttpRequestException(messageErreur);
            }

            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                if (doc.RootElement.TryGetProperty("choices", out var choices) &&
                    choices.GetArrayLength() > 0)
                {
                    var firstChoice = choices[0];
                    if (firstChoice.TryGetProperty("message", out var message) &&
                        message.TryGetProperty("content", out var textContent))
                    {
                        string texte = textContent.GetString() ?? string.Empty;
                        Debug.WriteLine($"[GroqService] {texte.Length} chars reçus");
                        return texte;
                    }
                }
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"[GroqService] Erreur parsing : {ex.Message}");
            }

            return responseJson;
        }

        // ── Extraction des items d'un tableau JSON ────────────────────────────

        /// <summary>
        /// Extrait le contenu intérieur d'un tableau JSON (sans les crochets externes).
        /// Exemple : "[{...},{...}]" → "{...},{...}"
        /// Utilisé pour fusionner plusieurs lots.
        /// </summary>
        private static string ExtraireItemsTableau(string texte)
        {
            int start = texte.IndexOf('[');
            if (start == -1) return string.Empty;

            int depth = 0;
            int end = -1;
            for (int i = start; i < texte.Length; i++)
            {
                if (texte[i] == '[') depth++;
                else if (texte[i] == ']')
                {
                    depth--;
                    if (depth == 0) { end = i; break; }
                }
            }

            if (end == -1) return string.Empty;

            // Contenu sans les crochets [ ]
            string interieur = texte[(start + 1)..end].Trim();
            return interieur;
        }

        // ── Extraction des messages d'erreur Groq ─────────────────────────────

        private static string ExtraireMessageErreur(string responseJson, int statusCode)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseJson);
                if (doc.RootElement.TryGetProperty("error", out var error) &&
                    error.TryGetProperty("message", out var msg))
                {
                    string detail = msg.GetString() ?? "Erreur inconnue";

                    return statusCode switch
                    {
                        401 => $"Clé API Groq invalide.\n\n" +
                               $"Vérifiez la clé dans GroqService.cs\n" +
                               $"Elle doit commencer par 'gsk_'\n\n" +
                               $"Détail : {detail}",

                        429 => $"429 — Quota Groq dépassé temporairement.\n\n" +
                               $"Le batching va réessayer automatiquement.\n" +
                               $"Si l'erreur persiste, augmentez DELAI_ENTRE_LOTS_MS dans GroqService.cs\n\n" +
                               $"Détail : {detail}",

                        503 => $"Serveurs Groq temporairement indisponibles.\n" +
                               $"Réessayez dans quelques secondes.",

                        _ => $"Erreur Groq (code {statusCode}) :\n{detail}"
                    };
                }
            }
            catch { }

            return $"Erreur Groq (code {statusCode}).\nRéponse : {responseJson[..Math.Min(200, responseJson.Length)]}";
        }

        // ── Utilitaires ───────────────────────────────────────────────────────

        public static string NomModele => MODELE;

        
        public static int TailleContexteParLot => TAILLE_CONTEXTE_PAR_LOT;

        public const int MAX_QUESTIONS_PAR_APPEL = 4;

        
        public const int LIMITE_CONTENU_CHARS = TAILLE_CONTEXTE_PAR_LOT;

       
        public static string BuildPromptQcmLot(string contenuCours, int nbQuestions, int numeroDepart)
        {
            return $@"Generate exactly {nbQuestions} multiple-choice questions (QCM) from this course content.
Start question numbering at {numeroDepart}.

Course content:
{contenuCours}

Return ONLY a JSON array with exactly {nbQuestions} objects. Each object must have:
- ""type"": ""QCM""
- ""enonce"": the question text (in the same language as the course)
- ""optionA"", ""optionB"", ""optionC"", ""optionD"": the 4 answer choices
- ""reponseCorrecte"": ""A"", ""B"", ""C"", or ""D""
- ""explication"": brief explanation of the correct answer

Output ONLY the JSON array, nothing else.";
        }

        /// <summary>
        /// Construit le prompt pour un lot de questions d'examen mixtes (QCM + Checkbox + Rédaction).
        /// </summary>
        public static string BuildPromptExamenLot(
            string contenuCours,
            int nbQCM, int nbCheckbox, int nbRedaction,
            string difficulte,
            int numeroDepart)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Generate exactly {nbQCM + nbCheckbox + nbRedaction} exam questions from this course content.");
            sb.AppendLine($"Difficulty: {difficulte}. Start numbering at {numeroDepart}.");
            sb.AppendLine($"Breakdown: {nbQCM} QCM, {nbCheckbox} CHECKBOX (multiple correct answers), {nbRedaction} REDACTION (open answer).");
            sb.AppendLine();
            sb.AppendLine("Course content:");
            sb.AppendLine(contenuCours);
            sb.AppendLine();
            sb.AppendLine("Return ONLY a JSON array. Each object must have:");
            sb.AppendLine("- \"type\": \"QCM\", \"CHECKBOX\", or \"REDACTION\"");
            sb.AppendLine("- \"enonce\": question text (same language as the course)");
            sb.AppendLine("For QCM: \"optionA\", \"optionB\", \"optionC\", \"optionD\", \"reponseCorrecte\" (A/B/C/D), \"explication\"");
            sb.AppendLine("For CHECKBOX: \"optionA\"..\"optionD\", \"reponsesCorrectes\" (array of letters like [\"A\",\"C\"]), \"explication\"");
            sb.AppendLine("For REDACTION: \"reponseModele\" (model answer), \"explication\"");
            sb.AppendLine("Output ONLY the JSON array, nothing else.");

            return sb.ToString();
        }

    
        public static string BuildPromptExamen(
            string contenuCours,
            int nbQCM, int nbCheckbox, int nbRedaction,
            string difficulte)
        {
            return BuildPromptExamenLot(contenuCours, nbQCM, nbCheckbox, nbRedaction, difficulte, 1);
        }
    }
}