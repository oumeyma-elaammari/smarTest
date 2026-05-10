using System.Windows;
using smartest_desktop.ViewModels;

namespace smartest_desktop.Views
{
    public partial class CoursWindow : Window
    {
        public CoursWindow()
        {
            InitializeComponent();

            if (DataContext is CoursViewModel vm)
            {
                vm.NavigateToDashboardRequested += () =>
                {
                    App.OuvrirShell(MainShellSection.Home);
                    this.Close();
                };

                vm.NavigateToStatistiquesRequested += () =>
                {
                    App.OuvrirShell(MainShellSection.Statistiques);
                    this.Close();
                };
            }
        }
    }
}