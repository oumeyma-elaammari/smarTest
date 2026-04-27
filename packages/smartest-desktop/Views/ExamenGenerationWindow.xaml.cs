using smartest_desktop.ViewModels;
using System.Windows;

namespace smartest_desktop.Views
{
    public partial class ExamenGenerationWindow : Window
    {
        public ExamenGenerationWindow()
        {
            InitializeComponent();

            var vm = new ExamenGenerationViewModel();
            DataContext = vm;

            vm.ExamenGenereAvecSucces += (questions, titre, duree, difficulte, cours) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var resultWindow = new ExamenResultWindow(
                        questions, titre, duree, difficulte, cours);
                    resultWindow.Show();

                    if (this.IsVisible)
                        this.Close();
                });
            };

            vm.NavigationAnnulee += () =>
            {
                var hub = new QuizExamenWindow();
                hub.Show();
                this.Close();
            };
        }
    }
}
