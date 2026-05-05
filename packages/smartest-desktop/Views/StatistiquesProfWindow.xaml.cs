using smartest_desktop.ViewModels;
using System.Windows;

namespace smartest_desktop.Views
{
    public partial class StatistiquesProfWindow : Window
    {
        public StatistiquesProfWindow(long? preferBackendQuizId = null, long? preferBackendExamenId = null)
        {
            InitializeComponent();

            Dashboard.DataContext = new StatistiquesQuizProfViewModel(preferBackendQuizId, preferBackendExamenId);

            var shell = new StatistiquesProfViewModel();
            DataContext = shell;

            shell.NavigateToDashboard += () =>
            {
                var w = new DashboardWindow();
                w.Show();
                Application.Current.MainWindow = w;
                Close();
            };

            shell.NavigateToQuizExamen += () =>
            {
                var w = new QuizExamenWindow();
                w.Show();
                Application.Current.MainWindow = w;
                Close();
            };
        }
    }
}
