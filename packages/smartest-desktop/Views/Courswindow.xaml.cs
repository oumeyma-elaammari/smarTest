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
                vm.NavigateToDashboardRequested += OnNavigateToDashboardRequested;
            }
        }

        private void OnNavigateToDashboardRequested()
        {
            var dashboard = new DashboardWindow();
            dashboard.Show();
            this.Close();
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            var dashboard = new DashboardWindow();
            dashboard.Show();
            this.Close();
        }
    }
}