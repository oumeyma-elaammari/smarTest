using DocumentFormat.OpenXml.Packaging;
using Microsoft.Win32;
using smartest_desktop.Data;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Helpers;
using smartest_desktop.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfApp = System.Windows.Application;

namespace smartest_desktop.ViewModels
{
    /// <summary>Élément affiché lorsque plusieurs cours sont importés pour un examen.</summary>
    public sealed class FichierCoursImporteItem : BaseViewModel
    {
        private string _titre = string.Empty;
        public string Titre
        {
            get => _titre;
            set => SetProperty(ref _titre, value);
        }

        private string _typeExtension = string.Empty;
        public string TypeExtension
        {
            get => _typeExtension;
            set => SetProperty(ref _typeExtension, value);
        }
    }

    public class ExamenGenerationViewModel : BaseViewModel
    {
        private readonly LocalDbContext _db;
        private readonly OllamaService  _ollama = new();
        private CancellationTokenSource? _cts;

        // ── Fichier importé ───────────────────────────────────────────────────

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
        public string ContenuCours
        {
            get => _contenuCours;
            set
            {
                SetProperty(ref _contenuCours, value);
                OnPropertyChanged(nameof(HasCours));
                OnPropertyChanged(nameof(HasNoCours));
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

        private bool _isImporting;
        public bool IsImporting
        {
            get => _isImporting;
            set { SetProperty(ref _isImporting, value); OnPropertyChanged(nameof(IsNotImporting)); }
        }
        public bool IsNotImporting => !_isImporting;

        public bool HasCours    => !string.IsNullOrWhiteSpace(ContenuCours);
        public bool HasNoCours  => !HasCours;
        public string NombreCaracteres =>
            HasCours ? $"{ContenuCours.Length:N0} caractères" : string.Empty;

        // ── Paramètres de l'examen ────────────────────────────────────────────

        private string _titreExamen = string.Empty;
        public string TitreExamen
        {
            get => _titreExamen;
            set => SetProperty(ref _titreExamen, value);
        }

        private int _duree = 90;
        public int Duree
        {
            get => _duree;
            set => SetProperty(ref _duree, value);
        }

        private string _difficulte = "Moyen";
        public string Difficulte
        {
            get => _difficulte;
            set => SetProperty(ref _difficulte, value);
        }

        public List<int> DureesDisponibles { get; } = new() { 30, 45, 60, 90, 120, 180 };
        public List<string> NiveauxDifficulte { get; } = new() { "Facile", "Moyen", "Difficile" };

        // ── Compteurs de questions par type ───────────────────────────────────

        private int _nbQCM = 3;
        public int NbQCM
        {
            get => _nbQCM;
            set
            {
                SetProperty(ref _nbQCM, Math.Max(0, value));
                OnPropertyChanged(nameof(TotalQuestions));
                ((RelayCommand)GenererCommand).RaiseCanExecuteChanged();
            }
        }

        private int _nbCheckbox = 0;
        public int NbCheckbox
        {
            get => _nbCheckbox;
            set
            {
                SetProperty(ref _nbCheckbox, Math.Max(0, value));
                OnPropertyChanged(nameof(TotalQuestions));
                ((RelayCommand)GenererCommand).RaiseCanExecuteChanged();
            }
        }

        private int _nbRedaction = 0;
        public int NbRedaction
        {
            get => _nbRedaction;
            set
            {
                SetProperty(ref _nbRedaction, Math.Max(0, value));
                OnPropertyChanged(nameof(TotalQuestions));
                ((RelayCommand)GenererCommand).RaiseCanExecuteChanged();
            }
        }

        public int TotalQuestions => NbQCM + NbCheckbox + NbRedaction;

        // ── État UI ───────────────────────────────────────────────────────────

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
        public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); }
        }
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        /// <summary>Fichiers sources (plusieurs imports possibles).</summary>
        public ObservableCollection<FichierCoursImporteItem> FichiersImportes { get; } = new();

        public string Nom => WpfApp.Current.Properties["Nom"]?.ToString() ?? "Professeur";
        public string Email => WpfApp.Current.Properties["Email"]?.ToString() ?? "";

        // ── Commandes ─────────────────────────────────────────────────────────

        public ICommand GenererCommand           { get; }
        public ICommand AnnulerGenerationCommand { get; }
        public ICommand AnnulerCommand           { get; }
        public ICommand SetDifficulteCommand     { get; }
        public ICommand ImporterFichierCommand   { get; }
        public ICommand EffacerContenuCommand    { get; }
        public ICommand IncrQCMCommand           { get; }
        public ICommand DecrQCMCommand           { get; }
        public ICommand IncrCheckboxCommand      { get; }
        public ICommand DecrCheckboxCommand      { get; }
        public ICommand IncrRedactionCommand     { get; }
        public ICommand DecrRedactionCommand     { get; }
        public ICommand RetourDashboardCommand   { get; }
        public ICommand LogoutCommand            { get; }

        // ── Événements de navigation ──────────────────────────────────────────

        public event Action<List<QuestionExamen>, string, int, string, string>? ExamenGenereAvecSucces;
        public event Action? NavigationAnnulee;
        public event Action? NavigateToDashboard;
        public event Action? NavigateToLogin;

        // ── Constructeur ──────────────────────────────────────────────────────

        public ExamenGenerationViewModel()
        {
            _db = App.LocalDb;

            SetDifficulteCommand = new RelayCommand(p =>
            {
                if (p is string d) Difficulte = d;
            });

            ImporterFichierCommand = new RelayCommand(
                async _ => await ImporterFichierAsync(),
                _ => !IsImporting && !IsGenerating);

            EffacerContenuCommand = new RelayCommand(
                _ =>
                {
                    var res = MessageBox.Show(
                        "Effacer tous les cours importés ?",
                        "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res != MessageBoxResult.Yes) return;
                    ContenuCours = string.Empty;
                    FichiersImportes.Clear();
                    TitreCours   = string.Empty;
                    NomFichier   = string.Empty;
                    TypeFichier  = string.Empty;
                    if (string.IsNullOrWhiteSpace(TitreExamen))
                        TitreExamen = string.Empty;
                },
                _ => HasCours && !IsGenerating);

            IncrQCMCommand       = new RelayCommand(_ => NbQCM++);
            DecrQCMCommand       = new RelayCommand(_ => NbQCM--);
            IncrCheckboxCommand  = new RelayCommand(_ => NbCheckbox++);
            DecrCheckboxCommand  = new RelayCommand(_ => NbCheckbox--);
            IncrRedactionCommand = new RelayCommand(_ => NbRedaction++);
            DecrRedactionCommand = new RelayCommand(_ => NbRedaction--);

            GenererCommand = new RelayCommand(
                async _ => await GenererExamen(),
                _ => HasCours && TotalQuestions > 0 && IsNotGenerating);

            AnnulerGenerationCommand = new RelayCommand(
                _ =>
                {
                    _cts?.Cancel();
                    StatusMessage = "⛔ Génération annulée.";
                    ErrorMessage  = string.Empty;
                    IsGenerating  = false;
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

            new SessionService(App.LocalDb).SupprimerSession();

            WpfApp.Current.Properties["Token"] = null;
            WpfApp.Current.Properties["Nom"] = null;
            WpfApp.Current.Properties["Email"] = null;

            NavigateToLogin?.Invoke();
        }

        // ── Import de fichier ─────────────────────────────────────────────────

        private async Task ImporterFichierAsync()
        {
            var dlg = new OpenFileDialog
            {
                Title  = "Importer un ou plusieurs cours",
                Filter = "Fichiers supportés (*.pdf;*.docx;*.txt)|*.pdf;*.docx;*.txt" +
                         "|PDF (*.pdf)|*.pdf|Word (*.docx)|*.docx|Texte (*.txt)|*.txt",
                Multiselect = true
            };

            if (dlg.ShowDialog() != true) return;

            var chemins = dlg.FileNames;
            if (chemins == null || chemins.Length == 0) return;

            IsImporting   = true;
            ErrorMessage  = string.Empty;
            StatusMessage = "📂 Extraction du contenu en cours...";

            try
            {
                int ajoutes = 0;

                foreach (string chemin in chemins)
                {
                    string extension = Path.GetExtension(chemin).ToLowerInvariant();
                    string contenu   = await Task.Run(() => ExtraireContenu(chemin, extension));

                    if (string.IsNullOrWhiteSpace(contenu))
                        continue;

                    AjouterBlocCours(chemin, extension, contenu.Trim());
                    ajoutes++;
                }

                if (ajoutes == 0)
                {
                    ErrorMessage  = "❌ Aucun contenu exploitable dans les fichiers sélectionnés.";
                    StatusMessage = string.Empty;
                    return;
                }

                NomFichier = Path.GetFileName(chemins[^1]);
                MettreAJourResumeFichiers();

                if (string.IsNullOrWhiteSpace(TitreExamen) && FichiersImportes.Count > 0)
                    TitreExamen = $"Examen — {FichiersImportes[0].Titre}";

                StatusMessage = ajoutes > 1
                    ? $"✅ {ajoutes} cours ajoutés ({NombreCaracteres})"
                    : $"✅ Cours importé ({NombreCaracteres})";

                _ = Task.Delay(2500).ContinueWith(_ =>
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (!IsGenerating) StatusMessage = string.Empty;
                    }));
            }
            catch (Exception ex)
            {
                ErrorMessage  = $"❌ Erreur lors de l'import : {ex.Message}";
                StatusMessage = string.Empty;
            }
            finally
            {
                IsImporting = false;
            }
        }

        private void AjouterBlocCours(string chemin, string extension, string contenuTrim)
        {
            string nomSansExt = Path.GetFileNameWithoutExtension(chemin);
            string label      = Path.GetFileName(chemin);
            string ext        = extension.TrimStart('.').ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(ContenuCours))
                ContenuCours = contenuTrim;
            else
            {
                ContenuCours +=
                    "\n\n────────────────────────────────────────\n" +
                    $"[{label}]\n" +
                    "────────────────────────────────────────\n\n" +
                    contenuTrim;
            }

            FichiersImportes.Add(new FichierCoursImporteItem
            {
                Titre          = nomSansExt,
                TypeExtension  = ext
            });
        }

        private void MettreAJourResumeFichiers()
        {
            if (FichiersImportes.Count == 0)
            {
                TitreCours  = string.Empty;
                TypeFichier = string.Empty;
                return;
            }

            if (FichiersImportes.Count == 1)
            {
                TitreCours  = FichiersImportes[0].Titre;
                TypeFichier = FichiersImportes[0].TypeExtension;
            }
            else
            {
                TitreCours  = $"{FichiersImportes.Count} cours importés";
                TypeFichier = "Plusieurs fichiers";
            }
        }

        private static string ExtraireContenu(string chemin, string extension) => extension switch
        {
            ".txt"  => File.ReadAllText(chemin, Encoding.UTF8),
            ".pdf"  => ExtrairePdf(chemin),
            ".docx" => ExtraireDocx(chemin),
            _       => throw new NotSupportedException($"Format non supporté : {extension}")
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

        // ── Génération ────────────────────────────────────────────────────────

        private async Task GenererExamen()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            IsGenerating  = true;
            ErrorMessage  = string.Empty;
            StatusMessage = "🤖 Connexion à Ollama...";

            try
            {
                await _ollama.VerifierAsync(ct);

                string modele = await _ollama.DetecterModeleAsync(ct);
                StatusMessage = $"✅ Modèle : {modele}";
                await Task.Delay(300, ct);

                string contenu = ContenuCours;
                int limiteContenu = TotalQuestions <= 5 ? 3000 : TotalQuestions <= 10 ? 5000 : 7000;
                if (contenu.Length > limiteContenu)
                    contenu = contenu[..limiteContenu];

                string prompt = _ollama.BuildPromptExamen(
                    contenu, NbQCM, NbCheckbox, NbRedaction, Difficulte);

                StatusMessage = $"🧠 Génération de {TotalQuestions} questions ({Difficulte})...";

                string texte = await _ollama.GenererStreamingAsync(
                    modele, prompt,
                    onProgress: _ => { },
                    ct: ct);

                if (string.IsNullOrWhiteSpace(texte))
                    throw new Exception("Ollama n'a retourné aucun texte.");

                StatusMessage = "📝 Extraction des questions...";
                var questions = ParseQuestions(texte);

                if (questions.Count == 0)
                    throw new Exception(
                        "Le modèle n'a pas produit de JSON valide.\n\n" +
                        "Essayez avec moins de questions ou changez de cours.");

                string titre = string.IsNullOrWhiteSpace(TitreExamen)
                    ? $"Examen — {TitreCours}"
                    : TitreExamen.Trim();

                StatusMessage = $"✅ {questions.Count} questions générées !";
                await Task.Delay(400, ct);

                ExamenGenereAvecSucces?.Invoke(
                    questions, titre, Duree, Difficulte, TitreCours);
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "⛔ Génération annulée.";
                ErrorMessage  = string.Empty;
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                ErrorMessage  = $"❌ Ollama inaccessible.\n\nLancez : ollama serve\n\nDétail : {ex.Message}";
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage  = $"❌ {ex.Message}";
                StatusMessage = string.Empty;
            }
            finally
            {
                IsGenerating = false;
            }
        }

        // ── Parser JSON ───────────────────────────────────────────────────────

        /// <summary>
        /// Nettoie la réponse brute d'Ollama avant de parser :
        /// supprime les blocs markdown, les virgules finales, les caractères parasites.
        /// </summary>
        private static string NettoyerJSON(string texte)
        {
            // Supprimer les blocs ```json ... ``` ou ``` ... ```
            texte = System.Text.RegularExpressions.Regex.Replace(
                texte, @"```[a-z]*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Supprimer les virgules finales avant ] ou }
            texte = System.Text.RegularExpressions.Regex.Replace(
                texte, @",\s*([}\]])", "$1");

            return texte.Trim();
        }

        private static List<QuestionExamen> ParseQuestions(string texte)
        {
            var result = new List<QuestionExamen>();
            try
            {
                texte = NettoyerJSON(texte);

                System.Diagnostics.Debug.WriteLine("=== OLLAMA EXAMEN RESPONSE ===");
                System.Diagnostics.Debug.WriteLine(texte[..Math.Min(800, texte.Length)]);

                // ── Extraire le tableau JSON ──────────────────────────────────────
                // Cas 1 : réponse directement un tableau [...]
                // Cas 2 : objet { "questions": [...] } ou { "exam": [...] }
                string jsonPart = ExtraireTableau(texte);
                if (string.IsNullOrEmpty(jsonPart)) return result;

                // Deuxième passe de nettoyage sur le segment extrait
                jsonPart = System.Text.RegularExpressions.Regex.Replace(
                    jsonPart, @",\s*([}\]])", "$1");

                var items = JsonSerializer.Deserialize<List<JsonElement>>(
                    jsonPart,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (items == null) return result;

                int n = 1;
                foreach (var item in items)
                {
                    try
                    {
                        // Type : accepte "QCM", "CHECKBOX", "REDACTION", "IMAGE"
                        // ou des variantes qu'Ollama peut produire
                        string type = StrAlt(item,
                            "type", "questionType", "question_type", "kind")
                            .ToUpperInvariant()
                            .Trim();

                        if (string.IsNullOrEmpty(type)) type = "QCM";
                        if (type.Contains("CHECK")) type = "CHECKBOX";
                        else if (type.Contains("REDAG") || type.Contains("OPEN") || type.Contains("LIBRE")) type = "REDACTION";
                        else if (type.Contains("IMAGE") || type.Contains("IMG")) type = "IMAGE";
                        else if (!type.StartsWith("QCM") && !type.StartsWith("CHECKBOX")
                                 && !type.StartsWith("REDACTION") && !type.StartsWith("IMAGE"))
                            type = "QCM"; // fallback

                        // Énoncé : plusieurs noms possibles
                        string enonce = StrAlt(item,
                            "enonce", "enoncé", "question", "texte", "text",
                            "questionText", "question_text", "libelle", "intitule");

                        if (string.IsNullOrWhiteSpace(enonce)) continue;

                        var q = new QuestionExamen
                        {
                            Numero      = n++,
                            Type        = type,
                            Enonce      = enonce,
                            Explication = StrAlt(item, "explication", "explanation",
                                                 "justification", "correction"),
                            Difficulte  = "Moyen",
                        };

                        if (type is "QCM" or "IMAGE")
                        {
                            // Options : soit optionA/B/C/D, soit tableau choix/options/choices
                            (q.OptionA, q.OptionB, q.OptionC, q.OptionD) = ExtraireOptions(item);
                            q.ReponseCorrecte = StrAlt(item,
                                "reponseCorrecte", "reponse_correcte", "reponse",
                                "correctAnswer", "correct", "answer", "bonne_reponse")
                                .ToUpper().Trim();

                            // Si la réponse est 1/2/3/4, convertir en A/B/C/D
                            q.ReponseCorrecte = q.ReponseCorrecte switch
                            {
                                "1" => "A", "2" => "B", "3" => "C", "4" => "D",
                                _   => q.ReponseCorrecte
                            };
                        }
                        else if (type == "CHECKBOX")
                        {
                            (q.OptionA, q.OptionB, q.OptionC, q.OptionD) = ExtraireOptions(item);
                            ExtraireReponsesCheckbox(item, q);
                        }
                        else if (type == "REDACTION")
                        {
                            q.ReponseModele = StrAlt(item,
                                "reponseModele", "reponse_modele", "modele",
                                "reponse", "answer", "expectedAnswer", "correction");
                        }

                        result.Add(q);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ParseQuestions] {ex.Message}");
            }
            return result;
        }

        // ── Helpers parseur ──────────────────────────────────────────────────────

        /// <summary>Extrait le premier tableau JSON valide du texte brut.</summary>
        private static string ExtraireTableau(string texte)
        {
            // Chercher le premier '[' et son ']' correspondant (balancé)
            int start = texte.IndexOf('[');
            if (start == -1)
            {
                // Peut-être un objet { "questions": [...] } sans tableau direct
                int ob = texte.IndexOf('{');
                if (ob == -1) return string.Empty;
                // Chercher un tableau à l'intérieur
                int inner = texte.IndexOf('[', ob);
                if (inner == -1) return string.Empty;
                start = inner;
            }

            // Trouver le crochet fermant équilibré
            int depth = 0;
            for (int i = start; i < texte.Length; i++)
            {
                if (texte[i] == '[') depth++;
                else if (texte[i] == ']') { depth--; if (depth == 0) return texte[start..(i + 1)]; }
            }
            return string.Empty;
        }

        /// <summary>
        /// Extrait les 4 options depuis optionA/B/C/D OU depuis un tableau
        /// (choix, options, choices, answers, propositions).
        /// </summary>
        private static (string A, string B, string C, string D) ExtraireOptions(JsonElement el)
        {
            string a = StrAlt(el, "optionA", "option_a", "choixA", "a");
            string b = StrAlt(el, "optionB", "option_b", "choixB", "b");
            string c = StrAlt(el, "optionC", "option_c", "choixC", "c");
            string d = StrAlt(el, "optionD", "option_d", "choixD", "d");

            // Si pas trouvé en individuel, chercher un tableau
            if (string.IsNullOrEmpty(a))
            {
                foreach (var key in new[] { "choix", "options", "choices", "answers",
                                             "propositions", "options_reponses" })
                {
                    JsonElement arr = default;
                    bool found = el.TryGetProperty(key, out arr);
                    if (!found)
                        foreach (var prop in el.EnumerateObject())
                            if (string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase))
                            { arr = prop.Value; found = true; break; }

                    if (found && arr.ValueKind == JsonValueKind.Array)
                    {
                        var list = arr.EnumerateArray().Select(x => x.GetString() ?? "").ToList();
                        return (
                            list.Count > 0 ? list[0] : "",
                            list.Count > 1 ? list[1] : "",
                            list.Count > 2 ? list[2] : "",
                            list.Count > 3 ? list[3] : ""
                        );
                    }
                }
            }

            return (a, b, c, d);
        }

        /// <summary>Extrait les cases cochées correctes pour une question CHECKBOX.</summary>
        private static void ExtraireReponsesCheckbox(JsonElement el, QuestionExamen q)
        {
            // Chercher reponsesCorrectes ou variantes
            foreach (var key in new[] { "reponsesCorrectes", "reponses_correctes",
                                         "correctAnswers", "correct_answers",
                                         "bonnesReponses", "bonnes_reponses" })
            {
                JsonElement arr = default;
                bool found = el.TryGetProperty(key, out arr);
                if (!found)
                    foreach (var prop in el.EnumerateObject())
                        if (string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase))
                        { arr = prop.Value; found = true; break; }

                if (found && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var rEl in arr.EnumerateArray())
                    {
                        var letter = (rEl.GetString() ?? "").ToUpper().Trim();
                        if (letter == "A" || letter == "1") q.OptionACorrecte = true;
                        else if (letter == "B" || letter == "2") q.OptionBCorrecte = true;
                        else if (letter == "C" || letter == "3") q.OptionCCorrecte = true;
                        else if (letter == "D" || letter == "4") q.OptionDCorrecte = true;
                    }
                    return;
                }
            }

            // Fallback : champs booléens individuels
            q.OptionACorrecte = StrAlt(el, "correcteA", "correctA", "aCorrect")
                                    .Equals("true", StringComparison.OrdinalIgnoreCase);
            q.OptionBCorrecte = StrAlt(el, "correcteB", "correctB", "bCorrect")
                                    .Equals("true", StringComparison.OrdinalIgnoreCase);
            q.OptionCCorrecte = StrAlt(el, "correcteC", "correctC", "cCorrect")
                                    .Equals("true", StringComparison.OrdinalIgnoreCase);
            q.OptionDCorrecte = StrAlt(el, "correcteD", "correctD", "dCorrect")
                                    .Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Lit une propriété en testant plusieurs noms alternatifs.</summary>
        private static string StrAlt(JsonElement el, params string[] keys)
        {
            foreach (var key in keys)
            {
                var v = Str(el, key);
                if (!string.IsNullOrEmpty(v)) return v;
            }
            return string.Empty;
        }

        private static string Str(JsonElement el, string key)
        {
            if (el.TryGetProperty(key, out var p) && p.ValueKind == JsonValueKind.String)
                return p.GetString() ?? string.Empty;
            foreach (var prop in el.EnumerateObject())
                if (string.Equals(prop.Name, key, StringComparison.OrdinalIgnoreCase)
                    && prop.Value.ValueKind == JsonValueKind.String)
                    return prop.Value.GetString() ?? string.Empty;
            return string.Empty;
        }
    }
}
