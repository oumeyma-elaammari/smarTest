using smartest_desktop.ViewModels;
using System.Threading;
using System.Windows;

namespace smartest_desktop.Views
{
    public partial class ExamenGenerationWindow : Window
    {
        private int _resultExamenOuverte;

        public ExamenGenerationWindow()
        {
            InitializeComponent();

            var vm = new ExamenGenerationViewModel();
            DataContext = vm;

            vm.ExamenGenereAvecSucces += (questions, titre, duree, difficulte, coursTitre) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Interlocked.CompareExchange(ref _resultExamenOuverte, 1, 0) != 0)
                        return;

                    var resultWindow = new ExamenResultWindow(
                        questions, titre, duree, difficulte, coursTitre);
                    resultWindow.Show();
                    App.GarderUneSeuleFenetreOuverte(resultWindow);

                    if (IsVisible)
                        Close();
                });
            };

            vm.NavigationAnnulee += () =>
            {
                App.OuvrirShell(MainShellSection.QuizExamens);
                this.Close();
            };

            vm.NavigateToDashboard += () =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    App.OuvrirShell(MainShellSection.Home);
                    Close();
                });
            };

            vm.NavigateToLogin += () =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var login = new LoginWindow();
                    login.Show();
                    Close();
                });
            };
        }
    }
}
