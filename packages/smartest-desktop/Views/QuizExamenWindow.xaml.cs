using smartest_desktop.ViewModels;
using System.Windows;

namespace smartest_desktop.Views
{
    public partial class QuizExamenWindow : Window
    {
        public QuizExamenWindow()
        {
            InitializeComponent();

            if (DataContext is QuizExamenViewModel vm)
            {
                // Clic sur "Générer un Quiz" → ouvrir QuizGenerationWindow
                vm.NavigateToQuizGeneration += () =>
                {
                    var quizGen = new QuizGenerationWindow();
                    quizGen.Show();

                    this.Hide(); // ✅ au lieu de Close()
                };

                // Retour Dashboard
                vm.NavigateToDashboard += () =>
                {
                    var dashboard = new DashboardWindow();
                    dashboard.Show();
                    this.Close();
                };
            }
        }
    }
}