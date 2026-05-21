using smartest_desktop.ViewModels;
using System.Windows;

namespace smartest_desktop.Views
{
    public partial class ExamenCorrectionEtudiantDialog : Window
    {
        private readonly ExamenCorrectionEtudiantViewModel _vm;

        public ExamenCorrectionEtudiantDialog(long examenBackendId, string titreExamen, long etudiantId, string bearerToken)
        {
            InitializeComponent();
            _vm = new ExamenCorrectionEtudiantViewModel(examenBackendId, titreExamen, etudiantId, bearerToken);
            _vm.DemandeFermetureSucces += () =>
            {
                DialogResult = true;
                Close();
            };
            DataContext = _vm;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await _vm.ChargerAsync();
        }
    }
}
