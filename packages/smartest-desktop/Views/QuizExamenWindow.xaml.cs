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
                vm.NavigateToQuizGeneration += () =>
                {
                    var quizGen = new QuizGenerationWindow();
                    quizGen.Show();
                    this.Hide();
                };

                vm.NavigateToExamenGeneration += () =>
                {
                    var examenGen = new ExamenGenerationWindow();
                    examenGen.Show();
                    this.Close();
                };

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