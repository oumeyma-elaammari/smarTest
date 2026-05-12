using smartest_desktop.ViewModels;
using System.Windows;

namespace smartest_desktop.Views
{
    public partial class StatistiquesProfWindow : Window
    {
        private readonly StatistiquesQuizProfViewModel _statsDashboardVm;

        public StatistiquesProfWindow(long? preferBackendQuizId = null, long? preferBackendExamenId = null)
        {
            InitializeComponent();

            _statsDashboardVm = new StatistiquesQuizProfViewModel(preferBackendQuizId, preferBackendExamenId);
            Dashboard.DataContext = _statsDashboardVm;
            Closed += (_, _) => _statsDashboardVm.StopPeriodicRefresh();

            var shell = new StatistiquesProfViewModel();
            DataContext = shell;

            shell.NavigateToDashboard += () =>
            {
                App.OuvrirShell(MainShellSection.Home);
                Close();
            };

            shell.NavigateToQuizExamen += () =>
            {
                App.OuvrirShell(MainShellSection.QuizExamens);
                Close();
            };
        }
    }
}
