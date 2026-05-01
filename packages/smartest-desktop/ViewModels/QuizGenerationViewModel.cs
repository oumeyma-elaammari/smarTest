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
using WpfApp = System.Windows.Application;

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

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

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

        public string Nom => WpfApp.Current.Properties["Nom"]?.ToString() ?? "Professeur";
        public string Email => WpfApp.Current.Properties["Email"]?.ToString() ?? "";

        // ── Commandes ─────────────────────────────────────────────────────────

        public ICommand ImporterFichierCommand { get; }
        public ICommand EffacerContenuCommand { get; }
        public ICommand SetDifficulteCommand { get; }
        public ICommand GenererCommand { get; }
        public ICommand AnnulerGenerationCommand { get; }
        public ICommand AnnulerCommand { get; }
        public ICommand RetourDashboardCommand { get; }
        public ICommand LogoutCommand { get; }

        // ── Événements ────────────────────────────────────────────────────────

        public event Action<List<QuestionQCM>, string, string, int, string?>? QuizGenereAvecSucces;
        public event Action? NavigationAnnulee;
        public event Action? NavigateToDashboard;
        public event Action? NavigateToLogin;

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

            RetourDashboardCommand = new RelayCommand(_ => NavigateToDashboard?.Invoke());
            LogoutCommand = new RelayCommand(_ => ExecuteLogout());
        }

        private void ExecuteLogout()
        {
            var result = MessageBox.Show(
                "Voulez-vous vraiment vous déconnecter ?",
                "Déconnexion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            new Services.SessionService(App.LocalDb).SupprimerSession();

            WpfApp.Current.Properties["Token"] = null;
            WpfApp.Current.Properties["Nom"] = null;
            WpfApp.Current.Properties["Email"] = null;

            NavigateToLogin?.Invoke();
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

        private static string ExtrairePdf(string chemin) =>
            NettoyerTexteSlides(PdfTextImport.ExtraireTexteBrut(chemin)).Trim();

        private static string ExtraireDocx(string chemin)
        {
            using var wordDoc = WordprocessingDocument.Open(chemin, isEditable: false);
            return wordDoc.MainDocumentPart?.Document.Body?.InnerText ?? string.Empty;
        }

        private static string NettoyerTexteSlides(string texte)
        {
            if (string.IsNullOrWhiteSpace(texte)) return string.Empty;
            var original = texte;

            var lignes = texte.Replace("\r\n", "\n").Split('\n');
            var sorties = new List<string>(lignes.Length);
            string previous = string.Empty;

            foreach (var brute in lignes)
            {
                var ligne = brute.Trim();
                if (ligne.Length == 0)
                {
                    if (sorties.Count > 0 && sorties[^1].Length > 0)
                        sorties.Add(string.Empty);
                    continue;
                }

                if (EstBruitDeSlide(ligne)) continue;
                if (string.Equals(previous, ligne, StringComparison.Ordinal)) continue;

                sorties.Add(ligne);
                previous = ligne;
            }

            var nettoye = string.Join("\n", sorties).Trim();
            return string.IsNullOrWhiteSpace(nettoye) ? original.Trim() : nettoye;
        }

        private static bool EstBruitDeSlide(string ligne)
        {
            if (ligne.StartsWith("©", StringComparison.Ordinal)) return true;
            if (ligne.Contains("Chapitre", StringComparison.OrdinalIgnoreCase)
                && ligne.Contains("AU:", StringComparison.OrdinalIgnoreCase)
                && ligne.Any(char.IsDigit)) return true;
            return false;
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
            StatusMessage = "🤖 Connexion au service IA...";

            try
            {
                await VerifierOllamaAsync(token);

                string modele = await DetecterModeleAsync(token);
                StatusMessage = $"✅ Modèle : {modele}";
                await Task.Delay(300, token);

                string contenu = ContenuCours;
                int limite = NombreQuestions <= 3  ? 1500 :
                             NombreQuestions <= 5  ? 2500 :
                             NombreQuestions <= 10 ? 4000 : 6000;
                if (contenu.Length > limite) contenu = contenu[..limite];

                StatusMessage = $"🧠 Génération ({NombreQuestions} questions, niveau {Difficulte})...";

                string reponse = await AppelOllamaStreamingAsync(modele, BuildPrompt(contenu), token);

                if (string.IsNullOrWhiteSpace(reponse))
                    throw new Exception("Aucune reponse n'a ete retournee.");

                System.Diagnostics.Debug.WriteLine("=== OLLAMA RAW ===");
                System.Diagnostics.Debug.WriteLine(reponse[..Math.Min(500, reponse.Length)]);

                StatusMessage = "📝 Extraction des questions...";
                var questions = ParseQCM(reponse);

                if (questions.Count == 0)
                {
                    string apercu = reponse.Length > 250
                        ? reponse[..250].Replace("\n", " ") + "…"
                        : reponse.Replace("\n", " ");
                    throw new Exception(
                        "Le modèle n'a pas produit de JSON valide.\n\n" +
                        $"Réponse reçue :\n{apercu}\n\n" +
                        "Conseil : réduisez le nombre de questions ou essayez un autre modèle (ex : mistral).");
                }

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
                ErrorMessage = $"❌ Service IA inaccessible.\n\nDetail : {ex.Message}";
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
                options = new { temperature = 0.2, num_predict = 8192, num_ctx = 8192, top_k = 40, top_p = 0.9 }
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
                throw new HttpRequestException("Le service IA local ne repond pas.");
            }
            catch (HttpRequestException)
            {
                throw new HttpRequestException("Le service IA local est indisponible.");
            }
        }

        // ── Prompt ────────────────────────────────────────────────────────────

        private string BuildPrompt(string contenu) =>
$@"You are a JSON quiz generator. Respond with ONLY a valid JSON array. No explanations. No markdown. No extra text.

Generate exactly {NombreQuestions} multiple-choice quiz questions in FRENCH. Difficulty: {Difficulte}.

Source text:
{contenu}

REQUIRED OUTPUT FORMAT (respond with ONLY this, starting with [ ):
[
{{""enonce"":""Question en français ?"",""optionA"":""Réponse A"",""optionB"":""Réponse B"",""optionC"":""Réponse C"",""optionD"":""Réponse D"",""reponseCorrecte"":""A"",""explication"":""Courte explication""}}
]

MANDATORY RULES:
- Your entire response must start with [ and end with ]
- Do NOT write anything before [ or after ]
- reponseCorrecte MUST be exactly one of: A, B, C or D
- Generate EXACTLY {NombreQuestions} question objects in the array";

        // ── Parser JSON robuste ───────────────────────────────────────────────

        private static List<QuestionQCM> ParseQCM(string texte)
        {
            var questions = new List<QuestionQCM>();
            try
            {
                // 1. Supprimer les balises markdown
                texte = Regex.Replace(texte, @"```json|```", "", RegexOptions.IgnoreCase).Trim();

                // 2. Extraire le tableau JSON (gérer {"questions":[...]} wrapper)
                string jsonPart = ExtraireTableau(texte);
                if (string.IsNullOrEmpty(jsonPart))
                {
                    System.Diagnostics.Debug.WriteLine("[ParseQCM] Aucun tableau JSON trouvé.");
                    return questions;
                }

                // 3. Nettoyer les virgules finales
                jsonPart = Regex.Replace(jsonPart, @",\s*([}\]])", "$1");

                // 4. Tentative parse direct
                var items = TryDeserialize(jsonPart);

                // 5. Tentative parse sur JSON tronqué
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
                        string enonce = StrAlt(item,
                            "enonce", "question", "text", "texte", "questionText", "enoncé");

                        string optA = StrAlt(item, "optionA", "option_a", "a", "choiceA");
                        string optB = StrAlt(item, "optionB", "option_b", "b", "choiceB");
                        string optC = StrAlt(item, "optionC", "option_c", "c", "choiceC");
                        string optD = StrAlt(item, "optionD", "option_d", "d", "choiceD");

                        // Certains modèles donnent "options": ["...", "...", "...", "..."]
                        if (string.IsNullOrWhiteSpace(optA))
                        {
                            var opts = ExtraireOptions(item);
                            if (opts.Count >= 2) { optA = opts[0]; optB = opts[1]; }
                            if (opts.Count >= 3) optC = opts[2];
                            if (opts.Count >= 4) optD = opts[3];
                        }

                        string rep = StrAlt(item,
                            "reponseCorrecte", "reponse_correcte", "answer", "correct_answer",
                            "correctAnswer", "correct", "bonne_reponse", "bonneReponse",
                            "reponse", "response");
                        rep = rep.ToUpper().Trim();

                        // Certains modèles retournent la lettre seule "a" ou "1"
                        if (rep == "1") rep = "A";
                        else if (rep == "2") rep = "B";
                        else if (rep == "3") rep = "C";
                        else if (rep == "4") rep = "D";
                        else if (rep.Length > 1 && !string.IsNullOrWhiteSpace(rep))
                        {
                            // ex: "A)" ou "A." ou "Option A"
                            rep = rep[0].ToString();
                        }

                        string explication = StrAlt(item,
                            "explication", "explanation", "justification",
                            "rationale", "reason", "expl");

                        var q = new QuestionQCM
                        {
                            Numero          = n++,
                            Enonce          = enonce,
                            OptionA         = optA,
                            OptionB         = optB,
                            OptionC         = optC,
                            OptionD         = optD,
                            ReponseCorrecte = rep,
                            Explication     = explication
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

        /// <summary>
        /// Extrait le tableau JSON depuis la réponse brute d'Ollama.
        /// Gère : tableau direct [...], objet wrapper {"questions":[...]},
        /// objet seul {...} (enveloppé automatiquement), et JSON tronqué.
        /// </summary>
        private static string ExtraireTableau(string texte)
        {
            int objDebut = texte.IndexOf('{');
            int arrDebut = texte.IndexOf('[');

            // Cas 1 : La réponse commence par un objet JSON (avant tout tableau)
            if (objDebut != -1 && (arrDebut == -1 || objDebut < arrDebut))
            {
                int objFin = texte.LastIndexOf('}');
                if (objFin > objDebut)
                {
                    try
                    {
                        string objText = texte[objDebut..(objFin + 1)];
                        using var doc = JsonDocument.Parse(objText);

                        // Chercher une propriété tableau à l'intérieur (ex. {"questions":[...]})
                        foreach (var prop in doc.RootElement.EnumerateObject())
                        {
                            if (prop.Value.ValueKind == JsonValueKind.Array)
                                return prop.Value.GetRawText();
                        }

                        // Aucune propriété tableau trouvée → l'objet EST une question → l'emballer
                        return "[" + objText + "]";
                    }
                    catch { }

                    // JSON invalide mais on a des accolades : tenter de récupérer tous les objets
                    var objets = ExtraireObjetsJSON(texte, objDebut);
                    if (objets.Count > 0)
                        return "[" + string.Join(",", objets) + "]";
                }
            }

            // Cas 2 : La réponse contient un tableau [...] (format attendu)
            if (arrDebut == -1) return string.Empty;
            int arrFin = texte.LastIndexOf(']');

            if (arrFin > arrDebut)
                return texte[arrDebut..(arrFin + 1)].Replace("\r", "").Replace("\t", " ");

            // Cas 3 : Tableau tronqué (pas de ']' final) → récupérer les objets complets
            var objetsArr = ExtraireObjetsJSON(texte, arrDebut);
            if (objetsArr.Count > 0)
                return "[" + string.Join(",", objetsArr) + "]";

            return string.Empty;
        }

        /// <summary>Extrait tous les objets JSON complets {...} depuis une position de départ.</summary>
        private static List<string> ExtraireObjetsJSON(string texte, int debut)
        {
            var objets = new List<string>();
            int i = debut;
            while (i < texte.Length)
            {
                int start = texte.IndexOf('{', i);
                if (start == -1) break;

                int depth = 0;
                int end = -1;
                bool inString = false;
                for (int j = start; j < texte.Length; j++)
                {
                    char c = texte[j];
                    if (c == '"' && (j == 0 || texte[j - 1] != '\\')) inString = !inString;
                    if (inString) continue;
                    if (c == '{') depth++;
                    else if (c == '}') { depth--; if (depth == 0) { end = j; break; } }
                }

                if (end == -1) break;

                string candidat = texte[start..(end + 1)];
                try
                {
                    JsonDocument.Parse(candidat);
                    objets.Add(candidat);
                }
                catch { }

                i = end + 1;
            }
            return objets;
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

        /// <summary>Tente plusieurs noms de champs alternatifs.</summary>
        private static string StrAlt(JsonElement el, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (el.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.String)
                    return p.GetString() ?? string.Empty;
                foreach (var prop in el.EnumerateObject())
                    if (string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase) &&
                        prop.Value.ValueKind == JsonValueKind.String)
                        return prop.Value.GetString() ?? string.Empty;
            }
            return string.Empty;
        }

        /// <summary>Extrait un tableau "options"/"choices"/"answers" si présent.</summary>
        private static List<string> ExtraireOptions(JsonElement el)
        {
            string[] arrKeys = { "options", "choices", "answers", "propositions" };
            foreach (var key in arrKeys)
            {
                JsonElement arr = default;
                bool found = false;
                if (el.TryGetProperty(key, out arr)) found = true;
                if (!found)
                    foreach (var prop in el.EnumerateObject())
                        if (string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase) &&
                            prop.Value.ValueKind == JsonValueKind.Array)
                        { arr = prop.Value; found = true; break; }

                if (found && arr.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<string>();
                    foreach (var item in arr.EnumerateArray())
                        list.Add(item.GetString() ?? item.GetRawText());
                    if (list.Count > 0) return list;
                }
            }
            return new List<string>();
        }
    }
}