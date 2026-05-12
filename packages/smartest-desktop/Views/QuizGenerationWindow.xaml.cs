using smartest_desktop.ViewModels;
using System.Threading;
using System.Windows;

namespace smartest_desktop.Views
{
    public partial class QuizGenerationWindow : Window
    {
        private int _resultQuizOuverte;

        public QuizGenerationWindow()
        {
            InitializeComponent();

            // DataContext instancié dans le XAML — on le récupère directement
            var vm = (QuizGenerationViewModel)DataContext;

            // Quiz généré → ouvrir QuizResultWindow (évite double ouverture si l'événement est invoqué 2 fois)
            vm.QuizGenereAvecSucces += (questions, titre, difficulte, nbQuestions, coursTitre, emailsPublicationWebJson) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (Interlocked.CompareExchange(ref _resultQuizOuverte, 1, 0) != 0)
                        return;

                    var resultWindow = new QuizResultWindow(
                        questions,
                        titre,
                        difficulte,
                        coursTitre ?? string.Empty,
                        "Validé",
                        quizIdExistant: null,
                        emailsPublicationWebJson: emailsPublicationWebJson);
                    resultWindow.Show();
                    App.GarderUneSeuleFenetreOuverte(resultWindow);
                    if (IsVisible)
                        Close();
                });
            };

            // Annulation → retour au hub Quiz/Examen
            vm.NavigationAnnulee += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    App.OuvrirShell(MainShellSection.QuizExamens);
                    Close();
                });
            };

            vm.NavigateToDashboard += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    App.OuvrirShell(MainShellSection.Home);
                    Close();
                });
            };

            vm.NavigateToLogin += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    var login = new LoginWindow();
                    login.Show();
                    Close();
                });
            };
        }
    }
}