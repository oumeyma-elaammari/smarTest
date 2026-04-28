using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace smartest_desktop.Services
{
    /// <summary>
    /// Service centralisé pour toutes les interactions avec Ollama (localhost:11434).
    /// Utilisé par les ViewModels d'examen ; le ViewModel quiz garde sa logique inline.
    /// </summary>
    public class OllamaService
    {
        private static readonly HttpClient _http = new()
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        private const string BaseUrl = "http://localhost:11434";

        // ── Vérification ─────────────────────────────────────────────────────

        public async Task VerifierAsync(CancellationToken ct = default)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);
                await _http.GetAsync(BaseUrl + "/", linked.Token);
            }
            catch (OperationCanceledException)
            {
                throw new HttpRequestException(
                    "Ollama ne répond pas en 5 secondes.\nLancez : ollama serve");
            }
            catch (HttpRequestException)
            {
                throw new HttpRequestException(
                    "Ollama n'est pas démarré sur le port 11434.\nLancez : ollama serve");
            }
        }

        // ── Détection du modèle ──────────────────────────────────────────────

        public async Task<string> DetecterModeleAsync(CancellationToken ct = default)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, cts.Token);

                var res  = await _http.GetAsync(BaseUrl + "/api/tags", linked.Token);
                var json = await res.Content.ReadAsStringAsync(linked.Token);

                using var doc = JsonDocument.Parse(json);

                string[] preference = {
                    "qwen3:1.7b", "qwen3",
                    "gemma2:2b", "gemma2",
                    "phi3:mini", "phi3", "phi",
                    "llama3.2:1b", "llama3.2:3b", "llama3.2",
                    "mistral", "llama3", "llama2",
                    "gemma:2b", "gemma"
                };

                if (doc.RootElement.TryGetProperty("models", out var models))
                {
                    var names = new List<string>();
                    foreach (var m in models.EnumerateArray())
                        if (m.TryGetProperty("name", out var n))
                            names.Add(n.GetString() ?? "");

                    foreach (var pref in preference)
                        foreach (var name in names)
                            if (name.StartsWith(pref, StringComparison.OrdinalIgnoreCase))
                                return name;

                    if (names.Count > 0) return names[0];
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DetecterModele] {ex.Message}");
            }

            return "mistral";
        }

        // ── Appel streaming ──────────────────────────────────────────────────

        /// <summary>
        /// Appelle Ollama en mode streaming et retourne le texte complet généré.
        /// <paramref name="onProgress"/> reçoit le texte accumulé toutes les 20 tokens.
        /// </summary>
        public async Task<string> GenererStreamingAsync(
            string modele,
            string prompt,
            Action<string>? onProgress = null,
            CancellationToken ct = default)
        {
            var requestBody = new
            {
                model  = modele,
                prompt = prompt,
                stream = true,
                options = new
                {
                    temperature = 0.1,
                    num_predict = 2048,
                    num_ctx     = 4096,
                    top_k       = 5,
                    top_p       = 0.3
                }
            };

            var json    = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, BaseUrl + "/api/generate")
            {
                Content = content
            };

            var response = await _http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                ct);

            response.EnsureSuccessStatusCode();

            var sb         = new StringBuilder();
            int tokenCount = 0;

            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (!reader.EndOfStream)
            {
                ct.ThrowIfCancellationRequested();

                string? line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    using var lineDoc = JsonDocument.Parse(line);
                    var root = lineDoc.RootElement;

                    if (root.TryGetProperty("response", out var tok))
                    {
                        sb.Append(tok.GetString() ?? "");
                        tokenCount++;

                        if (tokenCount % 20 == 0)
                            onProgress?.Invoke(sb.ToString());
                    }

                    if (root.TryGetProperty("done", out var done) && done.GetBoolean())
                        break;
                }
                catch (JsonException) { }
            }

            return sb.ToString();
        }

        // ── Prompt examen ────────────────────────────────────────────────────

        public string BuildPromptExamen(
            string contenu,
            int nbQCM,
            int nbCheckbox,
            int nbRedaction,
            string difficulte)
        {
            // Prompt volontairement court et en anglais — les petits modèles (phi3, llama3.2)
            // suivent mieux les instructions en anglais et produisent moins de texte parasite.
            var sb = new StringBuilder();

            int total = nbQCM + nbCheckbox + nbRedaction;

            sb.AppendLine($"Create {total} exam questions in FRENCH based on the text below. Difficulty: {difficulte}.");
            sb.AppendLine();

            if (nbQCM > 0)
                sb.AppendLine($"- {nbQCM} questions with type=\"QCM\" : single correct answer among optionA/B/C/D, field reponseCorrecte = \"A\"/\"B\"/\"C\"/\"D\".");
            if (nbCheckbox > 0)
                sb.AppendLine($"- {nbCheckbox} questions with type=\"CHECKBOX\" : multiple correct answers, field reponsesCorrectes = [\"A\",\"C\"] etc.");
            if (nbRedaction > 0)
                sb.AppendLine($"- {nbRedaction} questions with type=\"REDACTION\" : open question, field reponseModele = model answer. No options needed.");

            sb.AppendLine();
            sb.AppendLine("Text:");
            sb.AppendLine(contenu);
            sb.AppendLine();
            sb.AppendLine("Return ONLY a JSON array, no extra text, no markdown:");

            // Un seul exemple par type sélectionné, sans virgule finale
            var exemples = new List<string>();
            if (nbQCM > 0)
                exemples.Add(@"{""type"":""QCM"",""enonce"":""..."",""optionA"":""..."",""optionB"":""..."",""optionC"":""..."",""optionD"":""..."",""reponseCorrecte"":""A"",""explication"":""...""}");
            if (nbCheckbox > 0)
                exemples.Add(@"{""type"":""CHECKBOX"",""enonce"":""..."",""optionA"":""..."",""optionB"":""..."",""optionC"":""..."",""optionD"":""..."",""reponsesCorrectes"":[""A"",""C""],""explication"":""...""}");
            if (nbRedaction > 0)
                exemples.Add(@"{""type"":""REDACTION"",""enonce"":""..."",""reponseModele"":""..."",""explication"":""...""}");

            sb.AppendLine("[" + string.Join(",", exemples) + "]");

            return sb.ToString();
        }
    }
}
