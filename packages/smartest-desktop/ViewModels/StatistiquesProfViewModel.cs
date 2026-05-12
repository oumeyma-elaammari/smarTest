using System;
using System.Windows;
using System.Windows.Input;
using smartest_desktop.Helpers;
using WpfApp = System.Windows.Application;

namespace smartest_desktop.ViewModels
{
    /// <summary>Barre latérale de la fenêtre Statistiques (navigation prof).</summary>
    public sealed class StatistiquesProfViewModel : BaseViewModel
    {
        public string Nom { get; }
        public string Email { get; }

        public ICommand RetourDashboardCommand { get; }
        public ICommand OuvrirQuizExamenCommand { get; }
        public ICommand LogoutCommand { get; }

        public event Action? NavigateToDashboard;
        public event Action? NavigateToQuizExamen;

        public StatistiquesProfViewModel()
        {
            Nom = WpfApp.Current.Properties["Nom"]?.ToString() ?? "Professeur";
            Email = WpfApp.Current.Properties["Email"]?.ToString() ?? "";

            RetourDashboardCommand = new RelayCommand(_ => NavigateToDashboard?.Invoke());
            OuvrirQuizExamenCommand = new RelayCommand(_ => NavigateToQuizExamen?.Invoke());
            LogoutCommand = new RelayCommand(_ => ExecuteLogout());
        }

        private static void ExecuteLogout()
        {
            var result = MessageBox.Show(
                "Voulez-vous vraiment vous déconnecter ?",
                "Déconnexion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            smartest_desktop.App.Deconnecter();
        }
    }
}
