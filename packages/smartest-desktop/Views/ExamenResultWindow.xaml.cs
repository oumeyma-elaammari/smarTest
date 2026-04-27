using smartest_desktop.ViewModels;
using System.Collections.Generic;
using System.Windows;

namespace smartest_desktop.Views
{
    public partial class ExamenResultWindow : Window
    {
        public ExamenResultWindow(
            List<QuestionExamen> questions,
            string titre,
            int duree,
            string difficulte,
            string coursTitre)
        {
            InitializeComponent();

            var vm = new ExamenResultViewModel(questions, titre, duree, difficulte, coursTitre);
            DataContext = vm;

            vm.NavigationRetourRequested += () =>
            {
                var hub = new QuizExamenWindow();
                hub.Show();
                this.Close();
            };

            vm.NavigationRegenerarRequested += () =>
            {
                var examenGen = new ExamenGenerationWindow();
                examenGen.Show();
                this.Close();
            };

            vm.ExamenValide += (_, titreExamen, _, _, coursTitre) =>
            {
                MessageBox.Show(
                    $"✅ L'examen \"{titreExamen}\" a été sauvegardé avec succès !",
                    "Examen sauvegardé",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                var dashboard = new DashboardWindow();
                dashboard.Show();
                this.Close();
            };
        }
    }
}
