using smartest_desktop.Constants;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Exceptions;
using smartest_desktop.Helpers;
using smartest_desktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json;
using System.Windows.Threading;
using WpfApp = System.Windows.Application;

namespace smartest_desktop.ViewModels
{
    /// <summary>Ligne liste quiz : quiz + titre affiché (suffixe (2), (3) si même titre).</summary>
    public sealed class QuizListeRow : BaseViewModel
    {
        public QuizLocal Quiz { get; init; } = null!;
        public string TitreAffiche { get; init; } = "";

        private bool _isPublicationEnCours;
        public bool IsPublicationEnCours
        {
            get => _isPublicationEnCours;
            set
            {
                if (!SetProperty(ref _isPublicationEnCours, value))
                    return;
                OnPropertyChanged(nameof(LibelleBoutonPublication));
                OnPropertyChanged(nameof(PeutPublier));
                OnPropertyChanged(nameof(PeutOuvrirCodeQr));
            }
        }

        /// <summary>Fenêtre d’affichage du libellé « … en cours » ; le bouton reste désactivé tant que <see cref="IsPublicationEnCours"/>.</summary>
        private bool _afficheLibelleCourtePublicationEnCours;
        public bool AfficheLibelleCourtePublicationEnCours
        {
            get => _afficheLibelleCourtePublicationEnCours;
            set
            {
                if (!SetProperty(ref _afficheLibelleCourtePublicationEnCours, value))
                    return;
                OnPropertyChanged(nameof(LibelleBoutonPublication));
            }
        }

        private static bool EstDejaPublieSurLeWeb(QuizLocal? q) =>
            string.Equals(q?.Statut?.Trim(), "Publié", StringComparison.OrdinalIgnoreCase);

        public string LibelleBoutonPublication
        {
            get
            {
                if (IsPublicationEnCours && AfficheLibelleCourtePublicationEnCours)
                    return EstDejaPublieSurLeWeb(Quiz)
                        ? "Synchronisation en cours..."
                        : "Publication en cours...";

                return EstDejaPublieSurLeWeb(Quiz) ? "Synchroniser" : "Publier";
            }
        }

        public bool PeutPublier =>
            !IsPublicationEnCours &&
            QuizExamenViewModel.EstQuizEligibleBoutonPublicationWeb(Quiz);

        /// <summary>
        /// Le passage sur la plateforme web (liste étudiants) n'est actif qu'après « Publier sur le web » (statut Publié).
        /// Le QR temps réel est une session éphémère uniquement en mémoire serveur (<c>/api/qr-live</c> + STOMP) :
        /// aucune écriture MySQL (<c>quiz</c>) lors du clic « Code QR ».
        /// </summary>
        public bool PeutOuvrirCodeQr =>
            !IsPublicationEnCours &&
            Quiz != null &&
            Quiz.NombreQuestions > 0;
    }

    /// <summary>Ligne liste examen : examen + titre affiché (suffixe (2), (3) si même titre).</summary>
    public sealed class ExamenListeRow
    {
        public ExamenLocal Examen { get; init; } = null!;
        public string TitreAffiche { get; init; } = "";
    }

    public class QuizExamenViewModel : BaseViewModel
    {
        /// <summary>Première publication (Validé) ou synchro emails après publication (Publié).</summary>
        internal static bool EstQuizEligibleBoutonPublicationWeb(QuizLocal? q)
        {
            if (q == null)
                return false;
            var s = q.Statut?.Trim();
            return string.Equals(s, "Validé", StringComparison.OrdinalIgnoreCase)
                || string.Equals(s, "Publié", StringComparison.OrdinalIgnoreCase);
        }

        private const int TaillePage = 5;
        /// <summary>Durée d’affichage du texte « Publication / Synchronisation en cours » (le traitement peut être plus long).</summary>
        private const int DureeAffichageLibellePublicationMs = 6000;
        private const string LibelleCodeQr = "Code QR";
        private const string LibellePublicationWeb = "Publication web";
        private const string DefaultWebHost = "localhost";
        private const int DefaultWebPort = 5173;

        private readonly LocalQuizService _quizService;
        private readonly LocalExamenService _examenService;

        private readonly List<QuizLocal> _quizTous = new();
        private readonly List<ExamenLocal> _examensTous = new();
        private readonly HashSet<int> _quizPublicationEnCoursIds = new();
        /// <summary>Fin d’affichage du libellé court « en cours » par quiz (UTC).</summary>
        private readonly Dictionary<int, DateTime> _quizPublicationLibelleCourExpireUtc = new();

        /// <summary>Éléments affichés sur la page courante (quiz).</summary>
        public ObservableCollection<QuizListeRow> QuizPage { get; } = new();

        /// <summary>Éléments affichés sur la page courante (examens).</summary>
        public ObservableCollection<ExamenListeRow> ExamensPage { get; } = new();

        private string _filtreQuiz = string.Empty;
        public string FiltreQuiz
        {
            get => _filtreQuiz;
            set
            {
                if (SetProperty(ref _filtreQuiz, value))
                {
                    _pageQuiz = 1;
                    OnPropertyChanged(nameof(PageQuiz));
                    RafraichirPaginationQuiz();
                }
            }
        }

        private string _filtreExamen = string.Empty;
        public string FiltreExamen
        {
            get => _filtreExamen;
            set
            {
                if (SetProperty(ref _filtreExamen, value))
                {
                    _pageExamen = 1;
                    OnPropertyChanged(nameof(PageExamen));
                    RafraichirPaginationExamens();
                }
            }
        }

        private int _pageQuiz = 1;
        public int PageQuiz
        {
            get => _pageQuiz;
            set
            {
                if (SetProperty(ref _pageQuiz, value))
                    RafraichirPaginationQuiz();
            }
        }

        private int _pageExamen = 1;
        public int PageExamen
        {
            get => _pageExamen;
            set
            {
                if (SetProperty(ref _pageExamen, value))
                    RafraichirPaginationExamens();
            }
        }

        public int PagesQuizTotal { get; private set; }
        public int PagesExamensTotal { get; private set; }

        public string LibellePaginationQuiz => $"{PageQuiz} / {PagesQuizTotal}";

        public string LibellePaginationExamen => $"{PageExamen} / {PagesExamensTotal}";

        private QuizLocal? _selectedQuiz;
        private ExamenLocal? _selectedExamen;

        public QuizLocal? SelectedQuiz
        {
            get => _selectedQuiz;
            set => SetProperty(ref _selectedQuiz, value);
        }

        public ExamenLocal? SelectedExamen
        {
            get => _selectedExamen;
            set => SetProperty(ref _selectedExamen, value);
        }

        private int _totalQuiz;
        private int _totalExamens;

        public int TotalQuiz
        {
            get => _totalQuiz;
            set => SetProperty(ref _totalQuiz, value);
        }

        public int TotalExamens
        {
            get => _totalExamens;
            set => SetProperty(ref _totalExamens, value);
        }

        /// <summary>Nombre de quiz après filtre de recherche.</summary>
        public int ResultatsQuizFiltres { get; private set; }

        /// <summary>Nombre d'examens après filtre de recherche.</summary>
        public int ResultatsExamensFiltres { get; private set; }

        public bool HasNoQuiz => _quizTous.Count == 0;

        public bool HasNoQuizRecherche =>
            _quizTous.Count > 0 && ResultatsQuizFiltres == 0;

        public bool HasNoExamens => _examensTous.Count == 0;

        public bool HasNoExamensRecherche =>
            _examensTous.Count > 0 && ResultatsExamensFiltres == 0;

        public bool AfficherListeQuiz => ResultatsQuizFiltres > 0;

        public bool AfficherListeExamens => ResultatsExamensFiltres > 0;

        public static string Nom => WpfApp.Current.Properties["Nom"]?.ToString() ?? "Professeur";
        public static string Email => WpfApp.Current.Properties["Email"]?.ToString() ?? "";

        public event Action? NavigateToQuizGeneration;
        public event Action? NavigateToExamenGeneration;
        public event Action? NavigateToDashboard;
        public event Action<QuizLocal>? NavigateToQuizDetails;
        public event Action<ExamenLocal>? NavigateToExamenDetails;
        public event Action? OuvrirStatistiques;

        public ICommand GenererQuizCommand { get; }
        public ICommand GenererExamenCommand { get; }
        public ICommand RetourDashboardCommand { get; }
        public ICommand OuvrirStatistiquesCommand { get; }
        public ICommand LogoutCommand { get; }

        public ICommand SupprimerQuizCommand { get; }
        public ICommand PublierSurLeWebCommand { get; }

        public ICommand SupprimerExamenCommand { get; }
        public ICommand PublierExamenSurLeWebCommand { get; }
        public ICommand LancerExamenCommand { get; }
        public ICommand OuvrirQuizCommand { get; }
        public ICommand OuvrirCodeQrQuizCommand { get; }
        public ICommand OuvrirExamenCommand { get; }

        public ICommand QuizPagePrecedenteCommand { get; }
        public ICommand QuizPageSuivanteCommand { get; }
        public ICommand ExamenPagePrecedenteCommand { get; }
        public ICommand ExamenPageSuivanteCommand { get; }

        /// <summary>Réévalue périodiquement « Lancer » (supervision) pour activer le bouton à l’heure du créneau.</summary>
        private readonly DispatcherTimer _timerMiseAJourLancerExamen;

        public QuizExamenViewModel()
        {
            _quizService = new LocalQuizService(App.LocalDb);
            _examenService = new LocalExamenService(App.LocalDb);

            GenererQuizCommand = new RelayCommand(_ => NavigateToQuizGeneration?.Invoke());
            GenererExamenCommand = new RelayCommand(_ => NavigateToExamenGeneration?.Invoke());
            RetourDashboardCommand = new RelayCommand(_ => NavigateToDashboard?.Invoke());
            OuvrirStatistiquesCommand = new RelayCommand(_ => OuvrirStatistiques?.Invoke());
            LogoutCommand = new RelayCommand(_ => ExecuteLogout());

            SupprimerQuizCommand = new RelayCommand(
                async p => await SupprimerQuizAsync(p),
                p => p is QuizListeRow);

            PublierSurLeWebCommand = new RelayCommand(
                async p => await PublierSurLeWebAsync(p),
                p => p is QuizListeRow row &&
                     !_quizPublicationEnCoursIds.Contains(row.Quiz.Id) &&
                     EstQuizEligibleBoutonPublicationWeb(row.Quiz));

            SupprimerExamenCommand = new RelayCommand(
                async p => await SupprimerExamenAsync(p),
                p => p is ExamenListeRow);

            PublierExamenSurLeWebCommand = new RelayCommand(
                async p => await PublierExamenSurLeWebAsync(p),
                p => p is ExamenListeRow { Examen: var ex } && EstExamenPubliableSurLeWeb(ex));

            LancerExamenCommand = new RelayCommand(
                p => _ = OuvrirSupervisionExamenAsync(p),
                p => p is ExamenListeRow { Examen: var e } &&
                     EstExamenPublieSurLeWebPourSupervision(e) &&
                     EstCreneauLancementExamenAtteint(e));

            OuvrirQuizCommand = new RelayCommand(
                p =>
                {
                    if (p is not QuizListeRow row) return;
                    NavigateToQuizDetails?.Invoke(row.Quiz);
                },
                p => p is QuizListeRow);

            // Ne pas restreindre CanExecute à PeutOuvrirCodeQr : cette propriété calculée ne notifie pas
            // toujours WPF, et le bouton reste alors grisé sans explication. La logique et les messages
            // d’erreur sont gérés dans OuvrirCodeQrQuizAsync.
            OuvrirCodeQrQuizCommand = new RelayCommand(
                async p => await OuvrirCodeQrQuizAsync(p),
                p => p is QuizListeRow);

            OuvrirExamenCommand = new RelayCommand(
                p =>
                {
                    if (p is not ExamenListeRow row) return;
                    NavigateToExamenDetails?.Invoke(row.Examen);
                },
                p => p is ExamenListeRow);

            QuizPagePrecedenteCommand = new RelayCommand(
                _ => { if (PageQuiz > 1) PageQuiz--; },
                _ => PageQuiz > 1);

            QuizPageSuivanteCommand = new RelayCommand(
                _ => { if (PageQuiz < PagesQuizTotal) PageQuiz++; },
                _ => PageQuiz < PagesQuizTotal);

            ExamenPagePrecedenteCommand = new RelayCommand(
                _ => { if (PageExamen > 1) PageExamen--; },
                _ => PageExamen > 1);

            ExamenPageSuivanteCommand = new RelayCommand(
                _ => { if (PageExamen < PagesExamensTotal) PageExamen++; },
                _ => PageExamen < PagesExamensTotal);

            _ = ChargerDonneesAsync();

            _timerMiseAJourLancerExamen = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(15),
            };
            _timerMiseAJourLancerExamen.Tick += (_, _) =>
            {
                if (LancerExamenCommand is RelayCommand r)
                    r.RaiseCanExecuteChanged();
            };
            _timerMiseAJourLancerExamen.Start();
        }

        private List<QuizLocal> ObtenirQuizFiltres()
        {
            var f = FiltreQuiz.Trim();
            if (string.IsNullOrEmpty(f))
                return _quizTous.ToList();

            return _quizTous.Where(q =>
                (q.Titre?.Contains(f, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                (q.CoursSourceTitre?.Contains(f, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                (q.Statut?.Contains(f, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                (q.Difficulte?.Contains(f, System.StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
        }

        /// <summary>Même titre (insensible à la casse) : 1er sans suffixe, suivants « Titre (2) », « Titre (3) » (ordre par date décroissante).</summary>
        private static Dictionary<int, string> CalculerTitresAffiche(List<QuizLocal> filteredOrdered)
        {
            var map = new Dictionary<int, string>();
            var groups = filteredOrdered
                .GroupBy(q => (q.Titre ?? "").Trim(), System.StringComparer.OrdinalIgnoreCase);
            foreach (var grp in groups)
            {
                var list = grp.OrderByDescending(q => q.DateCreation).ToList();
                if (list.Count == 1)
                {
                    map[list[0].Id] = list[0].Titre ?? "";
                    continue;
                }

                for (int i = 0; i < list.Count; i++)
                {
                    var q = list[i];
                    var t = q.Titre ?? "";
                    map[q.Id] = i == 0 ? t : $"{t} ({i + 1})";
                }
            }

            return map;
        }

        /// <summary>Même titre (insensible à la casse) : 1er sans suffixe, suivants « Titre (2) », « Titre (3) ».</summary>
        private static Dictionary<int, string> CalculerTitresAfficheExamens(List<ExamenLocal> filteredOrdered)
        {
            var map = new Dictionary<int, string>();
            var groups = filteredOrdered
                .GroupBy(e => (e.Titre ?? "").Trim(), System.StringComparer.OrdinalIgnoreCase);
            foreach (var grp in groups)
            {
                var list = grp.OrderByDescending(e => e.DateCreation).ToList();
                if (list.Count == 1)
                {
                    map[list[0].Id] = list[0].Titre ?? "";
                    continue;
                }

                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    var t = e.Titre ?? "";
                    map[e.Id] = i == 0 ? t : $"{t} ({i + 1})";
                }
            }

            return map;
        }

        private List<ExamenLocal> ObtenirExamensFiltres()
        {
            var f = FiltreExamen.Trim();
            if (string.IsNullOrEmpty(f))
                return _examensTous.ToList();

            return _examensTous.Where(e =>
                (e.Titre?.Contains(f, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.Statut?.Contains(f, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.Description?.Contains(f, System.StringComparison.OrdinalIgnoreCase) ?? false)
            ).ToList();
        }

        private void RafraichirPaginationQuiz()
        {
            var filtered = ObtenirQuizFiltres();
            ResultatsQuizFiltres = filtered.Count;
            OnPropertyChanged(nameof(ResultatsQuizFiltres));

            PagesQuizTotal = filtered.Count == 0
                ? 1
                : (int)System.Math.Ceiling(filtered.Count / (double)TaillePage);

            if (_pageQuiz > PagesQuizTotal) _pageQuiz = PagesQuizTotal;
            if (_pageQuiz < 1) _pageQuiz = 1;
            OnPropertyChanged(nameof(PageQuiz));

            QuizPage.Clear();
            if (filtered.Count > 0)
            {
                var titres = CalculerTitresAffiche(filtered);
                int skip = (_pageQuiz - 1) * TaillePage;
                foreach (var q in filtered.Skip(skip).Take(TaillePage))
                {
                    var libelle = titres.TryGetValue(q.Id, out var t) ? t : (q.Titre ?? "");
                    bool pubEnCours = _quizPublicationEnCoursIds.Contains(q.Id);
                    var ligne = new QuizListeRow { Quiz = q, TitreAffiche = libelle };
                    ligne.IsPublicationEnCours = pubEnCours;
                    ligne.AfficheLibelleCourtePublicationEnCours =
                        pubEnCours && PublicationLibelleCourteEncoreVisible(q.Id);
                    QuizPage.Add(ligne);
                }
            }

            TotalQuiz = _quizTous.Count;
            OnPropertyChanged(nameof(LibellePaginationQuiz));
            OnPropertyChanged(nameof(PagesQuizTotal));
            OnPropertyChanged(nameof(HasNoQuiz));
            OnPropertyChanged(nameof(HasNoQuizRecherche));
            OnPropertyChanged(nameof(AfficherListeQuiz));

            if (QuizPagePrecedenteCommand is RelayCommand rp) rp.RaiseCanExecuteChanged();
            if (QuizPageSuivanteCommand is RelayCommand rs) rs.RaiseCanExecuteChanged();

            RafraichirCanExecutePublicationEtQr();
        }

        private bool PublicationLibelleCourteEncoreVisible(int quizId)
        {
            return _quizPublicationLibelleCourExpireUtc.TryGetValue(quizId, out var jusquA)
                && DateTime.UtcNow < jusquA;
        }

        private void PlanifierFinAffichageLibelleCourtePublication(int quizId)
        {
            _ = Task.Delay(DureeAffichageLibellePublicationMs).ContinueWith(
                _ =>
                {
                    WpfApp.Current?.Dispatcher.BeginInvoke(
                        new Action(() =>
                        {
                            if (!_quizPublicationEnCoursIds.Contains(quizId))
                                return;
                            RafraichirPaginationQuiz();
                        }));
                },
                TaskScheduler.Default);
        }

        private void RafraichirPaginationExamens()
        {
            var filtered = ObtenirExamensFiltres();
            ResultatsExamensFiltres = filtered.Count;
            OnPropertyChanged(nameof(ResultatsExamensFiltres));

            PagesExamensTotal = filtered.Count == 0
                ? 1
                : (int)System.Math.Ceiling(filtered.Count / (double)TaillePage);

            if (_pageExamen > PagesExamensTotal) _pageExamen = PagesExamensTotal;
            if (_pageExamen < 1) _pageExamen = 1;
            OnPropertyChanged(nameof(PageExamen));

            ExamensPage.Clear();
            if (filtered.Count > 0)
            {
                var titres = CalculerTitresAfficheExamens(filtered);
                int skip = (_pageExamen - 1) * TaillePage;
                foreach (var e in filtered.Skip(skip).Take(TaillePage))
                {
                    var libelle = titres.TryGetValue(e.Id, out var t) ? t : (e.Titre ?? "");
                    ExamensPage.Add(new ExamenListeRow { Examen = e, TitreAffiche = libelle });
                }
            }

            TotalExamens = _examensTous.Count;
            OnPropertyChanged(nameof(LibellePaginationExamen));
            OnPropertyChanged(nameof(PagesExamensTotal));
            OnPropertyChanged(nameof(HasNoExamens));
            OnPropertyChanged(nameof(HasNoExamensRecherche));
            OnPropertyChanged(nameof(AfficherListeExamens));

            if (ExamenPagePrecedenteCommand is RelayCommand ep) ep.RaiseCanExecuteChanged();
            if (ExamenPageSuivanteCommand is RelayCommand es) es.RaiseCanExecuteChanged();
        }

        /// <summary>Rafraîchit les listes depuis SQLite (utilisable après navigation shell).</summary>
        public async Task ChargerDonneesAsync()
        {
            try
            {
                var quiz = await _quizService.GetAllAsync();
                var examens = await _examenService.GetAllAsync();

                _quizTous.Clear();
                _quizTous.AddRange(quiz.OrderByDescending(q => q.DateCreation));

                _examensTous.Clear();
                _examensTous.AddRange(examens.OrderByDescending(e => e.DateCreation));

                _pageQuiz = 1;
                _pageExamen = 1;
                OnPropertyChanged(nameof(PageQuiz));
                OnPropertyChanged(nameof(PageExamen));

                RafraichirPaginationQuiz();
                RafraichirPaginationExamens();

                if (PublierSurLeWebCommand is RelayCommand rpw)
                    rpw.RaiseCanExecuteChanged();
                if (PublierExamenSurLeWebCommand is RelayCommand rpe)
                    rpe.RaiseCanExecuteChanged();
                if (LancerExamenCommand is RelayCommand rle)
                    rle.RaiseCanExecuteChanged();
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement: {ex.Message}");
            }
        }

        private async Task SupprimerQuizAsync(object? parameter)
        {
            if (parameter is not QuizListeRow row) return;
            var quiz = row.Quiz;

            var result = MessageBox.Show(
                $"Supprimer le quiz « {row.TitreAffiche} » ?\n\n" +
                "Si ce quiz existe sur la plateforme (publication web ou code QR), il sera d’abord supprimé côté serveur " +
                "avec les résultats, statistiques et données associées. La copie locale n’est effacée qu’ensuite.\n\n" +
                "Si vous êtes connecté et qu’aucun quiz correspondant n’est trouvé sur le serveur, vous pourrez choisir " +
                "de supprimer uniquement les données sur cet ordinateur.\n\n" +
                "Cette action est définitive.",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            var token = WpfApp.Current.Properties["Token"]?.ToString();

            try
            {
                if (!await SupprimerLocalesEtServeurAvecConfirmationLocaleAsync(quiz.Id, token))
                    return;
            }
            catch (InvalidOperationException iex)
            {
                MessageBox.Show(
                    iex.Message,
                    "Suppression impossible",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            catch (SmartestApiException ex)
            {
                MessageBox.Show(
                    "La suppression sur le serveur a échoué. Le quiz n'a pas été supprimé localement non plus.\n\n" +
                    UserErrorMessage.FromText(ex.Message, "La suppression n'a pas pu etre finalisee pour le moment."),
                    "Synchronisation serveur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            catch (SmartestNetworkException ex)
            {
                MessageBox.Show(
                    "La suppression sur le serveur a échoué. Le quiz n'a pas été supprimé localement non plus.\n\n" +
                    UserErrorMessage.FromText(ex.Message, "La suppression n'a pas pu etre finalisee pour le moment."),
                    "Synchronisation serveur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            _quizTous.RemoveAll(q => q.Id == quiz.Id);
            RafraichirPaginationQuiz();
            if (SelectedQuiz?.Id == quiz.Id) SelectedQuiz = null;
            if (PublierSurLeWebCommand is RelayCommand rpw)
                rpw.RaiseCanExecuteChanged();
            if (OuvrirCodeQrQuizCommand is RelayCommand rqr)
                rqr.RaiseCanExecuteChanged();
        }

        private async Task<bool> SupprimerLocalesEtServeurAvecConfirmationLocaleAsync(int quizLocalId, string? token)
        {
            try
            {
                await _quizService.SupprimerLocalesEtServeurAsync(quizLocalId, token);
                return true;
            }
            catch (QuizDeleteLocalOnlyConfirmationRequiredException ex)
            {
                var confirmation = MessageBox.Show(
                    ex.Message + Environment.NewLine + Environment.NewLine +
                    "Souhaitez-vous supprimer uniquement les données sur cet ordinateur ?",
                    "Aucune copie correspondante sur le serveur",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmation != MessageBoxResult.Yes)
                    return false;

                await _quizService.SupprimerLocalesEtServeurAsync(
                    quizLocalId,
                    token,
                    forceLocalDeleteIfNoServerMatch: true);
                return true;
            }
        }

        private async Task OuvrirCodeQrQuizAsync(object? parameter)
        {
            if (parameter is not QuizListeRow row) return;
            var quiz = row.Quiz;

            if (row.IsPublicationEnCours)
            {
                MessageBox.Show(
                    "Une publication ou une synchronisation web est en cours pour ce quiz. " +
                    "Attendez la fin de l’opération avant d’ouvrir le code QR.",
                    LibelleCodeQr,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var token = WpfApp.Current.Properties["Token"]?.ToString();
            if (string.IsNullOrWhiteSpace(token))
            {
                MessageBox.Show(
                    "Session invalide ou expirée. Reconnectez-vous pour utiliser le code QR.",
                    LibelleCodeQr,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var quizCompletLocal = await _quizService.GetByIdAsync(quiz.Id);
            var questionsQr = quizCompletLocal?.Questions ?? new List<QuestionLocale>();
            if (questionsQr.Count == 0)
            {
                MessageBox.Show(
                    "Ce quiz ne contient aucune question locale. Ouvrez-le puis enregistrez au moins une question avant d'activer le QR.",
                    LibelleCodeQr,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var ancienJetonQr = quizCompletLocal?.QrLiveSessionToken?.Trim();
            if (!string.IsNullOrEmpty(ancienJetonQr))
                await QuizQrService.SupprimerSessionQrAsync(token.Trim(), ancienJetonQr).ConfigureAwait(true);

            // Pas de CreateQuiz / sync MySQL ici : le flux « Code QR » ne touche que la mémoire serveur
            // (POST /api/qr-live/sessions + STOMP /app/quiz-qr/prof/init). La table quiz n’est écrite que par « Publier ».

            string sessionJeton;
            try
            {
                sessionJeton = await QuizQrService
                    .InitialiserSessionQrSansUiAsync(token.Trim(), quizCompletLocal ?? quiz, questionsQr)
                    .ConfigureAwait(true);
            }
            catch (SmartestApiException ex)
            {
                MessageBox.Show(
                    UserErrorMessage.FromText(ex.Message, "Session QR impossible (serveur)."),
                    LibelleCodeQr,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            catch (SmartestNetworkException ex)
            {
                MessageBox.Show(
                    UserErrorMessage.FromText(ex.Message, "Connexion impossible pour ouvrir le mode QR."),
                    LibelleCodeQr,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    UserErrorMessage.FromText(ex.Message, "Impossible d’ouvrir le code QR."),
                    LibelleCodeQr,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    UserErrorMessage.FromException(ex, "Une erreur inattendue s’est produite lors de l’ouverture du code QR."),
                    LibelleCodeQr,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            await _quizService.DefinirQrLiveSessionTokenAsync(quiz.Id, sessionJeton).ConfigureAwait(true);

            string baseWeb = FrontendPublicUrl.Resolve().TrimEnd('/');
            string urlLive = $"{baseWeb}/quiz-live/{Uri.EscapeDataString(sessionJeton)}";

            try
            {
                Process.Start(new ProcessStartInfo { FileName = urlLive, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    UserErrorMessage.FromException(ex, "Le navigateur n’a pas pu être ouvert. Le lien a été copié dans le presse-papiers."),
                    LibelleCodeQr,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }

            try
            {
                Clipboard.SetText(urlLive);
            }
            catch
            {
                /* ignore */
            }

            MessageBox.Show(
                "Le lien du quiz en direct a été ouvert dans votre navigateur (si possible) et copié dans le presse-papiers :\n\n"
                + urlLive,
                LibelleCodeQr,
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private static bool TryGetSavedPublicationEmails(QuizLocal quiz, out List<string> emails)
        {
            emails = new List<string>();
            if (string.IsNullOrWhiteSpace(quiz.EmailsPublicationWebJson))
                return false;
            try
            {
                var list = JsonConvert.DeserializeObject<List<string>>(quiz.EmailsPublicationWebJson);
                if (list == null) return false;
                emails.AddRange(list);
                return emails.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private static string GetBaseWebUrl()
        {
            var configuredUrl = Environment.GetEnvironmentVariable("SMARTEST_WEB_URL");
            if (!string.IsNullOrWhiteSpace(configuredUrl))
            {
                return configuredUrl.TrimEnd('/');
            }

            return $"http://{DefaultWebHost}:{DefaultWebPort}";
        }

        private static bool TryGetTokenForPublication(out string token)
        {
            token = WpfApp.Current.Properties["Token"]?.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(token))
            {
                return true;
            }

            MessageBox.Show(
                "Session invalide ou expirée. Reconnectez-vous pour publier ou synchroniser sur le web.",
                LibellePublicationWeb,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return false;
        }

        private static bool TryPrepareEmailsForPublication(QuizLocal quiz, out List<string> emails)
        {
            emails = new List<string>();
            if (!TryGetSavedPublicationEmails(quiz, out emails) || emails.Count == 0)
            {
                MessageBox.Show(
                    "Ce quiz n'a pas de liste d'emails pour la publication web.\n\n" +
                    "Ouvrez le quiz depuis la liste (icône / ligne), puis dans la colonne de gauche " +
                    "remplissez ou importez les emails sous « Publication web », et validez. " +
                    "Vous pouvez aussi les définir dès « Générer un quiz » avant la première validation.",
                    LibellePublicationWeb,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return false;
            }

            if (emails.Count > QuizPublicationLimits.MaxAuthorizedStudentEmails)
            {
                MessageBox.Show(
                    $"La liste comporte plus de {QuizPublicationLimits.MaxAuthorizedStudentEmails} emails. " +
                    $"Seuls les {QuizPublicationLimits.MaxAuthorizedStudentEmails} premiers seront envoyés au serveur.",
                    LibellePublicationWeb,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                emails = emails.Take(QuizPublicationLimits.MaxAuthorizedStudentEmails).ToList();
            }
            return true;
        }

        private static bool ConfirmerPublication(
            QuizListeRow row,
            QuizLocal quiz,
            IReadOnlyCollection<string> emails,
            bool estSynchronisationEmails)
        {
            var titreQuiz = string.IsNullOrWhiteSpace(quiz.Titre) ? row.TitreAffiche : quiz.Titre.Trim();
            string corps;
            string titreBoite;
            if (estSynchronisationEmails)
            {
                titreBoite = $"{LibellePublicationWeb} — synchronisation";
                corps =
                    $"Synchroniser le quiz « {titreQuiz} » sur le web ?\n\n" +
                    $"La liste d’emails autorisés sera mise à jour sur le serveur ({emails.Count} adresse(s)). " +
                    "Les quiz déjà disponibles pour les étudiants restent inchangés hors liste d’accès.\n\n" +
                    "Confirmer la synchronisation ?";
            }
            else
            {
                titreBoite = $"{LibellePublicationWeb} — confirmation";
                corps =
                    $"Publier le quiz « {titreQuiz} » sur le web ?\n\n" +
                    $"{emails.Count} adresse(s) autorisée(s) : ces étudiants pourront voir et passer le quiz sur la plateforme. " +
                    "Après publication, utilisez « Synchroniser » pour mettre à jour la liste d’emails.\n\n" +
                    "Confirmer la publication ?";
            }

            var confPub = MessageBox.Show(corps, titreBoite, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return confPub == MessageBoxResult.Yes;
        }

        private void RafraichirCanExecutePublicationEtQr()
        {
            if (PublierSurLeWebCommand is RelayCommand publierCmd)
                publierCmd.RaiseCanExecuteChanged();
            if (OuvrirCodeQrQuizCommand is RelayCommand qrCmd)
                qrCmd.RaiseCanExecuteChanged();
        }

        /// <summary>Crée ou réutilise le quiz serveur réservé à la publication web (jamais l’id miroir QR).</summary>
        private static async Task<long?> TryEnsureBackendQuizIdPublicationWebAsync(
            QuizWebPublicationApiService api,
            QuizLocal quiz,
            string token)
        {
            try
            {
                var profId = await api.GetProfesseurIdAsync(token);

                long backendId = quiz.BackendQuizIdPublicationWeb ?? 0;
                if (backendId <= 0 && quiz.BackendQuizId is long leg && leg > 0)
                    backendId = leg;
                if (backendId > 0)
                    return backendId;

                return await api.CreateQuizAsync(
                    token,
                    string.IsNullOrWhiteSpace(quiz.Titre) ? "Quiz" : quiz.Titre.Trim(),
                    profId);
            }
            catch (SmartestApiException ex)
            {
                MessageBox.Show(UserErrorMessage.FromText(ex.Message, "La publication web n'a pas pu etre preparee."), LibellePublicationWeb, MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            catch (SmartestNetworkException ex)
            {
                MessageBox.Show(UserErrorMessage.FromText(ex.Message, "Connexion impossible pour publier sur le web."), LibellePublicationWeb, MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        private async Task PublierSurLeWebAsync(object? parameter)
        {
            if (parameter is not QuizListeRow row) return;
            var quiz = row.Quiz;
            if (!EstQuizEligibleBoutonPublicationWeb(quiz)) return;
            if (_quizPublicationEnCoursIds.Contains(quiz.Id)) return;

            bool estSynchronisationEmails = string.Equals(
                quiz.Statut?.Trim(),
                "Publié",
                StringComparison.OrdinalIgnoreCase);

            if (!TryGetTokenForPublication(out var token)) return;
            if (!TryPrepareEmailsForPublication(quiz, out var emails)) return;
            if (!ConfirmerPublication(row, quiz, emails, estSynchronisationEmails)) return;

            var publicationFeedbackDemarreUtc = DateTime.UtcNow;
            _quizPublicationLibelleCourExpireUtc[quiz.Id] =
                publicationFeedbackDemarreUtc.AddMilliseconds(DureeAffichageLibellePublicationMs);
            _quizPublicationEnCoursIds.Add(quiz.Id);
            row.IsPublicationEnCours = true;
            row.AfficheLibelleCourtePublicationEnCours = true;
            PlanifierFinAffichageLibelleCourtePublication(quiz.Id);
            RafraichirCanExecutePublicationEtQr();

            // Si la requête réussit vite, le nettoyage ne doit pas arriver avant ce délai (sinon le libellé « en cours » disparaît aussitôt).
            var attendreDelaiMinimumFeedback = false;

            var api = new QuizWebPublicationApiService();
            var backendId = await TryEnsureBackendQuizIdPublicationWebAsync(api, quiz, token);
            if (!backendId.HasValue)
                goto PublicationEpilogue;

            // Enregistrer l’ID serveur tout de suite : si POST publication-web échoue après création POST /api/quizs,
            // la suppression desktop pourra quand même appeler DELETE /api/quizs/{id}.
            var lienAvantPublication = await _quizService.GetByIdAsync(quiz.Id);
            if (lienAvantPublication != null
                && (lienAvantPublication.BackendQuizIdPublicationWeb == null || lienAvantPublication.BackendQuizIdPublicationWeb <= 0))
            {
                await _quizService.DefinirBackendQuizIdPublicationWebSiAbsenteAsync(quiz.Id, backendId.Value);
                await _quizService.MarquerServeurOuQrPotentiellementCreeAsync(quiz.Id);
                quiz.BackendQuizIdPublicationWeb = backendId.Value;
                quiz.BackendQuizId = backendId.Value;
            }

            var quizComplet = await _quizService.GetByIdAsync(quiz.Id);
            var questionsPublication = quizComplet?.Questions ?? new List<QuestionLocale>();

            try
            {
                await api.PostPublicationWebAsync(token, backendId.Value, emails, questionsPublication);
            }
            catch (SmartestApiException ex)
            {
                MessageBox.Show(UserErrorMessage.FromText(ex.Message, "La publication web a echoue. Reessayez."), LibellePublicationWeb, MessageBoxButton.OK, MessageBoxImage.Error);
                goto PublicationEpilogue;
            }
            catch (SmartestNetworkException ex)
            {
                MessageBox.Show(UserErrorMessage.FromText(ex.Message, "Connexion impossible pour publier sur le web."), LibellePublicationWeb, MessageBoxButton.OK, MessageBoxImage.Error);
                goto PublicationEpilogue;
            }

            string json = JsonConvert.SerializeObject(emails);
            await _quizService.MettreAJourPublicationWebLocaleAsync(quiz.Id, backendId.Value, json, "Publié");

            // Applique immédiatement l'état publié sur la ligne courante pour éviter
            // que "Publication en cours..." reste affiché jusqu'à un refresh d'écran.
            quiz.Statut = "Publié";
            row.IsPublicationEnCours = false;
            RafraichirCanExecutePublicationEtQr();

            await ChargerDonneesAsync();
            attendreDelaiMinimumFeedback = true;

        PublicationEpilogue:
            if (attendreDelaiMinimumFeedback)
                await AttendreDelaiMinimumFeedbackPublicationAsync(
                    publicationFeedbackDemarreUtc,
                    DureeAffichageLibellePublicationMs);

            _quizPublicationEnCoursIds.Remove(quiz.Id);
            _quizPublicationLibelleCourExpireUtc.Remove(quiz.Id);
            row.IsPublicationEnCours = false;
            row.AfficheLibelleCourtePublicationEnCours = false;
            RafraichirCanExecutePublicationEtQr();
            RafraichirPaginationQuiz();
        }

        /// <summary>Garantit que le feedback « … en cours » reste visible au moins <paramref name="dureeMinimumMs"/> après le clic si le travail réseau s’est terminé plus tôt.</summary>
        private static async Task AttendreDelaiMinimumFeedbackPublicationAsync(
            DateTime publicationFeedbackDemarreUtc,
            int dureeMinimumMs)
        {
            var ecouleMs = (DateTime.UtcNow - publicationFeedbackDemarreUtc).TotalMilliseconds;
            var restantMs = dureeMinimumMs - ecouleMs;
            if (restantMs > 0)
                await Task.Delay((int)Math.Ceiling(restantMs));
        }

        private async Task SupprimerExamenAsync(object? parameter)
        {
            if (parameter is not ExamenListeRow row) return;
            var examen = row.Examen;

            var result = MessageBox.Show(
                $"Supprimer l'examen « {row.TitreAffiche} » ?",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            await _examenService.SupprimerAsync(examen.Id);
            _examensTous.RemoveAll(e => e.Id == examen.Id);
            RafraichirPaginationExamens();
            if (SelectedExamen?.Id == examen.Id) SelectedExamen = null;
        }

        private static bool EstExamenPubliableSurLeWeb(ExamenLocal e)
        {
            if (!string.Equals(e.Statut?.Trim(), "BROUILLON", StringComparison.OrdinalIgnoreCase))
                return false;
            if (!e.DatePrevue.HasValue)
                return false;
            return TryGetSavedPublicationEmailsExamen(e, out var list) && list.Count > 0;
        }

        /// <summary>Publié sur le web : statut et identifiant serveur (session distante créée).</summary>
        private static bool EstExamenPublieSurLeWebPourSupervision(ExamenLocal e) =>
            string.Equals(e.Statut?.Trim(), "PUBLIE", StringComparison.OrdinalIgnoreCase) &&
            e.BackendId.HasValue &&
            e.BackendId.Value > 0;

        private static DateTime NormaliserDatePrevueEnLocal(DateTime d)
        {
            if (d.Kind == DateTimeKind.Utc)
                return d.ToLocalTime();
            return d;
        }

        /// <summary>Vrai lorsque l’heure actuelle est au moins égale à la date/heure prévue (créneau).</summary>
        private static bool EstCreneauLancementExamenAtteint(ExamenLocal e)
        {
            if (!e.DatePrevue.HasValue)
                return false;
            var debut = NormaliserDatePrevueEnLocal(e.DatePrevue.Value);
            return DateTime.Now >= debut;
        }

        private static string FormaterDatePrevuePourMessage(ExamenLocal e)
        {
            if (!e.DatePrevue.HasValue)
                return "(non définie)";
            var d = NormaliserDatePrevueEnLocal(e.DatePrevue.Value);
            return d.ToString("dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.GetCultureInfo("fr-FR"));
        }

        private static bool TryGetSavedPublicationEmailsExamen(ExamenLocal examen, out List<string> emails)
        {
            emails = new List<string>();
            if (string.IsNullOrWhiteSpace(examen.EmailsPublicationWebJson))
                return false;
            try
            {
                var list = JsonConvert.DeserializeObject<List<string>>(examen.EmailsPublicationWebJson);
                if (list == null) return false;
                emails.AddRange(list.Where(s => !string.IsNullOrWhiteSpace(s)));
                return emails.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private async Task PublierExamenSurLeWebAsync(object? parameter)
        {
            if (parameter is not ExamenListeRow row) return;
            var examen = row.Examen;
            if (!EstExamenPubliableSurLeWeb(examen)) return;

            var token = WpfApp.Current.Properties["Token"]?.ToString();
            if (string.IsNullOrWhiteSpace(token))
            {
                MessageBox.Show(
                    "Session invalide ou expirée. Reconnectez-vous pour publier sur le web.",
                    "Publication web",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!TryGetSavedPublicationEmailsExamen(examen, out var emails) || emails.Count == 0)
            {
                MessageBox.Show(
                    "Cet examen n'a pas de liste d'emails pour la publication web.\n\n" +
                    "Ouvrez l'examen depuis la liste et remplissez la section « Publication et créneau », puis validez.",
                    "Publication web",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (!examen.DatePrevue.HasValue)
            {
                MessageBox.Show(
                    "Définissez la date et l'heure de lancement de l'examen (écran de révision, section créneau).",
                    "Publication web",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (emails.Count > QuizPublicationLimits.MaxAuthorizedStudentEmails)
            {
                MessageBox.Show(
                    $"La liste comporte plus de {QuizPublicationLimits.MaxAuthorizedStudentEmails} emails. " +
                    $"Seuls les {QuizPublicationLimits.MaxAuthorizedStudentEmails} premiers seront envoyés au serveur.",
                    "Publication web",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                emails = emails.Take(QuizPublicationLimits.MaxAuthorizedStudentEmails).ToList();
            }

            string titreEx = string.IsNullOrWhiteSpace(examen.Titre) ? row.TitreAffiche : examen.Titre.Trim();
            var debut = examen.DatePrevue.Value;
            if (debut.Kind == DateTimeKind.Utc)
                debut = debut.ToLocalTime();
            var conf = MessageBox.Show(
                $"Publier l'examen « {titreEx} » sur le web ?\n\n" +
                $"{emails.Count} adresse(s) autorisée(s). " +
                $"Début : {debut:dd/MM/yyyy HH:mm} — durée {examen.Duree} min (fin prévue côté serveur : {debut.AddMinutes(Math.Max(1, examen.Duree)):dd/MM/yyyy HH:mm}).\n\n" +
                "Confirmer la publication ?",
                "Publication web — examen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (conf != MessageBoxResult.Yes) return;

            try
            {
                var api = new ExamenWebPublicationApiService();
                long profId = await api.GetProfesseurIdAsync(token);
                long backendId = examen.BackendId ?? 0;
                var fin = debut.AddMinutes(Math.Max(1, examen.Duree));
                string desc = string.IsNullOrWhiteSpace(examen.Description) ? string.Empty : examen.Description.Trim();

                if (backendId <= 0)
                {
                    backendId = await api.CreateExamenAsync(
                        token, profId, titreEx, examen.Duree, desc, debut, fin);
                }

                var examContenu = await _examenService.GetByIdAsync(examen.Id);
                var qs = examContenu?.Questions ?? new List<QuestionLocale>();
                await api.SynchroniserQuestionsPublicationWebAsync(token, backendId, qs);

                await api.DefinirEmailsAutorisesAsync(token, backendId, emails);
                string json = JsonConvert.SerializeObject(emails);
                await _examenService.MettreAJourPublicationWebLocaleAsync(examen.Id, backendId, json, "PUBLIE");
                await ChargerDonneesAsync();
                var syncCount = qs
                    .Where(q => !string.IsNullOrWhiteSpace(q.Enonce)
                        && new[] { q.OptionA, q.OptionB, q.OptionC, q.OptionD }.Count(s => !string.IsNullOrWhiteSpace(s)) >= 2)
                    .Count();
                MessageBox.Show(
                    syncCount > 0
                        ? $"L'examen a été publié sur le web.\n\n{syncCount} question(s) avec propositions ont été enregistrées sur le serveur (supervision et passage élèves).\n\nSi la supervision web affichait « Aucune question », rouvrez-la après cette publication."
                        : "L'examen a été publié sur le web. Les étudiants listés verront l'épreuve à partir de la date prévue.",
                    "Publication web",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (SmartestApiException ex)
            {
                MessageBox.Show(ex.Message, "Publication web", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (SmartestNetworkException ex)
            {
                MessageBox.Show(ex.Message, "Publication web", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur inattendue : {ex.Message}",
                    "Publication web",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task OuvrirSupervisionExamenAsync(object? parameter)
        {
            if (parameter is not ExamenListeRow row) return;
            var examen = row.Examen;

            if (!EstExamenPublieSurLeWebPourSupervision(examen))
            {
                MessageBox.Show(
                    "Vous devez d'abord publier cet examen sur le web (bouton « Publier » : emails autorisés + créneau). " +
                    "Sans publication, la supervision et le lancement côté élèves ne sont pas disponibles.",
                    "Lancement / supervision",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            if (!examen.DatePrevue.HasValue)
            {
                MessageBox.Show(
                    "La date et l'heure de l'examen ne sont pas renseignées. Définissez le créneau dans l'écran de révision, puis republiez si nécessaire.",
                    "Créneau",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!EstCreneauLancementExamenAtteint(examen))
            {
                MessageBox.Show(
                    "Vous ne pouvez pas ouvrir la supervision pour lancer l'écran élève avant l'heure prévue du créneau.\n\n" +
                    $"Heure prévue : {FormaterDatePrevuePourMessage(examen)}\n\n" +
                    "Revenez à ce moment ou attendez que l'heure soit atteinte (le bouton « Lancer » s'activera automatiquement).",
                    "Créneau non atteint",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            string url = $"http://localhost:5173/supervision/examen/{examen.BackendId.Value}";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Impossible d'ouvrir automatiquement la supervision web.\n\n" +
                    $"Ouvrez manuellement cette adresse :\n{url}\n\n({ex.Message})",
                    "Supervision",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            await ChargerDonneesAsync();
        }

        private static void ExecuteLogout()
        {
            var result = MessageBox.Show(
                "Voulez-vous vraiment vous déconnecter ?",
                "Déconnexion", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;
            App.Deconnecter();
        }
    }
}
