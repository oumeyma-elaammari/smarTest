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

        public ObservableCollection<QuizLocal> Quiz { get; } = new();
        public ObservableCollection<ExamenLocal> Examens { get; } = new();

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

        public bool HasNoQuiz => Quiz.Count == 0;
        public bool HasNoExamens => Examens.Count == 0;

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
                p => p is QuizLocal);

            PublierQuizCommand = new RelayCommand(
                async p => await PublierQuizAsync(p),
                p => p is QuizLocal q && EstQuizPublisable(q));

            SupprimerExamenCommand = new RelayCommand(
                async p => await SupprimerExamenAsync(p),
                p => p is ExamenLocal);

            LancerExamenCommand = new RelayCommand(
                p => LancerExamen(p),
                p => p is ExamenLocal e && string.Equals(e.Statut, "PUBLIE", System.StringComparison.OrdinalIgnoreCase));

            OuvrirQuizCommand = new RelayCommand(
                p =>
                {
                    if (p is not QuizLocal quiz) return;
                    NavigateToQuizDetails?.Invoke(quiz);
                },
                p => p is QuizLocal);

            OuvrirExamenCommand = new RelayCommand(
                p =>
                {
                    if (p is not ExamenLocal examen) return;
                    NavigateToExamenDetails?.Invoke(examen);
                },
                p => p is ExamenLocal);

            _ = ChargerDonneesAsync();
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

        private async Task SupprimerQuizAsync(object? parameter)
        {
            if (parameter is not QuizLocal quiz) return;

            var result = MessageBox.Show(
                $"Supprimer le quiz \"{quiz.Titre}\" ?",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            await _quizService.SupprimerAsync(quiz.Id);
            Quiz.Remove(quiz);
            TotalQuiz = Quiz.Count;
            OnPropertyChanged(nameof(HasNoQuiz));
            if (SelectedQuiz?.Id == quiz.Id) SelectedQuiz = null;
        }

        private async Task PublierQuizAsync(object? parameter)
        {
            if (parameter is not QuizLocal quiz) return;
            if (!EstQuizPublisable(quiz)) return;

            await _quizService.ChangerStatutAsync(quiz.Id, "Publié");
            await ChargerDonneesAsync();
            OnPropertyChanged(nameof(HasNoQuiz));
        }

        private async Task SupprimerExamenAsync(object? parameter)
        {
            if (parameter is not ExamenLocal examen) return;

            var result = MessageBox.Show(
                $"Supprimer l'examen \"{examen.Titre}\" ?",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            await _examenService.SupprimerAsync(examen.Id);
            Examens.Remove(examen);
            TotalExamens = Examens.Count;
            OnPropertyChanged(nameof(HasNoExamens));
            if (SelectedExamen?.Id == examen.Id) SelectedExamen = null;
        }

        private void LancerExamen(object? parameter)
        {
            if (parameter is not ExamenLocal examen) return;

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
