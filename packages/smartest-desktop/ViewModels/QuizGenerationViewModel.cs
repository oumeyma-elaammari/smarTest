using DocumentFormat.OpenXml.Packaging;
using Microsoft.Win32;
using smartest_desktop.Data;
using smartest_desktop.Helpers;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace smartest_desktop.ViewModels
{
    // ═══════════════════════════════════════════════════════════════════════════
    // QuestionQCM
    // ═══════════════════════════════════════════════════════════════════════════
    public class QuestionQCM : BaseViewModel
    {
        private string _enonce = string.Empty;
        public string Enonce { get => _enonce; set => SetProperty(ref _enonce, value); }

        private string _optionA = string.Empty;
        public string OptionA { get => _optionA; set => SetProperty(ref _optionA, value); }

        private string _optionB = string.Empty;
        public string OptionB { get => _optionB; set => SetProperty(ref _optionB, value); }

        private string _optionC = string.Empty;
        public string OptionC { get => _optionC; set => SetProperty(ref _optionC, value); }

        private string _optionD = string.Empty;
        public string OptionD { get => _optionD; set => SetProperty(ref _optionD, value); }

        private string _reponseCorrecte = string.Empty;
        public string ReponseCorrecte { get => _reponseCorrecte; set => SetProperty(ref _reponseCorrecte, value); }

        private string _explication = string.Empty;
        public string Explication { get => _explication; set => SetProperty(ref _explication, value); }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set { SetProperty(ref _isEditing, value); OnPropertyChanged(nameof(IsNotEditing)); }
        }

        public bool IsNotEditing => !_isEditing;
        public int Numero { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // QuizGenerationViewModel
    //
    // Flux :
    //   1. Prof importe un fichier PDF / DOCX / TXT directement dans l'interface
    //   2. Contenu extrait localement — affiché et éditable
    //   3. Prof configure titre, nombre de questions, difficulté
    //   4. Clic "Générer" → Ollama streaming → ParseQCM → QuizGenereAvecSucces
    // ═══════════════════════════════════════════════════════════════════════════
    public class QuizGenerationViewModel : BaseViewModel
    {
        private readonly LocalDbContext _db;
        private static readonly HttpClient _http = new() { Timeout = Timeout.InfiniteTimeSpan };
        private CancellationTokenSource? _cts;

        // ── Cours importé ─────────────────────────────────────────────────────

        private string _titreCours = string.Empty;
        public string TitreCours
        {
            get => _titreCours;
            set
            {
                SetProperty(ref _titreCours, value);
                OnPropertyChanged(nameof(HasCours));
                ((RelayCommand)GenererCommand).RaiseCanExecuteChanged();
            }
        }

        private string _contenuCours = string.Empty;
        /// <summary>Texte brut extrait — éditable, ne transite JAMAIS vers le backend.</summary>
        public string ContenuCours
        {
            get => _contenuCours;
            set
            {
                SetProperty(ref _contenuCours, value);
                OnPropertyChanged(nameof(HasCours));
                OnPropertyChanged(nameof(NombreCaracteres));
                ((RelayCommand)GenererCommand).RaiseCanExecuteChanged();
            }
        }

        private string _nomFichier = string.Empty;
        public string NomFichier
        {
            get => _nomFichier;
            set => SetProperty(ref _nomFichier, value);
        }

        private string _typeFichier = string.Empty;
        public string TypeFichier
        {
            get => _typeFichier;
            set => SetProperty(ref _typeFichier, value);
        }

        public bool HasCours => !string.IsNullOrWhiteSpace(ContenuCours);
        public bool HasNoCours => !HasCours;

        public string NombreCaracteres =>
            HasCours ? $"{ContenuCours.Length:N0} caractères" : string.Empty;

        // ── Mode édition contenu ──────────────────────────────────────────────

        private bool _isEditingContenu;
        public bool IsEditingContenu
        {
            get => _isEditingContenu;
            set { SetProperty(ref _isEditingContenu, value); OnPropertyChanged(nameof(IsNotEditingContenu)); }
        }
        public bool IsNotEditingContenu => !_isEditingContenu;

        // ── Paramètres quiz ───────────────────────────────────────────────────

        private string _titreQuiz = string.Empty;
        public string TitreQuiz
        {
            get => _titreQuiz;
            set => SetProperty(ref _titreQuiz, value);
        }

        private int _nombreQuestions = 3;
        public int NombreQuestions
        {
            get => _nombreQuestions;
            set => SetProperty(ref _nombreQuestions, value);
        }

        private string _difficulte = "Moyen";
        public string Difficulte
        {
            get => _difficulte;
            set => SetProperty(ref _difficulte, value);
        }

        public List<int> NombresQuestions { get; } = new() { 3, 5, 7, 10, 15, 20 };

        // ── État ──────────────────────────────────────────────────────────────

        private bool _isImporting;
        public bool IsImporting
        {
            get => _isImporting;
            set { SetProperty(ref _isImporting, value); OnPropertyChanged(nameof(IsNotImporting)); }
        }
        public bool IsNotImporting => !_isImporting;

        private bool _isGenerating;
        public bool IsGenerating
        {
            get => _isGenerating;
            set
            {
                SetProperty(ref _isGenerating, value);
                OnPropertyChanged(nameof(IsNotGenerating));
                ((RelayCommand)GenererCommand).RaiseCanExecuteChanged();
                ((RelayCommand)AnnulerGenerationCommand).RaiseCanExecuteChanged();
            }
        }
        public bool IsNotGenerating => !_isGenerating;

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { SetProperty(ref _statusMessage, value); OnPropertyChanged(nameof(HasStatus)); }
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); }
        }

        public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // ── Commandes ─────────────────────────────────────────────────────────

        public ICommand ImporterFichierCommand { get; }
        public ICommand EffacerContenuCommand { get; }
        public ICommand ToggleEditionContenuCommand { get; }
        public ICommand SetDifficulteCommand { get; }
        public ICommand GenererCommand { get; }
        public ICommand AnnulerGenerationCommand { get; }
        public ICommand AnnulerCommand { get; }

        // ── Événements ────────────────────────────────────────────────────────

        public event Action<List<QuestionQCM>, string, string, int, string?>? QuizGenereAvecSucces;
        public event Action? NavigationAnnulee;

        // ── Constructeur ──────────────────────────────────────────────────────

        public QuizGenerationViewModel()
        {
            _db = App.LocalDb;

            ImporterFichierCommand = new RelayCommand(
                async _ => await ImporterFichierAsync(),
                _ => !IsImporting && !IsGenerating);

            EffacerContenuCommand = new RelayCommand(
                _ => EffacerContenu(),
                _ => HasCours && !IsGenerating);

            ToggleEditionContenuCommand = new RelayCommand(
                _ => IsEditingContenu = !IsEditingContenu,
                _ => HasCours && !IsGenerating);

            SetDifficulteCommand = new RelayCommand(param =>
            {
                if (param is string niveau) Difficulte = niveau;
            });

            GenererCommand = new RelayCommand(
                async _ => await GenererQuizAsync(),
                _ => HasCours && IsNotGenerating);

            AnnulerGenerationCommand = new RelayCommand(
                _ =>
                {
                    _cts?.Cancel();
                    StatusMessage = "⛔ Génération annulée.";
                    ErrorMessage = string.Empty;
                    IsGenerating = false;
                },
                _ => IsGenerating);

            AnnulerCommand = new RelayCommand(_ =>
            {
                _cts?.Cancel();
                NavigationAnnulee?.Invoke();
            });
        }

        // ══════════════════════════════════════════════════════════════════════
        // IMPORT FICHIER
        // ══════════════════════════════════════════════════════════════════════

        private async Task ImporterFichierAsync()
        {
            var dlg = new OpenFileDialog
            {
                Title = "Importer un cours",
                Filter = "Fichiers supportés (*.pdf;*.docx;*.txt)|*.pdf;*.docx;*.txt" +
                         "|PDF (*.pdf)|*.pdf|Word (*.docx)|*.docx|Texte (*.txt)|*.txt",
                Multiselect = false
            };

            if (dlg.ShowDialog() != true) return;

            string chemin = dlg.FileName;
            string extension = Path.GetExtension(chemin).ToLowerInvariant();

            IsImporting = true;
            ErrorMessage = string.Empty;
            StatusMessage = "📂 Extraction du contenu en cours...";

            try
            {
                string contenu = await Task.Run(() => ExtraireContenu(chemin, extension));

                if (string.IsNullOrWhiteSpace(contenu))
                {
                    ErrorMessage = "❌ Le fichier est vide ou le contenu n'a pas pu être extrait.";
                    StatusMessage = string.Empty;
                    return;
                }

                NomFichier = Path.GetFileName(chemin);
                TypeFichier = extension.TrimStart('.').ToUpperInvariant();
                TitreCours = Path.GetFileNameWithoutExtension(chemin);

                if (string.IsNullOrWhiteSpace(TitreQuiz))
                    TitreQuiz = $"Quiz — {TitreCours}";

                ContenuCours = contenu;
                IsEditingContenu = false;
                StatusMessage = $"✅ Cours importé ({NombreCaracteres})";

                _ = Task.Delay(2500).ContinueWith(_ =>
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (!IsGenerating) StatusMessage = string.Empty;
                    }));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"❌ Erreur lors de l'import : {ex.Message}";
                StatusMessage = string.Empty;
            }
            finally
            {
                IsImporting = false;
            }
        }

        // ── Extraction selon le format ────────────────────────────────────────

        private static string ExtraireContenu(string chemin, string extension) => extension switch
        {
            ".txt" => File.ReadAllText(chemin, Encoding.UTF8),
            ".pdf" => ExtrairePdf(chemin),
            ".docx" => ExtraireDocx(chemin),
            _ => throw new NotSupportedException($"Format non supporté : {extension}")
        };

        private static string ExtrairePdf(string chemin)
        {
            using var doc = UglyToad.PdfPig.PdfDocument.Open(chemin);
            var sb = new StringBuilder();
            foreach (var page in doc.GetPages())
                sb.AppendLine(page.Text);
            return sb.ToString().Trim();
        }

        private static string ExtraireDocx(string chemin)
        {
            using var wordDoc = WordprocessingDocument.Open(chemin, isEditable: false);
            return wordDoc.MainDocumentPart?.Document.Body?.InnerText ?? string.Empty;
        }

        private void EffacerContenu()
        {
            var result = MessageBox.Show(
                "Effacer le contenu importé ?\nVous devrez réimporter un fichier.",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            ContenuCours = string.Empty;
            TitreCours = string.Empty;
            NomFichier = string.Empty;
            TypeFichier = string.Empty;
            TitreQuiz = string.Empty;
            IsEditingContenu = false;
            StatusMessage = string.Empty;
            ErrorMessage = string.Empty;
        }

        // ══════════════════════════════════════════════════════════════════════
        // GÉNÉRATION OLLAMA
        // ══════════════════════════════════════════════════════════════════════

        private async Task GenererQuizAsync()
        {
            if (!HasCours) return;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            IsGenerating = true;
            ErrorMessage = string.Empty;
            StatusMessage = "🤖 Connexion à Ollama...";

            try
            {
                await VerifierOllamaAsync(token);

                string modele = await DetecterModeleAsync(token);
                StatusMessage = $"✅ Modèle : {modele}";
                await Task.Delay(300, token);

                string contenu = ContenuCours;
                int limite = NombreQuestions <= 3 ? 1500 :
                             NombreQuestions <= 5 ? 2500 : 4000;
                if (contenu.Length > limite) contenu = contenu[..limite];

                StatusMessage = $"🧠 Génération ({NombreQuestions} questions, niveau {Difficulte})...";

                string reponse = await AppelOllamaStreamingAsync(modele, BuildPrompt(contenu), token);

                if (string.IsNullOrWhiteSpace(reponse))
                    throw new Exception("Ollama n'a retourné aucun texte.");

                System.Diagnostics.Debug.WriteLine("=== OLLAMA RAW ===");
                System.Diagnostics.Debug.WriteLine(reponse[..Math.Min(500, reponse.Length)]);

                StatusMessage = "📝 Extraction des questions...";
                var questions = ParseQCM(reponse);

                if (questions.Count == 0)
                    throw new Exception(
                        "Le modèle n'a pas produit de JSON valide.\n\n" +
                        "Conseil : réduisez le nombre de questions ou raccourcissez le texte.");

                string titre = string.IsNullOrWhiteSpace(TitreQuiz)
                    ? $"Quiz — {TitreCours}"
                    : TitreQuiz.Trim();

                StatusMessage = $"✅ {questions.Count} questions générées !";
                await Task.Delay(400, token);

                QuizGenereAvecSucces?.Invoke(questions, titre, Difficulte, NombreQuestions, TitreCours);
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "⛔ Génération annulée.";
                ErrorMessage = string.Empty;
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = $"❌ Ollama inaccessible.\n\nLancez : ollama serve\n\nDétail : {ex.Message}";
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"❌ {ex.Message}";
                StatusMessage = string.Empty;
            }
            finally
            {
                IsGenerating = false;
            }
        }

        // ── Streaming ─────────────────────────────────────────────────────────

        private async Task<string> AppelOllamaStreamingAsync(
            string modele, string prompt, CancellationToken token)
        {
            var body = new
            {
                model = modele,
                prompt,
                stream = true,
                options = new { temperature = 0.1, num_predict = 2048, num_ctx = 4096, top_k = 5, top_p = 0.3 }
            };

            var req = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/generate")
            {
                Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
            };

            var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, token);
            res.EnsureSuccessStatusCode();

            var sb = new StringBuilder();
            int tokenCount = 0;

            await using var stream = await res.Content.ReadAsStreamAsync(token);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (!reader.EndOfStream)
            {
                token.ThrowIfCancellationRequested();
                string? line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("response", out var rt))
                    {
                        sb.Append(rt.GetString() ?? "");
                        if (++tokenCount % 20 == 0)
                            StatusMessage = $"🧠 Génération... ({sb.Length} caractères)";
                    }

                    if (root.TryGetProperty("done", out var done) && done.GetBoolean()) break;
                }
                catch (JsonException) { }
            }

            return sb.ToString();
        }

        // ── Détection modèle ──────────────────────────────────────────────────

        private async Task<string> DetecterModeleAsync(CancellationToken token)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token);

                var res = await _http.GetAsync("http://localhost:11434/api/tags", linked.Token);
                var json = await res.Content.ReadAsStringAsync(linked.Token);
                using var doc = JsonDocument.Parse(json);

                string[] preference =
                {
                    "mistral", "llama3.2:3b", "llama3.2", "llama3",
                    "llama2",  "phi3",         "phi3:mini", "phi",
                    "gemma:2b","gemma"
                };

                if (doc.RootElement.TryGetProperty("models", out var models))
                {
                    var names = models.EnumerateArray()
                        .Select(m => m.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "")
                        .Where(n => n.Length > 0)
                        .ToList();

                    System.Diagnostics.Debug.WriteLine($"[Ollama] Modèles : {string.Join(", ", names)}");

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

        // ── Ping Ollama ───────────────────────────────────────────────────────

        private async Task VerifierOllamaAsync(CancellationToken token)
        {
            try
            {
                using var ping = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, ping.Token);
                await _http.GetAsync("http://localhost:11434/", linked.Token);
            }
            catch (OperationCanceledException)
            {
                throw new HttpRequestException("Ollama ne répond pas.\nLancez : ollama serve");
            }
            catch (HttpRequestException)
            {
                throw new HttpRequestException("Ollama n'est pas démarré sur le port 11434.\nLancez : ollama serve");
            }
        }

        // ── Prompt ────────────────────────────────────────────────────────────

        private string BuildPrompt(string contenu) =>
$@"Tu es un générateur de QCM. Réponds UNIQUEMENT avec un tableau JSON, sans aucun texte avant ou après.

Génère exactement {NombreQuestions} questions QCM en français. Niveau de difficulté : {Difficulte}.

Texte source :
{contenu}

FORMAT JSON OBLIGATOIRE — commence par [ et termine par ] :
[
  {{""enonce"":""Question ?"",""optionA"":""Réponse A"",""optionB"":""Réponse B"",""optionC"":""Réponse C"",""optionD"":""Réponse D"",""reponseCorrecte"":""A"",""explication"":""Explication courte""}}
]

RÈGLES ABSOLUES :
- Commence DIRECTEMENT par le caractère [
- Termine DIRECTEMENT par le caractère ]
- Pas de ```json, pas d'introduction, pas de conclusion
- reponseCorrecte doit être exactement A, B, C ou D
- Génère exactement {NombreQuestions} objets dans le tableau";

        // ── Parser JSON robuste ───────────────────────────────────────────────

        private static List<QuestionQCM> ParseQCM(string texte)
        {
            var questions = new List<QuestionQCM>();
            try
            {
                // Supprimer les balises ```json``` que certains modèles ajoutent
                texte = Regex.Replace(texte, @"```json|```", "", RegexOptions.IgnoreCase).Trim();

                int debut = texte.IndexOf('[');
                int fin = texte.LastIndexOf(']');

                if (debut == -1 || fin == -1 || fin <= debut)
                {
                    System.Diagnostics.Debug.WriteLine("[ParseQCM] Aucun tableau JSON trouvé.");
                    return questions;
                }

                string jsonPart = texte[debut..(fin + 1)].Replace("\r", "").Replace("\t", " ");

                // Tentative 1 : parse direct
                var items = TryDeserialize(jsonPart);

                // Tentative 2 : réparer JSON tronqué
                if (items == null)
                {
                    int dernierObjet = jsonPart.LastIndexOf('}');
                    if (dernierObjet > 0)
                        items = TryDeserialize(jsonPart[..(dernierObjet + 1)] + "]");
                }

                if (items == null)
                {
                    System.Diagnostics.Debug.WriteLine("[ParseQCM] Échec total du parsing.");
                    return questions;
                }

                int n = 1;
                foreach (var item in items)
                {
                    try
                    {
                        var q = new QuestionQCM
                        {
                            Numero = n++,
                            Enonce = Str(item, "enonce"),
                            OptionA = Str(item, "optionA"),
                            OptionB = Str(item, "optionB"),
                            OptionC = Str(item, "optionC"),
                            OptionD = Str(item, "optionD"),
                            ReponseCorrecte = Str(item, "reponseCorrecte").ToUpper().Trim(),
                            Explication = Str(item, "explication")
                        };

                        if (!string.IsNullOrWhiteSpace(q.Enonce) &&
                            !string.IsNullOrWhiteSpace(q.OptionA) &&
                            !string.IsNullOrWhiteSpace(q.OptionB) &&
                            !string.IsNullOrWhiteSpace(q.ReponseCorrecte) &&
                            "ABCD".Contains(q.ReponseCorrecte))
                            questions.Add(q);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ParseQCM] Erreur : {ex.Message}");
            }

            System.Diagnostics.Debug.WriteLine($"[ParseQCM] {questions.Count} questions extraites.");
            return questions;
        }

        private static List<JsonElement>? TryDeserialize(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<List<JsonElement>>(
                    json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch { return null; }
        }

        private static string Str(JsonElement el, string key)
        {
            if (el.TryGetProperty(key, out var p)) return p.GetString() ?? string.Empty;
            foreach (var prop in el.EnumerateObject())
                if (string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase))
                    return prop.Value.GetString() ?? string.Empty;
            return string.Empty;
        }
    }
}