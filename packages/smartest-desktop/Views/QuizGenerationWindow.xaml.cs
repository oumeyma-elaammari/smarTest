using smartest_desktop.ViewModels;
using System.Windows;

namespace smartest_desktop.Views
{
    public partial class QuizGenerationWindow : Window
    {
        public QuizGenerationWindow()
        {
            InitializeComponent();

            // ⚠️ IMPORTANT : s'assurer qu'on a bien le DataContext
            if (DataContext is not QuizGenerationViewModel vm)
            {
                vm = new QuizGenerationViewModel();
                DataContext = vm;
            }

            // ✅ ÉVÉNEMENT : quiz généré → ouvrir résultat
            vm.QuizGenereAvecSucces += (questions, titre, difficulte, nbQuestions, coursTitre) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var resultWindow = new QuizResultWindow(
                        questions,
                        titre,
                        difficulte,
                        coursTitre,
                        "Validé"
                    );

                    resultWindow.Show();

                    // ✔️ éviter fermeture bug
                    if (this.IsVisible)
                        this.Close();
                });
            };

            // ✅ ÉVÉNEMENT : annulation
            vm.NavigationAnnulee += () =>
            {
                var hub = new QuizExamenWindow();
                hub.Show();
                this.Close();
            };
        }

        // ❗ DEBUG OPTIONNEL (PAS dans le constructeur)
        private void DebugMessage()
        {
            System.Diagnostics.Debug.WriteLine("🚨 Fenêtre QuizGeneration ouverte !");
        }
    }
}