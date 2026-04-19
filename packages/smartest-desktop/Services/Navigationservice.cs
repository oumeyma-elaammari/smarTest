using System.Linq;
using System.Windows;
using WpfApp = System.Windows.Application;

namespace smartest_desktop.Services
{
    /// <summary>
    /// Service de navigation centralisé.
    /// Remplace les boucles foreach répétées dans chaque ViewModel.
    /// </summary>
    public static class NavigationService
    {
        /// <summary>
        /// Ferme toutes les fenêtres du type TClose,
        /// puis ouvre une nouvelle fenêtre du type TOpen.
        /// </summary>
        public static void NavigateTo<TOpen, TClose>()
            where TOpen : Window, new()
            where TClose : Window
        {
            // Récupérer l'ancienne fenêtre AVANT d'en créer une nouvelle
            var oldWindow = WpfApp.Current.Windows
                .OfType<TClose>()
                .FirstOrDefault();

            var newWindow = new TOpen();
            newWindow.Show();
            oldWindow?.Close();
        }

        /// <summary>
        /// Ferme toutes les fenêtres du type TClose,
        /// puis ouvre une fenêtre existante déjà construite (avec paramètres).
        /// </summary>
        public static void NavigateTo<TClose>(Window newWindow)
            where TClose : Window
        {
            var oldWindow = WpfApp.Current.Windows
                .OfType<TClose>()
                .FirstOrDefault();

            newWindow.Show();
            oldWindow?.Close();
        }

        /// <summary>
        /// Ferme toutes les fenêtres du type TClose,
        /// puis ouvre une fenêtre existante en mode dialog.
        /// </summary>
        public static void NavigateToDialog<TClose>(Window newWindow)
            where TClose : Window
        {
            var oldWindow = WpfApp.Current.Windows
                .OfType<TClose>()
                .FirstOrDefault();

            oldWindow?.Close();
            newWindow.ShowDialog();
        }

        /// <summary>
        /// Ferme simplement toutes les fenêtres d'un type donné.
        /// </summary>
        public static void Close<TWindow>()
            where TWindow : Window
        {
            var window = WpfApp.Current.Windows
                .OfType<TWindow>()
                .FirstOrDefault();

            window?.Close();
        }
    }
}