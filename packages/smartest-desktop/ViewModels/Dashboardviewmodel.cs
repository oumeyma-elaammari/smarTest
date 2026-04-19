using smartest_desktop.Helpers;
using smartest_desktop.Services;
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
            Nom = WpfApp.Current.Properties["Nom"]?.ToString() ?? "Professeur";
            Email = WpfApp.Current.Properties["Email"]?.ToString() ?? "";

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

            WpfApp.Current.Properties["Token"] = null;
            WpfApp.Current.Properties["Nom"] = null;
            WpfApp.Current.Properties["Email"] = null;

            NavigationService.NavigateTo<Views.LoginWindow, Views.DashboardWindow>();
        }
    }
}

