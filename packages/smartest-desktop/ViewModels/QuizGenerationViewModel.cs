using smartest_desktop.Data;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Helpers;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace smartest_desktop.ViewModels
{
    // ═══════════════════════════════════════════════════════════════════════════
    // QuestionQCM
    // ═══════════════════════════════════════════════════════════════════════════
    public class QuestionQCM : BaseViewModel
    {
        private string _enonce = string.Empty;
        public string Enonce
        {
            get => _enonce;
            set => SetProperty(ref _enonce, value);
        }

        private string _optionA = string.Empty;
        public string OptionA
        {
            get => _optionA;
            set => SetProperty(ref _optionA, value);
        }

        private string _optionB = string.Empty;
        public string OptionB
        {
            get => _optionB;
            set => SetProperty(ref _optionB, value);
        }

        private string _optionC = string.Empty;
        public string OptionC
        {
            get => _optionC;
            set => SetProperty(ref _optionC, value);
        }

        private string _optionD = string.Empty;
        public string OptionD
        {
            get => _optionD;
            set => SetProperty(ref _optionD, value);
        }

        private string _reponseCorrecte = string.Empty;
        public string ReponseCorrecte
        {
            get => _reponseCorrecte;
            set => SetProperty(ref _reponseCorrecte, value);
        }

        private string _explication = string.Empty;
        public string Explication
        {
            get => _explication;
            set => SetProperty(ref _explication, value);
        }

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                SetProperty(ref _isEditing, value);
                OnPropertyChanged(nameof(IsNotEditing));
            }
        }

        public bool IsNotEditing => !_isEditing;
        public int Numero { get; set; }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CoursItem — wrapper CoursLocal avec EstSelectionne pour le XAML
    // ═══════════════════════════════════════════════════════════════════════════
    public class CoursItem : BaseViewModel
    {
        public CoursLocal Cours { get; }

        public CoursItem(CoursLocal cours)
        {
            Cours = cours;
        }

        public int Id => Cours.Id;
        public string Titre => Cours.Titre;
        public string TypeFichier => Cours.TypeFichier ?? string.Empty;
        public DateTime DateImport => Cours.DateImport;
        public string Contenu => Cours.Contenu ?? string.Empty;

        private bool _estSelectionne;
        public bool EstSelectionne
        {
            get => _estSelectionne;
            set => SetProperty(ref _estSelectionne, value);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // QuizGenerationViewModel
    // ═══════════════════════════════════════════════════════════════════════════
    public class QuizGenerationViewModel : BaseViewModel
    {
        private readonly LocalDbContext _db;

        // HttpClient sans timeout global — on gère le timeout manuellement
        // par CancellationToken pour pouvoir annuler proprement
        private static readonly HttpClient _http = new()
        {
            Timeout = Timeout.InfiniteTimeSpan
        };

        private CancellationTokenSource? _cts;

        // ── Cours ─────────────────────────────────────────────────────────────
        public ObservableCollection<CoursItem> CoursDisponibles { get; } = new();

        private CoursItem? _coursSelectionne;
        public CoursItem? CoursSelectionne
        {
            get => _coursSelectionne;
            set
            {
                if (_coursSelectionne != null)
                    _coursSelectionne.EstSelectionne = false;

                SetProperty(ref _coursSelectionne, value);

                if (_coursSelectionne != null)
                    _coursSelectionne.EstSelectionne = true;

                OnPropertyChanged(nameof(HasCoursSelectionne));
                ((RelayCommand)GenererCommand).RaiseCanExecuteChanged();
            }
        }

        public bool HasCoursSelectionne => CoursSelectionne != null;

        // ── Paramètres ────────────────────────────────────────────────────────
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
        public List<string> NiveauxDifficulte { get; } = new() { "Facile", "Moyen", "Difficile" };

        // ── État ──────────────────────────────────────────────────────────────
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

        // ── Tokens générés (pour affichage progression) ───────────────────────
        private string _tokensGeneres = string.Empty;
        public string TokensGeneres
        {
            get => _tokensGeneres;
            set => SetProperty(ref _tokensGeneres, value);
        }

        // ── Commandes ─────────────────────────────────────────────────────────
        public ICommand GenererCommand { get; }
        public ICommand AnnulerCommand { get; }
        public ICommand AnnulerGenerationCommand { get; }
        public ICommand SelectionnerCoursCommand { get; }
        public ICommand SetDifficulteCommand { get; }

        // ── Événements ────────────────────────────────────────────────────────
        public event Action<List<QuestionQCM>, string, string, int, string?>? QuizGenereAvecSucces;
        public event Action? NavigationAnnulee;

        // ── Constructeur ──────────────────────────────────────────────────────
        public QuizGenerationViewModel()
        {
            _db = App.LocalDb;

            SelectionnerCoursCommand = new RelayCommand(param =>
            {
                if (param is CoursItem item)
                    CoursSelectionne = item;
            });

            SetDifficulteCommand = new RelayCommand(param =>
            {
                if (param is string niveau)
                    Difficulte = niveau;
            });

            GenererCommand = new RelayCommand(
                async _ => await GenererQuiz(),
                _ => CoursSelectionne != null && IsNotGenerating);

            AnnulerGenerationCommand = new RelayCommand(
                _ =>
                {
                    _cts?.Cancel();
                    StatusMessage = "⛔ Génération annulée.";
                    ErrorMessage = string.Empty;
                    TokensGeneres = string.Empty;
                    IsGenerating = false;
                },
                _ => IsGenerating);

            AnnulerCommand = new RelayCommand(_ =>
            {
                _cts?.Cancel();
                NavigationAnnulee?.Invoke();
            });

            _ = ChargerCours();
        }

        // ── Chargement cours ──────────────────────────────────────────────────
        private async Task ChargerCours()
        {
            try
            {
                var liste = await Task.Run(() => _db.Cours.ToList());
                CoursDisponibles.Clear();
                foreach (var c in liste)
                    CoursDisponibles.Add(new CoursItem(c));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Erreur chargement cours : {ex.Message}";
            }
        }

        // ── Génération Ollama (mode streaming) ────────────────────────────────
        //
        // On utilise stream:true pour lire les tokens au fur et à mesure.
        // Avantage : le timeout HTTP ne bloque plus car on reçoit des données
        // en continu. On n'attend plus la réponse complète d'un coup.
        //
        private async Task GenererQuiz()
        {
            if (CoursSelectionne == null) return;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            IsGenerating = true;
            ErrorMessage = string.Empty;
            StatusMessage = "🤖 Connexion à Ollama...";
            TokensGeneres = string.Empty;

            try
            {
                // ── 1. Ping rapide ────────────────────────────────────────────
                await VerifierOllama(token);

                // ── 2. Détecter le meilleur modèle disponible ─────────────────
                string modele = await DetecterModele(token);
                StatusMessage = $"✅ Modèle détecté : {modele}";
                await Task.Delay(300, token);

                // ── 3. Préparer le contenu (limité) ───────────────────────────
                string contenu = CoursSelectionne.Contenu;
                // Limite stricte selon le nombre de questions demandé
                int limiteContenu = NombreQuestions <= 3 ? 1500 :
                                    NombreQuestions <= 5 ? 2500 : 4000;
                if (contenu.Length > limiteContenu)
                    contenu = contenu[..limiteContenu];

                // ── 4. Prompt minimal ─────────────────────────────────────────
                string prompt = BuildPrompt(contenu);

                // ── 5. Appel streaming ────────────────────────────────────────
                StatusMessage = $"🧠 Génération ({NombreQuestions} questions, niveau {Difficulte})...";

                string texteComplet = await AppelOllamaStreaming(modele, prompt, token);

                if (string.IsNullOrWhiteSpace(texteComplet))
                    throw new Exception("Ollama n'a retourné aucun texte.");

                System.Diagnostics.Debug.WriteLine("=== OLLAMA FULL RESPONSE ===");
                System.Diagnostics.Debug.WriteLine(texteComplet[..Math.Min(800, texteComplet.Length)]);

                // ── 6. Parser ─────────────────────────────────────────────────
                StatusMessage = "📝 Extraction des questions...";
                var questions = ParseQCM(texteComplet);

                if (questions.Count == 0)
                {
                    // Afficher la réponse brute pour diagnostic
                    System.Diagnostics.Debug.WriteLine("=== PARSE FAILED — FULL TEXT ===");
                    System.Diagnostics.Debug.WriteLine(texteComplet);
                    throw new Exception(
                        "Le modèle n'a pas produit de JSON valide.\n\n" +
                        "Essayez avec 3 questions ou changez de cours.");
                }

                // ── 7. Succès ─────────────────────────────────────────────────
                string titre = string.IsNullOrWhiteSpace(TitreQuiz)
                    ? $"Quiz — {CoursSelectionne.Titre}"
                    : TitreQuiz.Trim();

                StatusMessage = $"✅ {questions.Count} questions générées !";
                TokensGeneres = string.Empty;
                await Task.Delay(400, token);

                QuizGenereAvecSucces?.Invoke(
                    questions, titre, Difficulte, NombreQuestions, CoursSelectionne.Titre);
            }
            catch (OperationCanceledException)
            {
                if (_cts?.IsCancellationRequested == true)
                {
                    StatusMessage = "⛔ Génération annulée.";
                    ErrorMessage = string.Empty;
                }
                else
                {
                    ErrorMessage = "⏱️ Timeout réseau.\nOllama est peut-être surchargé, réessayez.";
                    StatusMessage = string.Empty;
                }
                TokensGeneres = string.Empty;
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = $"❌ Ollama inaccessible.\n\nLancez : ollama serve\n\nDétail : {ex.Message}";
                StatusMessage = string.Empty;
                TokensGeneres = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"❌ {ex.Message}";
                StatusMessage = string.Empty;
                TokensGeneres = string.Empty;
            }
            finally
            {
                IsGenerating = false;
            }
        }

        // ── Streaming Ollama ──────────────────────────────────────────────────
        private async Task<string> AppelOllamaStreaming(
            string modele, string prompt, CancellationToken token)
        {
            var requestBody = new
            {
                model = modele,
                prompt = prompt,
                stream = true,          // ← streaming activé
                options = new
                {
                    temperature = 0.1,   // très déterministe
                    num_predict = 1024,  // max tokens de sortie
                    num_ctx = 2048,  // contexte d'entrée
                    top_k = 5,
                    top_p = 0.3
                }
            };

            var jsonBody = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            // Requête en streaming avec HttpCompletionOption.ResponseHeadersRead
            // → on lit le corps au fil de l'eau sans attendre la fin
            var request = new HttpRequestMessage(HttpMethod.Post,
                "http://localhost:11434/api/generate")
            {
                Content = httpContent
            };

            var response = await _http.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                token);

            response.EnsureSuccessStatusCode();

            var sb = new StringBuilder();
            int tokenCount = 0;

            await using var stream = await response.Content.ReadAsStreamAsync(token);
            using var reader = new StreamReader(stream, Encoding.UTF8);

            while (!reader.EndOfStream)
            {
                token.ThrowIfCancellationRequested();

                string? line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    using var lineDoc = JsonDocument.Parse(line);
                    var root = lineDoc.RootElement;

                    // Chaque ligne de streaming contient {"response":"token","done":false}
                    if (root.TryGetProperty("response", out var responseToken))
                    {
                        string fragment = responseToken.GetString() ?? "";
                        sb.Append(fragment);
                        tokenCount++;

                        // Mettre à jour le statut toutes les 20 tokens
                        if (tokenCount % 20 == 0)
                        {
                            int charsGeneres = sb.Length;
                            StatusMessage = $"🧠 Génération... ({charsGeneres} caractères)";
                        }
                    }

                    // Fin du streaming
                    if (root.TryGetProperty("done", out var doneEl) && doneEl.GetBoolean())
                        break;
                }
                catch (JsonException)
                {
                    // Ligne malformée, on continue
                }
            }

            return sb.ToString();
        }

        // ── Détecter le modèle disponible ────────────────────────────────────
        private async Task<string> DetecterModele(CancellationToken token)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, cts.Token);

                var res = await _http.GetAsync("http://localhost:11434/api/tags", linked.Token);
                var json = await res.Content.ReadAsStringAsync(linked.Token);

                using var doc = JsonDocument.Parse(json);

                // Ordre de préférence : modèles légers d'abord
                string[] preference = {
                    "phi3:mini", "phi3", "phi",
                    "llama3.2:1b", "llama3.2:3b", "llama3.2",
                    "mistral", "llama3", "llama2",
                    "gemma:2b", "gemma"
                };

                if (doc.RootElement.TryGetProperty("models", out var models))
                {
                    var modelNames = new List<string>();
                    foreach (var m in models.EnumerateArray())
                        if (m.TryGetProperty("name", out var n))
                            modelNames.Add(n.GetString() ?? "");

                    System.Diagnostics.Debug.WriteLine(
                        $"[Ollama] Modèles disponibles : {string.Join(", ", modelNames)}");

                    // Chercher par préférence
                    foreach (var pref in preference)
                        foreach (var name in modelNames)
                            if (name.StartsWith(pref, StringComparison.OrdinalIgnoreCase))
                                return name;

                    // Prendre le premier disponible
                    if (modelNames.Count > 0)
                        return modelNames[0];
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DetecterModele] {ex.Message}");
            }

            // Fallback
            return "mistral";
        }

        // ── Vérification Ollama ───────────────────────────────────────────────
        private async Task VerifierOllama(CancellationToken token)
        {
            try
            {
                using var pingCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(token, pingCts.Token);
                var res = await _http.GetAsync("http://localhost:11434/", linked.Token);
                // Ollama répond 200 sur /
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

        // ── Prompt minimal ────────────────────────────────────────────────────
        private string BuildPrompt(string contenu)
        {
            return
$@"Create {NombreQuestions} multiple choice questions in FRENCH based on this text. Difficulty: {Difficulte}.

Text:
{contenu}

Return ONLY a JSON array, no other text:
[{{""enonce"":""..."",""optionA"":""..."",""optionB"":""..."",""optionC"":""..."",""optionD"":""..."",""reponseCorrecte"":""A"",""explication"":""...""}}]";
        }

        // ── Parser JSON ───────────────────────────────────────────────────────
        private static List<QuestionQCM> ParseQCM(string texte)
        {
            var questions = new List<QuestionQCM>();
            try
            {
                int debut = texte.IndexOf('[');
                int fin = texte.LastIndexOf(']');
                if (debut == -1 || fin == -1 || fin <= debut) return questions;

                string jsonPart = texte[debut..(fin + 1)];
                jsonPart = jsonPart.Replace("\r", "").Replace("\t", " ");

                var items = JsonSerializer.Deserialize<List<JsonElement>>(
                    jsonPart,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (items == null) return questions;

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
                    catch { /* question mal formée */ }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ParseQCM] {ex.Message}");
            }
            return questions;
        }

        private static string Str(JsonElement el, string key)
        {
            if (el.TryGetProperty(key, out var p))
                return p.GetString() ?? string.Empty;
            foreach (var prop in el.EnumerateObject())
                if (string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase))
                    return prop.Value.GetString() ?? string.Empty;
            return string.Empty;
        }
    }
}