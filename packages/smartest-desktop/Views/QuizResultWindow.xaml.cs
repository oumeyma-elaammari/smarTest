using smartest_desktop.ViewModels;
using System.Windows;

namespace smartest_desktop.Views
{
    public partial class QuizResultWindow : Window
    {
        public QuizResultWindow(List<QuestionQCM> questions, string titre, string difficulte, string coursTitre, string statut)
        {
            InitializeComponent();

            var vm = new QuizResultViewModel(questions, titre, difficulte, coursTitre, statut);
            DataContext = vm;

            vm.NavigationRetourRequested += () =>
            {
                var hub = new QuizExamenWindow();
                hub.Show();
                this.Close();
            };

            vm.NavigationRegenerarRequested += () =>
            {
                var quizGen = new QuizGenerationWindow();
                quizGen.Show();
                this.Close();
            };

            vm.QuizValide += (questionsValidees, titreQuiz, difficulteQuiz, coursTitreQuiz, statutQuiz) =>
            {
                MessageBox.Show(
                    $"✅ Le quiz \"{titreQuiz}\" a été validé avec succès !\n\n" +
                    $"{questionsValidees.Count} questions sauvegardées.\n" +
                    $"• Difficulté : {difficulteQuiz}\n" +
                    $"• Cours : {coursTitreQuiz}\n" +
                    $"• Statut : {statutQuiz}\n\n" +
                    $"Vous pouvez maintenant le publier pour vos étudiants.",
                    "Quiz validé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                var dashboard = new DashboardWindow();
                dashboard.Show();
                this.Close();
            };
        }
    }
}