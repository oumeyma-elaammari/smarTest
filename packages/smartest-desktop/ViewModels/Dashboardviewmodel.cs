using smartest_desktop.Helpers;
using System.Windows;
using System.Windows.Input;
using WpfApp = System.Windows.Application;

namespace smartest_desktop.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private string _nom;
        private string _email;

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

        public ICommand LogoutCommand { get; }

        public DashboardViewModel()
        {
            // ✅ Récupérer les infos depuis Application.Properties
            Nom = WpfApp.Current.Properties["Nom"]?.ToString() ?? "Professeur";
            Email = WpfApp.Current.Properties["Email"]?.ToString() ?? "";

            LogoutCommand = new RelayCommand(_ => ExecuteLogout());
        }

        private void ExecuteLogout()
        {
            // ✅ Confirmation avant déconnexion
            var result = MessageBox.Show(
                "Voulez-vous vraiment vous déconnecter ?",
                "Déconnexion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes) return;

            // ✅ Nettoyer les données de session
            WpfApp.Current.Properties["Token"] = null;
            WpfApp.Current.Properties["Nom"] = null;
            WpfApp.Current.Properties["Email"] = null;

            // ✅ Ouvrir LoginWindow
            var login = new Views.LoginWindow();
            login.Show();

            // ✅ Fermer DashboardWindow
            foreach (System.Windows.Window w in WpfApp.Current.Windows)
            {
                if (w is Views.DashboardWindow)
                {
                    w.Close();
                    break;
                }
            }
        }
    }
}