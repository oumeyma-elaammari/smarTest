using smartest_desktop.Data;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Helpers;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfApp = System.Windows.Application;


namespace smartest_desktop.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly LocalDbContext _db;

        private string _nom = string.Empty;
        private string _email = string.Empty;

        public string Nom
        {
            get => _nom;
            set => SetProperty(ref _nom, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public ObservableCollection<CoursLocal> DerniersCours { get; set; } = new();

        private int _totalCours;
        public int TotalCours
        {
            get => _totalCours;
            set => SetProperty(ref _totalCours, value);
        }

        private int _totalCategories;
        public int TotalCategories
        {
            get => _totalCategories;
            set => SetProperty(ref _totalCategories, value);
        }

        private int _totalExamens;
        public int TotalExamens
        {
            get => _totalExamens;
            set => SetProperty(ref _totalExamens, value);
        }

        public ICommand LogoutCommand { get; }
        public ICommand OpenCoursCommand { get; }
        public ICommand OpenQuizCommand { get; }

        public DashboardViewModel()
        {
            _db = App.LocalDb;

            Nom = WpfApp.Current.Properties["Nom"]?.ToString() ?? "Professeur";
            Email = WpfApp.Current.Properties["Email"]?.ToString() ?? "";

            LogoutCommand = new RelayCommand(_ => ExecuteLogout());
            OpenCoursCommand = new RelayCommand(_ => ExecuteOpenCours());


            // ✅ AJOUT ICI
            OpenQuizCommand = new RelayCommand(_ => ExecuteOpenQuiz());

            _ = ChargerDonnees();
        }

        private void ExecuteOpenCours()
        {
            try
            {
                var coursWindow = new Views.CoursWindow();
                coursWindow.Show();
                Application.Current.MainWindow = coursWindow;

                // Fermer DashboardWindow
                foreach (Window w in WpfApp.Current.Windows)
                {
                    if (w is Views.DashboardWindow)
                    {
                        w.Close();
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ChargerDonnees()
        {
            try
            {
                var liste = await Task.Run(() =>
                    _db.Cours.OrderByDescending(c => c.DateImport).Take(5).ToList());

                DerniersCours.Clear();
                foreach (var c in liste)
                    DerniersCours.Add(c);

                TotalCours = await Task.Run(() => _db.Cours.Count());

                // Calculer le nombre de catégories uniques à partir des cours
                var categories = await Task.Run(() => _db.Cours
                    .Where(c => c.Categorie != null && c.Categorie != string.Empty)
                    .Select(c => c.Categorie)
                    .Distinct()
                    .ToList());
                TotalCategories = categories.Count;

                // Total examens (à adapter selon votre logique)
                TotalExamens = 0;
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur chargement dashboard: {ex.Message}");
            }
        }
        public bool HasNoCours => TotalCours == 0;


        private void ExecuteLogout()
        {
            var result = MessageBox.Show(
                "Voulez-vous vraiment vous déconnecter ?",
                "Déconnexion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            WpfApp.Current.Properties["Token"] = null;
            WpfApp.Current.Properties["Nom"] = null;
            WpfApp.Current.Properties["Email"] = null;

            var login = new Views.LoginWindow();
            login.Show();

            foreach (Window w in WpfApp.Current.Windows)
            {
                if (w is Views.DashboardWindow) { w.Close(); break; }
            }
        }


        private void ExecuteOpenQuiz()
        {
            try
            {
                var hub = new Views.QuizExamenWindow();
                hub.Show();
                Application.Current.MainWindow = hub;

                foreach (Window w in WpfApp.Current.Windows)
                {
                    if (w is Views.DashboardWindow)
                    {
                        w.Close();
                        break;
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}