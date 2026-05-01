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

        private string _blocContenu = string.Empty;
        public string BlocContenu
        {
            get => _blocContenu;
            set => SetProperty(ref _blocContenu, value);
        }
    }

    public class ExamenGenerationViewModel : BaseViewModel
    {
        private readonly LocalDbContext _db;
        private readonly GroqService _groq = new();
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

        public bool HasCours => !string.IsNullOrWhiteSpace(ContenuCours);
        public bool HasNoCours => !HasCours;
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
                ((RelayCommand)SupprimerFichierImporteCommand).RaiseCanExecuteChanged();
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

        public ICommand GenererCommand { get; }
        public ICommand AnnulerGenerationCommand { get; }
        public ICommand AnnulerCommand { get; }
        public ICommand SetDifficulteCommand { get; }
        public ICommand ImporterFichierCommand { get; }
        public ICommand EffacerContenuCommand { get; }
        public ICommand IncrQCMCommand { get; }
        public ICommand DecrQCMCommand { get; }
        public ICommand IncrCheckboxCommand { get; }
        public ICommand DecrCheckboxCommand { get; }
        public ICommand IncrRedactionCommand { get; }
        public ICommand DecrRedactionCommand { get; }
        public ICommand RetourDashboardCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand SupprimerFichierImporteCommand { get; }

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
                    TitreCours = string.Empty;
                    NomFichier = string.Empty;
                    TypeFichier = string.Empty;
                    if (string.IsNullOrWhiteSpace(TitreExamen))
                        TitreExamen = string.Empty;
                },
                _ => HasCours && !IsGenerating);

            SupprimerFichierImporteCommand = new RelayCommand(
                p =>
                {
                    if (p is not FichierCoursImporteItem fichier) return;
                    if (!FichiersImportes.Contains(fichier)) return;

                    FichiersImportes.Remove(fichier);
                    RebuildContenuDepuisFichiers();
                    MettreAJourResumeFichiers();

                    if (FichiersImportes.Count == 0)
                    {
                        TitreCours = string.Empty;
                        NomFichier = string.Empty;
                        TypeFichier = string.Empty;
                    }

                    StatusMessage = "🗑 Cours supprimé.";
                    _ = Task.Delay(1800).ContinueWith(_ =>
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (!IsGenerating) StatusMessage = string.Empty;
                        }));
                },
                _ => !IsGenerating && FichiersImportes.Count > 0);

            IncrQCMCommand = new RelayCommand(_ => NbQCM++);
            DecrQCMCommand = new RelayCommand(_ => NbQCM--);
            IncrCheckboxCommand = new RelayCommand(_ => NbCheckbox++);
            DecrCheckboxCommand = new RelayCommand(_ => NbCheckbox--);
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
                Title = "Importer un ou plusieurs cours",
                Filter = "Fichiers supportés (*.pdf;*.docx;*.txt)|*.pdf;*.docx;*.txt" +
                         "|PDF (*.pdf)|*.pdf|Word (*.docx)|*.docx|Texte (*.txt)|*.txt",
                Multiselect = true
            };

            if (dlg.ShowDialog() != true) return;

            var chemins = dlg.FileNames;
            if (chemins == null || chemins.Length == 0) return;

            IsImporting = true;
            ErrorMessage = string.Empty;
            StatusMessage = "📂 Extraction du contenu en cours...";

            try
            {
                int ajoutes = 0;

                foreach (string chemin in chemins)
                {
                    string extension = Path.GetExtension(chemin).ToLowerInvariant();
                    string contenu = await Task.Run(() => ExtraireContenu(chemin, extension));

                    if (string.IsNullOrWhiteSpace(contenu))
                        continue;

                    AjouterBlocCours(chemin, extension, contenu.Trim());
                    ajoutes++;
                }

                if (ajoutes == 0)
                {
                    ErrorMessage = "❌ Aucun contenu exploitable dans les fichiers sélectionnés.";
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
                ErrorMessage = $"❌ Erreur lors de l'import : {ex.Message}";
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
            string label = Path.GetFileName(chemin);
            string ext = extension.TrimStart('.').ToUpperInvariant();

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
                Titre = nomSansExt,
                TypeExtension = ext,
                BlocContenu = contenuTrim
            });
        }

        private void RebuildContenuDepuisFichiers()
        {
            if (FichiersImportes.Count == 0)
            {
                ContenuCours = string.Empty;
                return;
            }

            var sb = new StringBuilder();
            for (int i = 0; i < FichiersImportes.Count; i++)
            {
                var item = FichiersImportes[i];
                if (i > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("────────────────────────────────────────");
                    sb.AppendLine($"[{item.Titre}.{item.TypeExtension.ToLowerInvariant()}]");
                    sb.AppendLine("────────────────────────────────────────");
                    sb.AppendLine();
                }
                sb.Append(item.BlocContenu);
            }
            ContenuCours = sb.ToString();
        }

        private void MettreAJourResumeFichiers()
        {
            if (FichiersImportes.Count == 0)
            {
                TitreCours = string.Empty;
                TypeFichier = string.Empty;
                return;
            }

            if (FichiersImportes.Count == 1)
            {
                TitreCours = FichiersImportes[0].Titre;
                TypeFichier = FichiersImportes[0].TypeExtension;
            }
            else
            {
                TitreCours = $"{FichiersImportes.Count} cours importés";
                TypeFichier = "Plusieurs fichiers";
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

        // ── Génération via Groq (avec génération par lots) ────────────────────

        private async Task GenererExamen()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            IsGenerating = true;
            ErrorMessage = string.Empty;
            StatusMessage = "🤖 Connexion à Groq...";

            try
            {
                // Vérification clé API
                GroqService.VerifierConfiguration();
                StatusMessage = $"✅ Modèle : {GroqService.NomModele}";
                await Task.Delay(300, ct);

                // ── Préparation du contenu du cours ──────────────────────────────────
                //
                // STRATÉGIE BATCHING :
                //   Au lieu d'envoyer tout le cours en une seule requête (→ rate limit),
                //   on génère les questions par petits lots de 4.
                //   Chaque lot reçoit un EXTRAIT du cours (~2000 chars) pour rester
                //   dans la limite de tokens par minute de Groq.
                //
                //   Pour varier les questions entre lots, on fait tourner une "fenêtre"
                //   dans le contenu du cours (lot 1 → début, lot 2 → milieu, lot 3 → fin, etc.)

                string contenuComplet = ContenuCours;

                // Limite raisonnable pour éviter les cours de 100 pages
                // (inutile d'envoyer 50 000 chars pour générer 20 questions)
                const int LIMITE_CONTENU_TOTAL = 20_000;
                if (contenuComplet.Length > LIMITE_CONTENU_TOTAL)
                    contenuComplet = contenuComplet[..LIMITE_CONTENU_TOTAL];

                int tailleContexte = GroqService.TailleContexteParLot;
                int totalQuestions = TotalQuestions;

                // ── Décision : batching ou requête unique ? ───────────────────────────
                //
                // Si ≤ 4 questions ET cours court → requête unique (plus rapide)
                // Sinon → batching automatique
                bool useBatching = totalQuestions > 4 || contenuComplet.Length > tailleContexte;

                if (!useBatching)
                {
                    // ── CAS SIMPLE : une seule requête (≤ 4 questions, cours court) ──
                    string contenu = contenuComplet.Length > tailleContexte
                        ? contenuComplet[..tailleContexte]
                        : contenuComplet;

                    string prompt = GroqService.BuildPromptExamen(
                        contenu, NbQCM, NbCheckbox, NbRedaction, Difficulte);

                    StatusMessage = $"🧠 Génération de {totalQuestions} questions ({Difficulte})...";

                    var (texte, duree) = await _groq.GenererAsync(prompt, ct);

                    if (string.IsNullOrWhiteSpace(texte))
                        throw new Exception("Groq n'a retourné aucun texte.");

                    StatusMessage = "📝 Extraction des questions...";
                    var questions = ParseQuestions(texte);
                    questions = AjusterTypesQuestions(questions, NbQCM, NbCheckbox, NbRedaction);

                    if (questions.Count == 0)
                        throw new Exception(
                            "Le modèle n'a pas produit de JSON valide.\n\n" +
                            "Essayez avec moins de questions ou changez de cours.");

                    string titreQ = string.IsNullOrWhiteSpace(TitreExamen)
                        ? $"Examen — {TitreCours}"
                        : TitreExamen.Trim();

                    StatusMessage = $"✅ {questions.Count} questions générées en {duree.TotalSeconds:F1} s !";
                    await Task.Delay(400, ct);

                    ExamenGenereAvecSucces?.Invoke(questions, titreQ, Duree, Difficulte, TitreCours);
                }
                else
                {
                    // ── CAS BATCHING : plusieurs lots de questions ────────────────────
                    //
                    // On répartit les types de questions proportionnellement sur les lots.
                    // Exemple : 6 QCM + 4 CHECKBOX + 2 REDACTION = 12 questions
                    //   Lot 1 : 2 QCM + 1 CHECKBOX + 1 REDACTION = 4 questions
                    //   Lot 2 : 2 QCM + 2 CHECKBOX + 0 REDACTION = 4 questions
                    //   Lot 3 : 2 QCM + 1 CHECKBOX + 1 REDACTION = 4 questions

                    int nbLots = (int)Math.Ceiling((double)totalQuestions / 4.0);

                    // Distribuer les types sur les lots
                    var lotsQCM = DistribuerSurLots(NbQCM, nbLots);
                    var lotsCheckbox = DistribuerSurLots(NbCheckbox, nbLots);
                    var lotsRedaction = DistribuerSurLots(NbRedaction, nbLots);

                    StatusMessage = $"🧠 Génération par lots : {totalQuestions} questions en {nbLots} lots...";

                    var toutesQuestions = new List<QuestionExamen>();
                    int questionDepart = 1;
                    var swTotal = System.Diagnostics.Stopwatch.StartNew();

                    // Fonction qui construit le prompt pour un lot donné
                    // Elle fait "tourner" la fenêtre de contexte dans le cours
                    string BuildPromptPourLot(int nbQInLot, int numeroPremiereLigne)
                    {
                        // Indice du lot courant (0-based)
                        int idxLot = (numeroPremiereLigne - 1) / 4;

                        // Faire tourner la fenêtre de contexte pour varier les questions
                        int debut = (idxLot * tailleContexte / 2) % Math.Max(1, contenuComplet.Length - tailleContexte);
                        string extrait = contenuComplet.Length <= tailleContexte
                            ? contenuComplet
                            : contenuComplet[debut..Math.Min(debut + tailleContexte, contenuComplet.Length)];

                        // Calculer la répartition pour CE lot spécifique
                        int idxLotSafe = Math.Min(idxLot, nbLots - 1);
                        int qcmLot = idxLotSafe < lotsQCM.Length ? lotsQCM[idxLotSafe] : 0;
                        int cbkLot = idxLotSafe < lotsCheckbox.Length ? lotsCheckbox[idxLotSafe] : 0;
                        int redLot = idxLotSafe < lotsRedaction.Length ? lotsRedaction[idxLotSafe] : 0;

                        return GroqService.BuildPromptExamenLot(
                            extrait, qcmLot, cbkLot, redLot, Difficulte, numeroPremiereLigne);
                    }

                    // Appel batching avec callback de progression
                    var (jsonFusionne, dureeTotal) = await _groq.GenererParLotsAsync(
                        buildPromptPourLot: (nbQInLot, numDepart) => BuildPromptPourLot(nbQInLot, numDepart),
                        totalQuestions: totalQuestions,
                        onProgres: (lotActuel, nbTotalLots) =>
                        {
                            // Mise à jour UI depuis le thread de travail → Dispatcher requis
                            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                            {
                                StatusMessage = $"🧠 Lot {lotActuel}/{nbTotalLots} généré...";
                            });
                        },
                        ct: ct);

                    StatusMessage = "📝 Extraction et fusion des questions...";

                    var questions = ParseQuestions(jsonFusionne);
                    questions = AjusterTypesQuestions(questions, NbQCM, NbCheckbox, NbRedaction);

                    if (questions.Count == 0)
                        throw new Exception(
                            "Aucune question n'a pu être extraite des lots générés.\n\n" +
                            "Essayez avec moins de questions ou un cours plus structuré.");

                    string titre = string.IsNullOrWhiteSpace(TitreExamen)
                        ? $"Examen — {TitreCours}"
                        : TitreExamen.Trim();

                    string avertissement = questions.Count < totalQuestions
                        ? $"\n⚠️ {questions.Count}/{totalQuestions} obtenues (certains lots incomplets)"
                        : string.Empty;

                    StatusMessage = $"✅ {questions.Count} questions en {dureeTotal.TotalSeconds:F1} s !{avertissement}";
                    await Task.Delay(400, ct);

                    ExamenGenereAvecSucces?.Invoke(questions, titre, Duree, Difficulte, TitreCours);
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "⛔ Génération annulée.";
                ErrorMessage = string.Empty;
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                ErrorMessage = $"❌ Groq inaccessible.\n\nVérifiez votre connexion internet.\n\nDétail : {ex.Message}";
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

        // ── Utilitaire : distribuer N éléments sur K lots ─────────────────────────────

        /// <summary>
        /// Distribue N questions aussi uniformément que possible sur K lots.
        ///
        /// Exemple : 7 questions sur 3 lots → [3, 2, 2]
        ///           5 questions sur 4 lots → [2, 1, 1, 1]
        ///           0 questions sur 3 lots → [0, 0, 0]
        /// </summary>
        private static int[] DistribuerSurLots(int total, int nbLots)
        {
            if (nbLots <= 0) return Array.Empty<int>();
            if (total <= 0) return new int[nbLots]; // tous à zéro

            var lots = new int[nbLots];
            int base_ = total / nbLots;
            int reste = total % nbLots;

            for (int i = 0; i < nbLots; i++)
                lots[i] = base_ + (i < reste ? 1 : 0);

            return lots;
        }


        /// <summary>
        /// Découpe (nbQCM, nbCheckbox, nbRedaction) en lots de taille ≤ maxParLot.
        /// Chaque lot respecte les proportions autant que possible.
        /// </summary>
        private static List<(int QCM, int Checkbox, int Redaction)> DecoupeEnLots(
            int nbQCM, int nbCheckbox, int nbRedaction, int maxParLot)
        {
            int total = nbQCM + nbCheckbox + nbRedaction;

            // Cas simple : tout tient dans un seul lot
            if (total <= maxParLot)
                return new List<(int QCM, int Checkbox, int Redaction)> { (nbQCM, nbCheckbox, nbRedaction) };

            // Calculer le nombre de lots nécessaires
            int nbLots = (int)Math.Ceiling((double)total / maxParLot);
            var lots = new List<(int QCM, int Checkbox, int Redaction)>();

            int restQCM = nbQCM;
            int restCheckbox = nbCheckbox;
            int restRedaction = nbRedaction;

            for (int i = 0; i < nbLots; i++)
            {
                int restTotal = restQCM + restCheckbox + restRedaction;
                int lotsRestants = nbLots - i;

                // Taille de ce lot (répartition équitable)
                int tailleLot = (int)Math.Ceiling((double)restTotal / lotsRestants);
                tailleLot = Math.Min(tailleLot, maxParLot);

                // Distribuer proportionnellement dans ce lot
                int lotQCM = Math.Min(restQCM,
                    (int)Math.Round((double)nbQCM / (nbQCM + nbCheckbox + nbRedaction) * tailleLot));
                int lotCheckbox = Math.Min(restCheckbox,
                    (int)Math.Round((double)nbCheckbox / (nbQCM + nbCheckbox + nbRedaction) * tailleLot));
                int lotRedaction = Math.Min(restRedaction, tailleLot - lotQCM - lotCheckbox);

                // Ajuster pour ne pas dépasser maxParLot
                int lotTotal = lotQCM + lotCheckbox + lotRedaction;
                if (lotTotal > maxParLot)
                {
                    // Réduire proportionnellement
                    lotQCM = (int)Math.Floor((double)lotQCM * maxParLot / lotTotal);
                    lotCheckbox = (int)Math.Floor((double)lotCheckbox * maxParLot / lotTotal);
                    lotRedaction = maxParLot - lotQCM - lotCheckbox;
                }

                // S'assurer qu'on génère au moins 1 question par lot
                if (lotQCM + lotCheckbox + lotRedaction == 0)
                {
                    if (restQCM > 0) lotQCM = 1;
                    else if (restCheckbox > 0) lotCheckbox = 1;
                    else if (restRedaction > 0) lotRedaction = 1;
                }

                lots.Add((lotQCM, lotCheckbox, lotRedaction));

                restQCM -= lotQCM;
                restCheckbox -= lotCheckbox;
                restRedaction -= lotRedaction;

                if (restQCM + restCheckbox + restRedaction == 0) break;
            }

            // Si des questions restent (à cause des arrondis), les ajouter au dernier lot
            if (restQCM + restCheckbox + restRedaction > 0 && lots.Count > 0)
            {
                var last = lots[^1];
                lots[^1] = (last.QCM + restQCM, last.Checkbox + restCheckbox, last.Redaction + restRedaction);
            }

            // Supprimer les lots vides
            lots.RemoveAll(l => l.QCM + l.Checkbox + l.Redaction == 0);

            return lots;
        }

        /// <summary>
        /// Découpe le contenu du cours en N segments de taille ≤ LIMITE_CONTENU_CHARS.
        /// Chaque lot reçoit un segment différent pour couvrir l'ensemble du document.
        /// Si le cours est court, tous les segments sont identiques (le cours entier tronqué).
        /// </summary>
        private static List<string> DecoupeContenuEnSegments(string contenu, int nbSegments)
        {
            int limite = GroqService.LIMITE_CONTENU_CHARS;
            var segments = new List<string>();

            if (contenu.Length <= limite || nbSegments <= 1)
            {
                // Cours court : même segment pour tout le monde
                string seg = contenu.Length > limite ? contenu[..limite] : contenu;
                for (int i = 0; i < nbSegments; i++) segments.Add(seg);
                return segments;
            }

            // Cours long : découper en tranches qui se chevauchent légèrement
            // pour ne pas couper au milieu d'une phrase importante
            int tailleTranche = Math.Max(limite, contenu.Length / nbSegments);

            for (int i = 0; i < nbSegments; i++)
            {
                int debut = i * tailleTranche;
                if (debut >= contenu.Length)
                {
                    // Plus de contenu : répéter le dernier segment
                    segments.Add(segments[^1]);
                    continue;
                }

                int fin = Math.Min(debut + limite, contenu.Length);

                // Essayer de couper sur un espace pour ne pas tronquer un mot
                if (fin < contenu.Length)
                {
                    int espaceProche = contenu.LastIndexOf(' ', fin, Math.Min(100, fin - debut));
                    if (espaceProche > debut) fin = espaceProche;
                }

                segments.Add(contenu[debut..fin].Trim());
            }

            return segments;
        }

        /// <summary>
        /// Si Groq n'a pas produit exactement le bon nombre de questions par type,
        /// on coupe les excès et on ré-étiquette les manquants pour respecter les compteurs.
        /// </summary>
        private static List<QuestionExamen> AjusterTypesQuestions(
            List<QuestionExamen> src, int wantQCM, int wantCheckbox, int wantRedaction)
        {
            var qcms = src.Where(q => q.Type == "QCM").Take(wantQCM).ToList();
            var checks = src.Where(q => q.Type == "CHECKBOX").Take(wantCheckbox).ToList();
            var reds = src.Where(q => q.Type == "REDACTION").Take(wantRedaction).ToList();

            var restants = src.Where(q => q.Type == "QCM" && !qcms.Contains(q)).ToList();

            while (checks.Count < wantCheckbox && restants.Count > 0)
            {
                var q = restants[0]; restants.RemoveAt(0);
                q.Type = "CHECKBOX";
                checks.Add(q);
            }

            while (reds.Count < wantRedaction && restants.Count > 0)
            {
                var q = restants[0]; restants.RemoveAt(0);
                q.Type = "REDACTION";
                q.ReponseModele = q.Explication;
                reds.Add(q);
            }

            while (qcms.Count < wantQCM && restants.Count > 0)
            {
                qcms.Add(restants[0]);
                restants.RemoveAt(0);
            }

            var result = qcms.Concat(checks).Concat(reds).ToList();
            int n = 1;
            foreach (var q in result) q.Numero = n++;
            return result;
        }

        // ── Parser JSON ───────────────────────────────────────────────────────

        private static string NettoyerJSON(string texte)
        {
            // Supprimer les blocs markdown
            texte = System.Text.RegularExpressions.Regex.Replace(
                texte, @"```[a-z]*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            texte = texte.Replace("```", "");

            // Supprimer les virgules traînantes avant } ou ]
            texte = System.Text.RegularExpressions.Regex.Replace(
                texte, @",\s*([}\]])", "$1");

            // Supprimer les caractères de contrôle invisibles sauf \n \r \t
            texte = System.Text.RegularExpressions.Regex.Replace(
                texte, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", "");

            return texte.Trim();
        }

        private static List<QuestionExamen> ParseQuestions(string texte)
        {
            var result = new List<QuestionExamen>();
            try
            {
                texte = NettoyerJSON(texte);

                System.Diagnostics.Debug.WriteLine("=== GROQ EXAMEN RESPONSE ===");
                System.Diagnostics.Debug.WriteLine(texte[..Math.Min(800, texte.Length)]);

                string jsonPart = ExtraireTableau(texte);

                // ✅ Si le tableau est vide ou incomplet, tenter de le réparer
                if (string.IsNullOrEmpty(jsonPart))
                {
                    System.Diagnostics.Debug.WriteLine("[ParseQuestions] Tableau non trouvé, tentative de réparation...");
                    jsonPart = TenterReparerJSON(texte);
                }

                if (string.IsNullOrEmpty(jsonPart)) return result;

                // Supprimer virgules traînantes (seconde passe après extraction)
                jsonPart = System.Text.RegularExpressions.Regex.Replace(
                    jsonPart, @",\s*([}\]])", "$1");

                List<JsonElement>? items = null;
                try
                {
                    items = JsonSerializer.Deserialize<List<JsonElement>>(
                        jsonPart,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException)
                {
                    // ✅ Dernière tentative : extraire les objets JSON individuels
                    System.Diagnostics.Debug.WriteLine("[ParseQuestions] Échec désérialisation, extraction individuelle...");
                    items = ExtraireObjetsJSON(jsonPart);
                }

                if (items == null) return result;

                int n = 1;
                foreach (var item in items)
                {
                    try
                    {
                        string type = StrAlt(item,
                            "type", "questionType", "question_type", "kind")
                            .ToUpperInvariant()
                            .Trim();

                        if (string.IsNullOrEmpty(type)) type = "QCM";
                        if (type.Contains("CHECK")) type = "CHECKBOX";
                        else if (type.Contains("REDAC") || type.Contains("OPEN") || type.Contains("LIBRE")) type = "REDACTION";
                        else if (type.Contains("IMAGE") || type.Contains("IMG")) type = "IMAGE";
                        else if (!type.StartsWith("QCM") && !type.StartsWith("CHECKBOX")
                                 && !type.StartsWith("REDACTION") && !type.StartsWith("IMAGE"))
                            type = "QCM";

                        string enonce = StrAlt(item,
                            "enonce", "enoncé", "question", "texte", "text",
                            "questionText", "question_text", "libelle", "intitule");

                        if (string.IsNullOrWhiteSpace(enonce)) continue;

                        var q = new QuestionExamen
                        {
                            Numero = n++,
                            Type = type,
                            Enonce = enonce,
                            Explication = StrAlt(item, "explication", "explanation",
                                                 "justification", "correction"),
                            Difficulte = "Moyen",
                        };

                        if (type is "QCM" or "IMAGE")
                        {
                            (q.OptionA, q.OptionB, q.OptionC, q.OptionD) = ExtraireOptions(item);
                            q.ReponseCorrecte = StrAlt(item,
                                "reponseCorrecte", "reponse_correcte", "reponse",
                                "correctAnswer", "correct", "answer", "bonne_reponse")
                                .ToUpper().Trim();

                            q.ReponseCorrecte = q.ReponseCorrecte switch
                            {
                                "1" => "A",
                                "2" => "B",
                                "3" => "C",
                                "4" => "D",
                                _ => q.ReponseCorrecte
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

        // ── Helpers parseur ───────────────────────────────────────────────────

        private static string ExtraireTableau(string texte)
        {
            int start = texte.IndexOf('[');
            if (start == -1)
            {
                int ob = texte.IndexOf('{');
                if (ob == -1) return string.Empty;
                int inner = texte.IndexOf('[', ob);
                if (inner == -1) return string.Empty;
                start = inner;
            }

            int depth = 0;
            for (int i = start; i < texte.Length; i++)
            {
                if (texte[i] == '[') depth++;
                else if (texte[i] == ']') { depth--; if (depth == 0) return texte[start..(i + 1)]; }
            }

            // ✅ Si le tableau n'est pas fermé (JSON tronqué), essayer de récupérer ce qu'on a
            if (depth > 0 && start >= 0)
            {
                System.Diagnostics.Debug.WriteLine("[ExtraireTableau] JSON tronqué, tentative récupération partielle.");
                return ExtraireTableauPartiel(texte, start);
            }

            return string.Empty;
        }

        /// <summary>
        /// Tente de récupérer le maximum d'objets JSON complets d'un tableau tronqué.
        /// </summary>
        private static string ExtraireTableauPartiel(string texte, int start)
        {
            // Trouver tous les objets JSON complets (profondeur objet = 0 après chaque })
            var objets = new List<string>();
            int i = start + 1; // sauter le [
            int depth = 0;
            int objStart = -1;

            while (i < texte.Length)
            {
                char c = texte[i];

                if (c == '{')
                {
                    if (depth == 0) objStart = i;
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0 && objStart >= 0)
                    {
                        objets.Add(texte[objStart..(i + 1)]);
                        objStart = -1;
                    }
                }
                i++;
            }

            if (objets.Count == 0) return string.Empty;

            return "[" + string.Join(",", objets) + "]";
        }

        /// <summary>
        /// Tente de réparer un JSON invalide en cherchant des objets individuels.
        /// </summary>
        private static string TenterReparerJSON(string texte)
        {
            // Chercher tous les objets JSON { ... } au premier niveau
            var objets = new List<string>();
            int i = 0;
            int depth = 0;
            int objStart = -1;

            while (i < texte.Length)
            {
                char c = texte[i];
                if (c == '{')
                {
                    if (depth == 0) objStart = i;
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0 && objStart >= 0)
                    {
                        string obj = texte[objStart..(i + 1)];
                        // Vérifier que c'est un objet JSON valide
                        try
                        {
                            JsonDocument.Parse(obj);
                            objets.Add(obj);
                        }
                        catch { }
                        objStart = -1;
                    }
                }
                i++;
            }

            if (objets.Count == 0) return string.Empty;
            return "[" + string.Join(",", objets) + "]";
        }

        /// <summary>
        /// Extrait les objets JSON individuels depuis un tableau JSON partiellement invalide.
        /// </summary>
        private static List<JsonElement> ExtraireObjetsJSON(string jsonArray)
        {
            var result = new List<JsonElement>();
            int i = 0;
            int depth = 0;
            int start = -1;

            // Passer le premier '['
            while (i < jsonArray.Length && jsonArray[i] != '[') i++;
            i++;

            while (i < jsonArray.Length)
            {
                char c = jsonArray[i];
                if (c == '{')
                {
                    if (depth == 0) start = i;
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 0 && start >= 0)
                    {
                        string obj = jsonArray[start..(i + 1)];
                        try
                        {
                            using var doc = JsonDocument.Parse(obj);
                            result.Add(doc.RootElement.Clone());
                        }
                        catch { }
                        start = -1;
                    }
                }
                i++;
            }

            return result;
        }

        private static (string A, string B, string C, string D) ExtraireOptions(JsonElement el)
        {
            string a = StrAlt(el, "optionA", "option_a", "choixA", "a");
            string b = StrAlt(el, "optionB", "option_b", "choixB", "b");
            string c = StrAlt(el, "optionC", "option_c", "choixC", "c");
            string d = StrAlt(el, "optionD", "option_d", "choixD", "d");

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

        private static void ExtraireReponsesCheckbox(JsonElement el, QuestionExamen q)
        {
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

            q.OptionACorrecte = StrAlt(el, "correcteA", "correctA", "aCorrect")
                                    .Equals("true", StringComparison.OrdinalIgnoreCase);
            q.OptionBCorrecte = StrAlt(el, "correcteB", "correctB", "bCorrect")
                                    .Equals("true", StringComparison.OrdinalIgnoreCase);
            q.OptionCCorrecte = StrAlt(el, "correcteC", "correctC", "cCorrect")
                                    .Equals("true", StringComparison.OrdinalIgnoreCase);
            q.OptionDCorrecte = StrAlt(el, "correcteD", "correctD", "dCorrect")
                                    .Equals("true", StringComparison.OrdinalIgnoreCase);
        }

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