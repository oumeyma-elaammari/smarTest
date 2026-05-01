using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Helpers;
using smartest_desktop.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfApp = System.Windows.Application;

namespace smartest_desktop.ViewModels
{
    /// <summary>Ligne liste quiz : quiz + titre affiché (suffixe (2), (3) si même titre).</summary>
    public sealed class QuizListeRow
    {
        public QuizLocal Quiz { get; init; } = null!;
        public string TitreAffiche { get; init; } = "";
    }

    /// <summary>Ligne liste examen : examen + titre affiché (suffixe (2), (3) si même titre).</summary>
    public sealed class ExamenListeRow
    {
        public ExamenLocal Examen { get; init; } = null!;
        public string TitreAffiche { get; init; } = "";
    }

    public class QuizExamenViewModel : BaseViewModel
    {
        private const int TaillePage = 5;

        private readonly LocalQuizService _quizService;
        private readonly LocalExamenService _examenService;

        private readonly List<QuizLocal> _quizTous = new();
        private readonly List<ExamenLocal> _examensTous = new();

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

        public string Nom => WpfApp.Current.Properties["Nom"]?.ToString() ?? "Professeur";
        public string Email => WpfApp.Current.Properties["Email"]?.ToString() ?? "";

        public event Action? NavigateToQuizGeneration;
        public event Action? NavigateToExamenGeneration;
        public event Action? NavigateToDashboard;
        public event Action<QuizLocal>? NavigateToQuizDetails;
        public event Action<ExamenLocal>? NavigateToExamenDetails;

        public ICommand GenererQuizCommand { get; }
        public ICommand GenererExamenCommand { get; }
        public ICommand RetourDashboardCommand { get; }
        public ICommand LogoutCommand { get; }

        public ICommand SupprimerQuizCommand { get; }
        public ICommand PublierQuizCommand { get; }

        public ICommand SupprimerExamenCommand { get; }
        public ICommand LancerExamenCommand { get; }
        public ICommand OuvrirQuizCommand { get; }
        public ICommand OuvrirExamenCommand { get; }

        public ICommand QuizPagePrecedenteCommand { get; }
        public ICommand QuizPageSuivanteCommand { get; }
        public ICommand ExamenPagePrecedenteCommand { get; }
        public ICommand ExamenPageSuivanteCommand { get; }

        public QuizExamenViewModel()
        {
            _quizService = new LocalQuizService(App.LocalDb);
            _examenService = new LocalExamenService(App.LocalDb);

            GenererQuizCommand = new RelayCommand(_ => NavigateToQuizGeneration?.Invoke());
            GenererExamenCommand = new RelayCommand(_ => NavigateToExamenGeneration?.Invoke());
            RetourDashboardCommand = new RelayCommand(_ => NavigateToDashboard?.Invoke());
            LogoutCommand = new RelayCommand(_ => ExecuteLogout());

            SupprimerQuizCommand = new RelayCommand(
                async p => await SupprimerQuizAsync(p),
                p => p is QuizListeRow);

            PublierQuizCommand = new RelayCommand(
                async p => await PublierQuizAsync(p),
                p => p is QuizListeRow { Quiz: var q } && EstQuizPublisable(q));

            SupprimerExamenCommand = new RelayCommand(
                async p => await SupprimerExamenAsync(p),
                p => p is ExamenListeRow);

            LancerExamenCommand = new RelayCommand(
                p => LancerExamen(p),
                p => p is ExamenListeRow { Examen: var e } &&
                     string.Equals(e.Statut, "PUBLIE", System.StringComparison.OrdinalIgnoreCase));

            OuvrirQuizCommand = new RelayCommand(
                p =>
                {
                    if (p is not QuizListeRow row) return;
                    NavigateToQuizDetails?.Invoke(row.Quiz);
                },
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
                    QuizPage.Add(new QuizListeRow { Quiz = q, TitreAffiche = libelle });
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

        private static bool EstQuizPublisable(QuizLocal q)
        {
            var s = q.Statut?.Trim();
            return string.Equals(s, "Brouillon", System.StringComparison.OrdinalIgnoreCase)
                   || string.Equals(s, "BROUILLON", System.StringComparison.OrdinalIgnoreCase);
        }

        private async Task ChargerDonneesAsync()
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
                $"Supprimer le quiz « {row.TitreAffiche} » ?",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            await _quizService.SupprimerAsync(quiz.Id);
            _quizTous.RemoveAll(q => q.Id == quiz.Id);
            RafraichirPaginationQuiz();
            if (SelectedQuiz?.Id == quiz.Id) SelectedQuiz = null;
        }

        private async Task PublierQuizAsync(object? parameter)
        {
            if (parameter is not QuizListeRow row) return;
            var quiz = row.Quiz;
            if (!EstQuizPublisable(quiz)) return;

            await _quizService.ChangerStatutAsync(quiz.Id, "Publié");
            await ChargerDonneesAsync();
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

        private void LancerExamen(object? parameter)
        {
            if (parameter is not ExamenListeRow row) return;
            var examen = row.Examen;

            MessageBox.Show(
                $"Lancer la session pour « {examen.Titre} » (durée {examen.Duree} min).\n\n" +
                "La supervision temps réel sera disponible dans une prochaine version.",
                "Lancer l'examen",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ExecuteLogout()
        {
            var result = MessageBox.Show(
                "Voulez-vous vraiment vous déconnecter ?",
                "Déconnexion", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            WpfApp.Current.Properties["Token"] = null;
            WpfApp.Current.Properties["Nom"] = null;
            WpfApp.Current.Properties["Email"] = null;

            NavigateToDashboard?.Invoke();
        }
    }
}
