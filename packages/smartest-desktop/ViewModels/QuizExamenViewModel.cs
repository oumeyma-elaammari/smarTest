using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Helpers;
using smartest_desktop.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfApp = System.Windows.Application;

namespace smartest_desktop.ViewModels
{
    public class QuizExamenViewModel : BaseViewModel
    {
        private readonly LocalQuizService _quizService;
        private readonly LocalExamenService _examenService;

        // ── Collections ──────────────────────────────────────────
        public ObservableCollection<QuizLocal> Quiz { get; } = new();
        public ObservableCollection<ExamenLocal> Examens { get; } = new();

        // ── Selected items ────────────────────────────────────────
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

        // ── Stats ─────────────────────────────────────────────────
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

        public bool HasNoQuiz => Quiz.Count == 0;
        public bool HasNoExamens => Examens.Count == 0;

        // ── Nom/Email sidebar ─────────────────────────────────────
        public string Nom => WpfApp.Current.Properties["Nom"]?.ToString() ?? "Professeur";
        public string Email => WpfApp.Current.Properties["Email"]?.ToString() ?? "";

        // ── Events navigation ─────────────────────────────────────
        public event Action? NavigateToQuizGeneration;
        public event Action? NavigateToExamenGeneration;
        public event Action? NavigateToDashboard;

        // ── Commands ──────────────────────────────────────────────
        public ICommand GenererQuizCommand { get; }
        public ICommand GenererExamenCommand { get; }
        public ICommand RetourDashboardCommand { get; }
        public ICommand LogoutCommand { get; }

        // Quiz CRUD
        public ICommand SupprimerQuizCommand { get; }
        public ICommand PublierQuizCommand { get; }

        // Examen CRUD
        public ICommand SupprimerExamenCommand { get; }
        public ICommand LancerExamenCommand { get; }

        public QuizExamenViewModel()
        {
            _quizService = new LocalQuizService(App.LocalDb);
            _examenService = new LocalExamenService(App.LocalDb);

            GenererQuizCommand   = new RelayCommand(_ => NavigateToQuizGeneration?.Invoke());
            GenererExamenCommand = new RelayCommand(_ => NavigateToExamenGeneration?.Invoke());
            RetourDashboardCommand = new RelayCommand(_ => NavigateToDashboard?.Invoke());
            LogoutCommand = new RelayCommand(_ => ExecuteLogout());

            SupprimerQuizCommand = new RelayCommand(async _ => await SupprimerQuizAsync(),
                                         _ => SelectedQuiz != null);
            PublierQuizCommand = new RelayCommand(async _ => await PublierQuizAsync(),
                                         _ => SelectedQuiz?.Statut == "Brouillon");

            SupprimerExamenCommand = new RelayCommand(async _ => await SupprimerExamenAsync(),
                                         _ => SelectedExamen != null);
            LancerExamenCommand = new RelayCommand(_ => LancerExamen(),
                                         _ => SelectedExamen?.Statut == "PUBLIE");

            _ = ChargerDonneesAsync();
        }

        // ── Chargement ────────────────────────────────────────────
        private async Task ChargerDonneesAsync()
        {
            try
            {
                var quiz = await _quizService.GetAllAsync();
                var examens = await _examenService.GetAllAsync();

                Quiz.Clear();
                foreach (var q in quiz) Quiz.Add(q);

                Examens.Clear();
                foreach (var e in examens) Examens.Add(e);

                TotalQuiz = Quiz.Count;
                TotalExamens = Examens.Count;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement: {ex.Message}");
            }
        }

        // ── CRUD Quiz ─────────────────────────────────────────────
        private async Task SupprimerQuizAsync()
        {
            if (SelectedQuiz == null) return;

            var result = MessageBox.Show(
                $"Supprimer le quiz \"{SelectedQuiz.Titre}\" ?",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            await _quizService.SupprimerAsync(SelectedQuiz.Id);
            Quiz.Remove(SelectedQuiz);
            TotalQuiz = Quiz.Count;
            SelectedQuiz = null;
        }

        private async Task PublierQuizAsync()
        {
            if (SelectedQuiz == null) return;

            await _quizService.ChangerStatutAsync(SelectedQuiz.Id, "Publié");
            SelectedQuiz.Statut = "Publié";

            // Forcer refresh
            var temp = SelectedQuiz;
            SelectedQuiz = null;
            SelectedQuiz = temp;
        }

        // ── CRUD Examen ───────────────────────────────────────────
        private async Task SupprimerExamenAsync()
        {
            if (SelectedExamen == null) return;

            var result = MessageBox.Show(
                $"Supprimer l'examen \"{SelectedExamen.Titre}\" ?",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            await _examenService.SupprimerAsync(SelectedExamen.Id);
            Examens.Remove(SelectedExamen);
            TotalExamens = Examens.Count;
            SelectedExamen = null;
        }

        private void LancerExamen()
        {
            // À implémenter — navigation vers SupervisionWindow
            MessageBox.Show("Lancement de session — Sprint 2",
                "Bientôt disponible", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ── Déconnexion ───────────────────────────────────────────
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