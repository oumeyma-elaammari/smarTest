#nullable enable
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace smartest_desktop.Services
{
   
    public class GroqService : IGroqGenerationClient
    {
        private readonly string _apiKey;

        public GroqService(string apiKey)
        {
            _apiKey = apiKey;
        }

        private const string MODELE = "meta-llama/llama-4-scout-17b-16e-instruct";

        private const int QUESTIONS_PAR_LOT = 4;

        /// <summary>Espace les lots pour rester sous le TPM (tokens/minute) Groq gratuit.</summary>
        private const int DELAI_ENTRE_LOTS_MS = 4_000;

        private const int TAILLE_CONTEXTE_PAR_LOT = 2500;

        // ══════════════════════════════════════════════════════════════════════

        private const string GROQ_URL = "https://api.groq.com/openai/v1/chat/completions";

        private static readonly HttpClient _http = new()
        {
            Timeout = TimeSpan.FromSeconds(45)
        };

        // ── Vérification de la clé API ────────────────────────────────────────

        public void VerifierConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_apiKey) ||
                _apiKey.Length < 20 ||
                !_apiKey.StartsWith("gsk_"))
            {
                throw new InvalidOperationException(
                    "Clé API Groq non configurée.\n\n" +
                    "Allez dans Paramètres → Clé API Groq\n" +
                    "et collez votre clé depuis console.groq.com");
            }
        }

        
        /// <summary>Retourne la température adaptée au niveau de difficulté.</summary>
        public static double TemperaturePourDifficulte(string difficulte) => difficulte switch
        {
            "Facile"    => 0.4,  // précis, factuel, peu de créativité
            "Difficile" => 0.8,  // nuancé, analytique, plus créatif
            _           => 0.65  // Moyen
        };

        public async Task<(string Texte, TimeSpan Duree)> GenererAsync(
            string prompt,
            CancellationToken ct = default,
            double temperature = 0.65)
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
                temperature,
                max_tokens = 2000,
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
        /// Envoie un prompt avec retry en cas d'erreur 429 (TPM). Utilise le délai indiqué par Groq
        /// (« try again in Xs ») ou un backoff progressif.
        /// </summary>
        public async Task<string> GenererAvecRetryAsync(string prompt, CancellationToken ct, double temperature = 0.65)
        {
            const int MAX_TENTATIVES = 6;

            for (int tentative = 1; tentative <= MAX_TENTATIVES; tentative++)
            {
                try
                {
                    var (texte, _) = await GenererAsync(prompt, ct, temperature);
                    return texte;
                }
                catch (HttpRequestException ex) when (MessageIndiqueQuotaGroq(ex.Message))
                {
                    if (tentative >= MAX_TENTATIVES)
                        throw;

                    int delayMs = CalculerDelaiRetryMsApres429(ex.Message, tentative);
                    Debug.WriteLine(
                        $"[GroqService] 429 TPM — attente {delayMs} ms avant tentative {tentative + 1}/{MAX_TENTATIVES}");
                    await Task.Delay(delayMs, ct);
                }
            }

            throw new HttpRequestException("La génération a échoué. Le quota de requêtes est temporairement atteint — réessayez dans quelques minutes.");
        }

        private static bool MessageIndiqueQuotaGroq(string message) =>
            message.Contains("429", StringComparison.Ordinal)
            || message.Contains("Rate limit", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Quota Groq", StringComparison.OrdinalIgnoreCase);

        /// <summary>Délai conseillé par l’API (« try again in 2.64s ») + marge, sinon backoff.</summary>
        private static int CalculerDelaiRetryMsApres429(string message, int tentative)
        {
            if (!string.IsNullOrEmpty(message))
            {
                var m = Regex.Match(message, @"try\s+again\s+in\s+([0-9.]+)\s*s", RegexOptions.IgnoreCase);
                if (m.Success
                    && double.TryParse(
                        m.Groups[1].Value,
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out var secondes))
                {
                    int ms = (int)Math.Ceiling(secondes * 1000) + 800;
                    return Math.Clamp(ms, 3000, 90_000);
                }
            }

            int[] backoff = { 8000, 14_000, 22_000, 35_000, 50_000, 65_000 };
            int idx = Math.Clamp(tentative - 1, 0, backoff.Length - 1);
            return backoff[idx];
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
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

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
                    "Impossible de contacter le service de génération.\nVérifiez votre connexion internet.");
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

                    // Détecter une réponse tronquée par max_tokens
                    if (firstChoice.TryGetProperty("finish_reason", out var finishReason) &&
                        finishReason.GetString() == "length")
                    {
                        Debug.WriteLine("[GroqService] ⚠️ Réponse tronquée (finish_reason=length) — JSON incomplet possible");
                    }

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
                        401 => "Clé API invalide ou révoquée.\n\nRendez-vous dans Paramètres pour configurer une nouvelle clé.",

                        429 => "Quota temporairement dépassé.\nLa génération réessaie automatiquement — cela peut prendre quelques secondes.",

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

        /// <summary>Délai entre deux lots API — à garder aligné avec la logique anti-429 (quiz / examen).</summary>
        public static int DelaiEntreLotsMs => DELAI_ENTRE_LOTS_MS;

        
        public static int TailleContexteParLot => TAILLE_CONTEXTE_PAR_LOT;

        public const int MAX_QUESTIONS_PAR_APPEL = 4;

        
        public const int LIMITE_CONTENU_CHARS = TAILLE_CONTEXTE_PAR_LOT;

       
        public static string BuildPromptQcmLot(string contenuCours, int nbQuestions, int numeroDepart, string difficulte = "Moyen", IReadOnlyList<string>? avoid = null)
        {
            string difficultyInstructions = GetDifficultyInstructions(difficulte);
            string avoidSection = BuildAvoidSection(avoid);
            string variationSection = BuildVariationSection();
            string pedagogicalSection = BuildPedagogicalSection(difficulte);
            return $@"Generate EXACTLY {nbQuestions} multiple-choice questions (QCM) from this course content.
Start question numbering at {numeroDepart}.

{difficultyInstructions}
{variationSection}
{pedagogicalSection}
IMPORTANT: Base ALL questions STRICTLY on the provided course content below. Do NOT invent or assume any information not explicitly mentioned in the text.
{avoidSection}
Course content:
{contenuCours}

Return ONLY a JSON array of EXACTLY {nbQuestions} objects. Each object must have:
- ""type"": ""QCM""
- ""enonce"": the question text (in the same language as the course)
- ""optionA"", ""optionB"", ""optionC"", ""optionD"": 4 distinct answer choices
- ""reponseCorrecte"": exactly one of ""A"", ""B"", ""C"", or ""D""
- ""explication"": brief explanation of the correct answer

RULES:
1. Start with [ — nothing before
2. End with ] — nothing after
3. EXACTLY {nbQuestions} objects in the array — no more, no less
4. All 4 options must be distinct and plausible
5. Strictly respect the difficulty level
Output ONLY the JSON array, nothing else.";
        }

        private static string BuildAvoidSection(IReadOnlyList<string>? avoid)
        {
            if (avoid == null || avoid.Count == 0) return string.Empty;
            var lines = avoid
                .TakeLast(15)
                .Select((e, i) => $"{i + 1}. \"{(e.Length > 90 ? e[..90] + "…" : e)}\"");
            return
                "\nALREADY GENERATED — do NOT repeat, rephrase or paraphrase these questions:\n" +
                string.Join("\n", lines) +
                "\nGenerate COMPLETELY DIFFERENT questions covering other aspects of the content.\n";
        }

        private static string BuildVariationSection() =>
            "VARIATION REQUIREMENTS:\n" +
            $"- Session identifier: {Guid.NewGuid()}\n" +
            "- Vary the generated questions compared to previous generations.\n" +
            "- Ne répète pas les mêmes questions. Génère des questions différentes à chaque appel, en explorant des angles variés du contenu.\n";

        private static string BuildPedagogicalSection(string difficulte) =>
            "PEDAGOGICAL QUALITY REQUIREMENTS:\n" +
            "- Each question must target one clear learning objective.\n" +
            "- Keep wording explicit and avoid ambiguous wording.\n" +
            "- Distractors should reflect plausible misconceptions.\n" +
            "- Add a concise explanation linked to the course text.\n" +
            (difficulte == "Facile"
                ? "- Cognitive mix: mostly comprehension, some simple application.\n"
                : difficulte == "Difficile"
                    ? "- Cognitive mix: mostly application/reasoning, some comprehension.\n"
                    : "- Cognitive mix: balanced comprehension and application.\n");

        /// <summary>
        /// Construit le prompt pour un lot de questions d'examen mixtes (QCM + Checkbox + Rédaction).
        /// </summary>
        private static string GetDifficultyInstructions(string difficulte) => difficulte switch
        {
            "Facile" =>
                "DIFFICULTY: EASY (Facile)\n" +
                "- Ask about basic definitions and simple facts directly stated in the text\n" +
                "- Questions should test recall and recognition only\n" +
                "- Wrong answers (distractors) should be clearly incorrect and easy to eliminate\n" +
                "- Open answers (REDACTION) should be short and factual (1-2 sentences)\n" +
                "- Use simple, short sentences\n" +
                "- Example style: 'Qu'est-ce que X ?' / 'Quel est le rôle de Y ?'",

            "Difficile" =>
                "DIFFICULTY: HARD (Difficile)\n" +
                "- Ask about subtle distinctions, implicit relationships, and advanced reasoning\n" +
                "- Questions should require deep understanding and critical analysis\n" +
                "- Wrong answers (distractors) must be plausible and require careful thinking to eliminate\n" +
                "- For CHECKBOX, use 2 or 3 correct answers; prefer 3 when the content allows it\n" +
                "- Open answers (REDACTION) should require detailed explanation, analysis or comparison (3-5 sentences)\n" +
                "- Include questions about causes, consequences, comparisons, and exceptions\n" +
                "- Example style: 'Pourquoi X entraîne-t-il Y dans le contexte de Z ?' / 'Quelle distinction fondamentale existe entre A et B ?'",

            _ => // Moyen (défaut)
                "DIFFICULTY: MEDIUM (Moyen)\n" +
                "- Ask about concepts that require understanding, not just memorization\n" +
                "- Questions should test comprehension and application of ideas\n" +
                "- Wrong answers (distractors) should be plausible but clearly wrong upon reflection\n" +
                "- Open answers (REDACTION) should require a structured explanation (2-3 sentences)\n" +
                "- Mix factual and conceptual questions\n" +
                "- Example style: 'Comment fonctionne X ?' / 'Quel est l'effet de Y sur Z ?'"
        };

        public static string BuildPromptExamenLot(
            string contenuCours,
            int nbQCM, int nbVF, int nbCheckbox, int nbRedaction,
            string difficulte,
            int numeroDepart,
            IReadOnlyList<string>? avoid = null)
        {
            string difficultyInstructions = GetDifficultyInstructions(difficulte);
            string avoidSection = BuildAvoidSection(avoid);
            string variationSection = BuildVariationSection();
            string pedagogicalSection = BuildPedagogicalSection(difficulte);
            int total = nbQCM + nbVF + nbCheckbox + nbRedaction;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"Generate EXACTLY {total} exam questions from this course content. Start numbering at {numeroDepart}.");
            sb.AppendLine($"MANDATORY BREAKDOWN — you MUST generate EXACTLY: {nbQCM} questions of type QCM, {nbVF} questions of type VF, {nbCheckbox} questions of type CHECKBOX, {nbRedaction} questions of type REDACTION. No substitutions allowed.");
            sb.AppendLine();
            sb.AppendLine(difficultyInstructions);
            sb.AppendLine();
            sb.AppendLine(variationSection);
            sb.AppendLine(pedagogicalSection);
            sb.AppendLine("CRITICAL: Base ALL questions STRICTLY on the provided course content. Do NOT invent or assume any information not explicitly mentioned in the text.");
            if (!string.IsNullOrEmpty(avoidSection)) sb.AppendLine(avoidSection);
            sb.AppendLine("Course content:");
            sb.AppendLine(contenuCours);
            sb.AppendLine();
            sb.AppendLine("Return ONLY a JSON array of EXACTLY " + total + " objects. Use ONLY these formats:");
            sb.AppendLine();
            sb.AppendLine("QCM format (use for EXACTLY " + nbQCM + " questions, exactly 1 correct answer):");
            sb.AppendLine("{\"type\":\"QCM\",\"enonce\":\"Question?\",\"optionA\":\"...\",\"optionB\":\"...\",\"optionC\":\"...\",\"optionD\":\"...\",\"reponseCorrecte\":\"A\",\"explication\":\"...\"}");
            if (nbVF > 0)
            {
                sb.AppendLine();
                sb.AppendLine("VF format (use for EXACTLY " + nbVF + " questions, only 'Vrai' and 'Faux' options):");
                sb.AppendLine("{\"type\":\"VF\",\"enonce\":\"Question?\",\"optionA\":\"Vrai\",\"optionB\":\"Faux\",\"reponseCorrecte\":\"A\",\"explication\":\"...\"}");
                sb.AppendLine("Note: reponseCorrecte must be \"A\" for Vrai or \"B\" for Faux.");
            }
            if (nbCheckbox > 0)
            {
                sb.AppendLine();
                sb.AppendLine("CHECKBOX format (use for EXACTLY " + nbCheckbox + " questions, 1 to 4 correct answers — choose the most appropriate number for each question):");
                sb.AppendLine("{\"type\":\"CHECKBOX\",\"enonce\":\"Question?\",\"optionA\":\"...\",\"optionB\":\"...\",\"optionC\":\"...\",\"optionD\":\"...\",\"reponsesCorrectes\":[\"A\",\"C\"],\"explication\":\"...\"}");
                sb.AppendLine("Note: reponsesCorrectes is an array of 1 to 4 letters (e.g. [\"B\"], [\"A\",\"C\"], [\"A\",\"B\",\"D\"])");
            }
            if (nbRedaction > 0)
            {
                sb.AppendLine();
                sb.AppendLine("REDACTION format (use for EXACTLY " + nbRedaction + " questions, open answer requiring a written response):");
                sb.AppendLine("{\"type\":\"REDACTION\",\"enonce\":\"Question?\",\"reponseModele\":\"Complete detailed model answer based on the course...\",\"explication\":\"...\"}");
            }
            sb.AppendLine();
            sb.AppendLine("RULES:");
            sb.AppendLine("1. Start with [ — nothing before");
            sb.AppendLine("2. End with ] — nothing after");
            sb.AppendLine($"3. EXACTLY {total} objects total: {nbQCM} QCM + {nbVF} VF + {nbCheckbox} CHECKBOX + {nbRedaction} REDACTION");
            sb.AppendLine("4. \"type\" field must be exactly \"QCM\", \"VF\", \"CHECKBOX\", or \"REDACTION\"");
            sb.AppendLine("5. Same language as the course content");
            sb.AppendLine("6. Strictly respect the difficulty level");
            sb.AppendLine("7. All options (A/B/C/D) must be distinct and plausible");
            sb.AppendLine("Output ONLY the JSON array, nothing else.");

            return sb.ToString();
        }

    
        public static string BuildPromptExamen(
            string contenuCours,
            int nbQCM, int nbVF, int nbCheckbox, int nbRedaction,
            string difficulte)
        {
            return BuildPromptExamenLot(contenuCours, nbQCM, nbVF, nbCheckbox, nbRedaction, difficulte, 1);
        }
    }
}