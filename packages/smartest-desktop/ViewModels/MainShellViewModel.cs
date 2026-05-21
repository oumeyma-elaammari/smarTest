using smartest_desktop.Helpers;
using System.Windows;
using System.Windows.Input;
using WpfApp = System.Windows.Application;

namespace smartest_desktop.ViewModels
{
    public enum MainShellSection
    {
        Home,
        QuizExamens,
        QuizGeneration,
        ExamenGeneration,
        Statistiques,
        Sessions,
        Parametres
    }

    public sealed class MainShellViewModel : BaseViewModel
    {
        private MainShellSection _sectionActive = MainShellSection.Home;
        public MainShellSection SectionActive
        {
            get => _sectionActive;
            set
            {
                if (!SetProperty(ref _sectionActive, value))
                    return;
                OnPropertyChanged(nameof(IsHome));
                OnPropertyChanged(nameof(IsQuizExamens));
                OnPropertyChanged(nameof(IsQuizGeneration));
                OnPropertyChanged(nameof(IsExamenGeneration));
                OnPropertyChanged(nameof(IsCentralHosted));
                OnPropertyChanged(nameof(ShowShellChrome));
                OnPropertyChanged(nameof(IsStatistiques));
                OnPropertyChanged(nameof(IsSessions));
                OnPropertyChanged(nameof(IsParametres));
                if (IsParametres)
                {
                    SettingsAccount.ResetDraft();
                }
            }
        }

        public bool IsHome => SectionActive == MainShellSection.Home;
        public bool IsQuizExamens => SectionActive == MainShellSection.QuizExamens;
        public bool IsQuizGeneration => SectionActive == MainShellSection.QuizGeneration;
        public bool IsExamenGeneration => SectionActive == MainShellSection.ExamenGeneration;
        public bool IsCentralHosted =>
            SectionActive == MainShellSection.Home ||
            SectionActive == MainShellSection.QuizExamens ||
            SectionActive == MainShellSection.QuizGeneration ||
            SectionActive == MainShellSection.ExamenGeneration ||
            SectionActive == MainShellSection.Statistiques;
        public bool ShowShellChrome =>
            SectionActive != MainShellSection.QuizGeneration &&
            SectionActive != MainShellSection.ExamenGeneration;
        public bool IsStatistiques => SectionActive == MainShellSection.Statistiques;
        public bool IsSessions => SectionActive == MainShellSection.Sessions;
        public bool IsParametres => SectionActive == MainShellSection.Parametres;

        public SettingsAccountViewModel SettingsAccount { get; }
        public SessionsActivesViewModel SessionsActives { get; }

        public ICommand GoHomeCommand { get; }
        public ICommand GoQuizExamensCommand { get; }
        public ICommand GoStatistiquesCommand { get; }
        public ICommand GoSessionsCommand { get; }
        public ICommand GoParametresCommand { get; }
        public ICommand GoQuizGenerationCommand { get; }
        public ICommand GoExamenGenerationCommand { get; }
        public ICommand OuvrirQuizExamensLegacyCommand { get; }
        public ICommand OuvrirStatistiquesLegacyCommand { get; }
        public ICommand OuvrirCoursLegacyCommand { get; }
        public ICommand OuvrirQuizGenerationCommand { get; }
        public ICommand OuvrirExamenGenerationCommand { get; }
        public ICommand LogoutCommand { get; }

        public MainShellViewModel()
        {
            SettingsAccount = new SettingsAccountViewModel();
            SessionsActives = new SessionsActivesViewModel();
            GoHomeCommand = new RelayCommand(_ => SectionActive = MainShellSection.Home);
            GoQuizExamensCommand = new RelayCommand(_ => SectionActive = MainShellSection.QuizExamens);
            GoStatistiquesCommand = new RelayCommand(_ => SectionActive = MainShellSection.Statistiques);
            GoSessionsCommand = new RelayCommand(_ => SectionActive = MainShellSection.Sessions);
            GoParametresCommand = new RelayCommand(_ => SectionActive = MainShellSection.Parametres);
            GoQuizGenerationCommand = new RelayCommand(_ => SectionActive = MainShellSection.QuizGeneration);
            GoExamenGenerationCommand = new RelayCommand(_ => SectionActive = MainShellSection.ExamenGeneration);

            OuvrirQuizExamensLegacyCommand = new RelayCommand(_ =>
            {
                SectionActive = MainShellSection.QuizExamens;
            });

            OuvrirStatistiquesLegacyCommand = new RelayCommand(_ =>
            {
                SectionActive = MainShellSection.Statistiques;
            });

            OuvrirCoursLegacyCommand = new RelayCommand(_ =>
            {
                var w = new Views.CoursWindow();
                w.Show();
                Application.Current.MainWindow = w;
                FermerShellCourant();
            });

            OuvrirQuizGenerationCommand = new RelayCommand(_ =>
            {
                SectionActive = MainShellSection.QuizGeneration;
            });

            OuvrirExamenGenerationCommand = new RelayCommand(_ =>
            {
                SectionActive = MainShellSection.ExamenGeneration;
            });

            LogoutCommand = new RelayCommand(_ => ExecuteLogout());
        }

        private static void FermerShellCourant()
        {
            foreach (Window w in WpfApp.Current.Windows)
            {
                if (w is Views.MainShellWindow)
                {
                    w.Close();
                    break;
                }
            }
        }

        private static void ExecuteLogout()
        {
            var result = MessageBox.Show(
                "Voulez-vous vraiment vous déconnecter ?",
                "Déconnexion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;
            App.Deconnecter();
        }

    }
}
