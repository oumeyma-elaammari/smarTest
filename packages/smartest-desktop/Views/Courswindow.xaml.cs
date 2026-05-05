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
                    var dashboard = new DashboardWindow();
                    dashboard.Show();
                    Application.Current.MainWindow = dashboard;
                    this.Close();
                };

                vm.NavigateToStatistiquesRequested += () =>
                {
                    var stats = new StatistiquesProfWindow();
                    stats.Show();
                    Application.Current.MainWindow = stats;
                    this.Close();
                };
            }
        }
    }
}