using smartest_desktop.ViewModels;
using System.Windows;

namespace smartest_desktop.Views
{
    public partial class QuizGenerationWindow : Window
    {
        public QuizGenerationWindow()
        {
            InitializeComponent();

            // DataContext instancié dans le XAML — on le récupère directement
            var vm = (QuizGenerationViewModel)DataContext;

            // Quiz généré → ouvrir QuizResultWindow
            vm.QuizGenereAvecSucces += (questions, titre, difficulte, nbQuestions, coursTitre) =>
            {
                Dispatcher.Invoke(() =>
                {
                    var resultWindow = new QuizResultWindow(
                        questions,
                        titre,
                        difficulte,
                        coursTitre ?? string.Empty,
                        "Validé"
                    );
                    resultWindow.Show();
                    if (IsVisible) Close();
                });
            };

            // Annulation → retour au hub Quiz/Examen
            vm.NavigationAnnulee += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    var hub = new QuizExamenWindow();
                    hub.Show();
                    Close();
                });
            };
        }
    }
}