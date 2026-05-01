using DocumentFormat.OpenXml.Packaging;
using Microsoft.Win32;
using smartest_desktop.Data;
using smartest_desktop.Helpers;
using smartest_desktop.Services;
using System.Diagnostics;
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

    public class QuizGenerationViewModel : BaseViewModel
    {
        private readonly LocalDbContext _db;
        private readonly GroqService _groq = new();
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

        // ── État UI ───────────────────────────────────────────────────────────

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
        // GÉNÉRATION GROQ
        // ══════════════════════════════════════════════════════════════════════

        private async Task GenererQuizAsync()
        {
            if (!HasCours) return;

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            IsGenerating = true;
            ErrorMessage = string.Empty;

            try
            {
                GroqService.VerifierConfiguration();

                StatusMessage = $"🚀 Connexion à Groq ({GroqService.NomModele})...";
                await Task.Delay(100, token);

                // ── Découpe en lots de MAX 5 questions ───────────────────────
                // Budget par appel : ~625 tokens contenu + ~200 instructions + ~800 réponse = ~1625 TPM ✅
                int maxParLot = GroqService.MAX_QUESTIONS_PAR_APPEL;
                int nbLots = (int)Math.Ceiling((double)NombreQuestions / maxParLot);
                var segmentsCours = DecoupeContenuEnSegments(ContenuCours, nbLots);

                List<QuestionQCM> toutesQuestions = new();
                TimeSpan dureeTotale = TimeSpan.Zero;

                for (int lotIdx = 0; lotIdx < nbLots && !token.IsCancellationRequested; lotIdx++)
                {
                    int dejaObtenu = toutesQuestions.Count;
                    int resteAObtenir = NombreQuestions - dejaObtenu;
                    if (resteAObtenir <= 0) break;

                    int nbCeLot = Math.Min(maxParLot, resteAObtenir);
                    string contenuLot = segmentsCours[lotIdx];

                    StatusMessage = nbLots > 1
                        ? $"🧠 Lot {lotIdx + 1}/{nbLots} — génération de {nbCeLot} questions ({Difficulte})..."
                        : $"🧠 Génération de {NombreQuestions} questions ({Difficulte})...";

                    // Délai anti-rate-limit entre lots
                    if (lotIdx > 0)
                        await Task.Delay(3000, token);

                    // Tentatives par lot (max 2)
                    int tentativesMax = 2;
                    List<QuestionQCM> questionsLot = new();

                    for (int tentative = 1; tentative <= tentativesMax && questionsLot.Count < nbCeLot; tentative++)
                    {
                        if (tentative > 1)
                        {
                            StatusMessage = $"🔄 Lot {lotIdx + 1} — tentative {tentative}/{tentativesMax}...";
                            await Task.Delay(2000, token);
                        }

                        string prompt = tentative == 1
                            ? BuildPrompt(contenuLot, nbCeLot, Difficulte)
                            : BuildPromptStrict(contenuLot, nbCeLot - questionsLot.Count, Difficulte);

                        var (reponse, duree) = await _groq.GenererAsync(prompt, token);
                        dureeTotale += duree;

                        var nouvelles = ParseQCM(reponse);

                        if (tentative == 1)
                        {
                            questionsLot = nouvelles;
                        }
                        else
                        {
                            var enonceExistants = new HashSet<string>(
                                questionsLot.Select(q => q.Enonce.ToLowerInvariant().Trim()));
                            foreach (var q in nouvelles)
                            {
                                if (!enonceExistants.Contains(q.Enonce.ToLowerInvariant().Trim()))
                                {
                                    questionsLot.Add(q);
                                    enonceExistants.Add(q.Enonce.ToLowerInvariant().Trim());
                                }
                            }
                        }
                    }

                    // Dédupliquer avec les questions déjà obtenues
                    var enonceGlobal = new HashSet<string>(
                        toutesQuestions.Select(q => q.Enonce.ToLowerInvariant().Trim()));
                    foreach (var q in questionsLot)
                    {
                        if (!enonceGlobal.Contains(q.Enonce.ToLowerInvariant().Trim()) &&
                            toutesQuestions.Count < NombreQuestions)
                        {
                            toutesQuestions.Add(q);
                            enonceGlobal.Add(q.Enonce.ToLowerInvariant().Trim());
                        }
                    }

                    if (nbLots > 1)
                        StatusMessage = $"✅ Lot {lotIdx + 1}/{nbLots} : {toutesQuestions.Count}/{NombreQuestions} questions...";
                }

                if (toutesQuestions.Count == 0)
                    throw new Exception(
                        "Groq n'a pas produit de JSON valide.\n\n" +
                        "Conseil : réduisez le nombre de questions ou réessayez.");

                toutesQuestions = CorrigerNombreQuestions(toutesQuestions, NombreQuestions);

                string avertissement = toutesQuestions.Count < NombreQuestions
                    ? $"\n⚠️ {toutesQuestions.Count}/{NombreQuestions} questions (max atteint)"
                    : string.Empty;

                string titre = string.IsNullOrWhiteSpace(TitreQuiz)
                    ? $"Quiz — {TitreCours}"
                    : TitreQuiz.Trim();

                StatusMessage = $"✅ {toutesQuestions.Count} questions en {dureeTotale.TotalSeconds:F1} s !{avertissement}";
                await Task.Delay(400, token);

                Debug.WriteLine($"[GenererQuiz] ✅ Terminé — {toutesQuestions.Count} questions en {dureeTotale.TotalSeconds:F1} s");

                QuizGenereAvecSucces?.Invoke(toutesQuestions, titre, Difficulte, NombreQuestions, TitreCours);
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "⛔ Génération annulée.";
                ErrorMessage = string.Empty;
            }
            catch (InvalidOperationException ex)
            {
                ErrorMessage = $"⚙️ Configuration requise.\n\n{ex.Message}";
                StatusMessage = string.Empty;
            }
            catch (TimeoutException ex)
            {
                ErrorMessage = $"⏱️ Timeout.\n\n{ex.Message}";
                StatusMessage = string.Empty;
            }
            catch (HttpRequestException ex)
            {
                ErrorMessage = $"❌ Erreur réseau.\n\n{ex.Message}";
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

        // ══════════════════════════════════════════════════════════════════════
        // DÉCOUPE DU CONTENU EN SEGMENTS (anti-TPM)
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Découpe le contenu du cours en N segments de taille ≤ LIMITE_CONTENU_CHARS (2500 chars).
        /// Chaque lot reçoit un segment différent pour couvrir tout le document
        /// tout en restant sous la limite TPM Groq gratuite (6000 tokens/min).
        /// </summary>
        private static List<string> DecoupeContenuEnSegments(string contenu, int nbSegments)
        {
            int limite = GroqService.LIMITE_CONTENU_CHARS;
            var segments = new List<string>();

            if (contenu.Length <= limite || nbSegments <= 1)
            {
                string seg = contenu.Length > limite ? contenu[..limite] : contenu;
                for (int i = 0; i < nbSegments; i++) segments.Add(seg);
                return segments;
            }

            int tailleTranche = Math.Max(limite, contenu.Length / nbSegments);

            for (int i = 0; i < nbSegments; i++)
            {
                int debut = i * tailleTranche;
                if (debut >= contenu.Length)
                {
                    segments.Add(segments[^1]);
                    continue;
                }

                int fin = Math.Min(debut + limite, contenu.Length);
                if (fin < contenu.Length)
                {
                    int espaceProche = contenu.LastIndexOf(' ', fin, Math.Min(100, fin - debut));
                    if (espaceProche > debut) fin = espaceProche;
                }

                segments.Add(contenu[debut..fin].Trim());
            }

            return segments;
        }

        // ══════════════════════════════════════════════════════════════════════
        // PROMPTS — DIFFÉRENCIÉS PAR NIVEAU (FIX PRINCIPAL)
        // ══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Retourne les instructions de difficulté détaillées selon le niveau choisi.
        /// C'est ici que réside la différenciation réelle entre Facile / Moyen / Difficile.
        /// </summary>
        private static string GetDifficultyInstructions(string difficulte) => difficulte switch
        {
            "Facile" =>
                "DIFFICULTY: EASY (Facile)\n" +
                "- Ask about basic definitions, simple facts directly stated in the text\n" +
                "- Questions should test recall and recognition only\n" +
                "- Wrong answers (distractors) should be clearly incorrect and easy to eliminate\n" +
                "- Use simple, short sentences\n" +
                "- Example question style: 'Qu'est-ce que X ?' / 'Quel est le rôle de Y ?'",

            "Difficile" =>
                "DIFFICULTY: HARD (Difficile)\n" +
                "- Ask about subtle distinctions, implicit relationships, and advanced reasoning\n" +
                "- Questions should require deep understanding and critical analysis\n" +
                "- Wrong answers (distractors) must be plausible and require careful thinking to eliminate\n" +
                "- Include questions about causes, consequences, comparisons, and exceptions\n" +
                "- Example question style: 'Pourquoi X entraîne-t-il Y dans le contexte de Z ?' / 'Quelle distinction fondamentale existe entre A et B ?'",

            _ => // Moyen (défaut)
                "DIFFICULTY: MEDIUM (Moyen)\n" +
                "- Ask about concepts that require understanding, not just memorization\n" +
                "- Questions should test comprehension and application of ideas\n" +
                "- Wrong answers (distractors) should be plausible but clearly wrong upon reflection\n" +
                "- Mix factual and conceptual questions\n" +
                "- Example question style: 'Comment fonctionne X ?' / 'Quel est l'effet de Y sur Z ?'"
        };

        private string BuildPrompt(string contenu, int nbQuestions, string difficulte)
        {
            string difficultyInstructions = GetDifficultyInstructions(difficulte);

            return
$@"Generate EXACTLY {nbQuestions} multiple-choice questions in FRENCH about the text below.

{difficultyInstructions}

TEXT:
{contenu}

OUTPUT FORMAT — respond with ONLY a JSON array of EXACTLY {nbQuestions} objects:
[
{{""enonce"":""Question en français ?"",""optionA"":""..."",""optionB"":""..."",""optionC"":""..."",""optionD"":""..."",""reponseCorrecte"":""A"",""explication"":""...""}}
]

RULES:
1. Start with [ — nothing before
2. End with ] — nothing after
3. reponseCorrecte = exactly one of: A, B, C or D
4. EXACTLY {nbQuestions} objects in the array
5. All text in French
6. Respect the difficulty level strictly — adapt complexity of questions AND distractors
7. No commentary outside the JSON array";
        }

        private string BuildPromptStrict(string contenu, int nbQuestions, string difficulte)
        {
            string difficultyInstructions = GetDifficultyInstructions(difficulte);
            int texteLength = Math.Min(contenu.Length, 4000);
            string texteTronque = contenu.Length > texteLength ? contenu[..texteLength] : contenu;

            return
$@"GÉNÉRATION STRICTE DE {nbQuestions} QUESTIONS QCM EN FRANÇAIS

{difficultyInstructions}

TEXTE SOURCE:
{texteTronque}

INSTRUCTIONS OBLIGATOIRES:
1. GÉNÉRER EXACTEMENT {nbQuestions} QUESTIONS - PAS PLUS, PAS MOINS
2. FORMAT: UNIQUEMENT JSON, AUCUN TEXTE AVANT OU APRÈS
3. COMMENCER PAR [ ET TERMINER PAR ]
4. Respecter strictement le niveau de difficulté indiqué ci-dessus

FORMAT:
[
{{""enonce"":""Question?"",""optionA"":""..."",""optionB"":""..."",""optionC"":""..."",""optionD"":""..."",""reponseCorrecte"":""A"",""explication"":""...""}}
]

RÉPONSE UNIQUEMENT LE JSON - RIEN D'AUTRE!";
        }

        // ══════════════════════════════════════════════════════════════════════
        // CORRECTION DU NOMBRE DE QUESTIONS
        // ══════════════════════════════════════════════════════════════════════

        private List<QuestionQCM> CorrigerNombreQuestions(List<QuestionQCM> questions, int nbSouhaite)
        {
            if (questions.Count == nbSouhaite)
                return questions;

            if (questions.Count > nbSouhaite)
            {
                Debug.WriteLine($"[Corriger] TROP de questions: {questions.Count}, garde {nbSouhaite}");
                return questions.Take(nbSouhaite).ToList();
            }
            else
            {
                Debug.WriteLine($"[Corriger] PAS ASSEZ: {questions.Count}/{nbSouhaite}, duplication");
                var result = new List<QuestionQCM>(questions);
                int index = 0;

                while (result.Count < nbSouhaite && result.Count > 0)
                {
                    var original = questions[index % questions.Count];
                    var duplique = new QuestionQCM
                    {
                        Enonce = $"{original.Enonce} (variante)",
                        OptionA = original.OptionA,
                        OptionB = original.OptionB,
                        OptionC = original.OptionC,
                        OptionD = original.OptionD,
                        ReponseCorrecte = original.ReponseCorrecte,
                        Explication = original.Explication,
                        Numero = result.Count + 1
                    };
                    result.Add(duplique);
                    index++;
                }

                return result;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // PARSER JSON ROBUSTE
        // ══════════════════════════════════════════════════════════════════════

        private static List<QuestionQCM> ParseQCM(string texte)
        {
            var questions = new List<QuestionQCM>();
            try
            {
                texte = Regex.Replace(texte, @"```json|```", "", RegexOptions.IgnoreCase).Trim();
                texte = texte.Replace('\u201C', '"').Replace('\u201D', '"')
                             .Replace('\u2018', '\'').Replace('\u2019', '\'');

                string jsonPart = ExtraireTableau(texte);
                if (string.IsNullOrEmpty(jsonPart))
                {
                    Debug.WriteLine("[ParseQCM] Aucun tableau JSON trouvé.");
                    return questions;
                }

                jsonPart = Regex.Replace(jsonPart, @",\s*([}\]])", "$1");

                var items = TryDeserialize(jsonPart);

                if (items == null)
                {
                    int dernierObjet = jsonPart.LastIndexOf('}');
                    if (dernierObjet > 0)
                    {
                        string repare = jsonPart[..(dernierObjet + 1)] + "]";
                        items = TryDeserialize(repare);
                        if (items != null)
                            Debug.WriteLine("[ParseQCM] JSON réparé (bracket final ajouté).");
                    }
                }

                if (items == null)
                {
                    Debug.WriteLine("[ParseQCM] Échec total du parsing.");
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

                        if (rep == "1") rep = "A";
                        else if (rep == "2") rep = "B";
                        else if (rep == "3") rep = "C";
                        else if (rep == "4") rep = "D";
                        else if (rep.Length > 1 && !string.IsNullOrWhiteSpace(rep))
                            rep = rep[0].ToString();

                        string explication = StrAlt(item,
                            "explication", "explanation", "justification",
                            "rationale", "reason", "expl");

                        var q = new QuestionQCM
                        {
                            Numero = n++,
                            Enonce = enonce,
                            OptionA = optA,
                            OptionB = optB,
                            OptionC = optC,
                            OptionD = optD,
                            ReponseCorrecte = rep,
                            Explication = explication
                        };

                        if (!string.IsNullOrWhiteSpace(q.Enonce) &&
                            !string.IsNullOrWhiteSpace(q.OptionA) &&
                            !string.IsNullOrWhiteSpace(q.OptionB) &&
                            !string.IsNullOrWhiteSpace(q.ReponseCorrecte) &&
                            "ABCD".Contains(q.ReponseCorrecte))
                        {
                            questions.Add(q);
                        }
                        else
                        {
                            Debug.WriteLine($"[ParseQCM] Question ignorée : enonce={q.Enonce[..Math.Min(q.Enonce.Length, 30)]}, rep={q.ReponseCorrecte}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[ParseQCM] Erreur sur un item : {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ParseQCM] Erreur globale : {ex.Message}");
            }

            Debug.WriteLine($"[ParseQCM] {questions.Count} questions extraites et validées.");
            return questions;
        }

        private static string ExtraireTableau(string texte)
        {
            int objDebut = texte.IndexOf('{');
            int arrDebut = texte.IndexOf('[');

            if (objDebut != -1 && (arrDebut == -1 || objDebut < arrDebut))
            {
                int objFin = texte.LastIndexOf('}');
                if (objFin > objDebut)
                {
                    try
                    {
                        string objText = texte[objDebut..(objFin + 1)];
                        using var doc = JsonDocument.Parse(objText);
                        foreach (var prop in doc.RootElement.EnumerateObject())
                            if (prop.Value.ValueKind == JsonValueKind.Array)
                                return prop.Value.GetRawText();
                        return "[" + objText + "]";
                    }
                    catch { }

                    var objets = ExtraireObjetsJSON(texte, objDebut);
                    if (objets.Count > 0)
                        return "[" + string.Join(",", objets) + "]";
                }
            }

            if (arrDebut == -1) return string.Empty;
            int arrFin = texte.LastIndexOf(']');

            if (arrFin > arrDebut)
                return texte[arrDebut..(arrFin + 1)].Replace("\r", "").Replace("\t", " ");

            var objetsArr = ExtraireObjetsJSON(texte, arrDebut);
            if (objetsArr.Count > 0)
                return "[" + string.Join(",", objetsArr) + "]";

            return string.Empty;
        }

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
                try { JsonDocument.Parse(candidat); objets.Add(candidat); }
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