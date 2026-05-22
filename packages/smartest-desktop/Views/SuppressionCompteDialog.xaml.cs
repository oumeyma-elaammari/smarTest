using System.Windows;

namespace smartest_desktop.Views
{
    public partial class SuppressionCompteDialog : Window
    {
        public bool Confirme { get; private set; }

        public SuppressionCompteDialog()
        {
            InitializeComponent();
        }

        public static bool DemanderConfirmation(Window? owner)
        {
            var dlg = new SuppressionCompteDialog
            {
                Owner = owner ?? Application.Current?.MainWindow
            };
            dlg.ShowDialog();
            return dlg.Confirme;
        }

        private void BtnConfirmer_Click(object sender, RoutedEventArgs e)
        {
            Confirme = true;
            DialogResult = true;
            Close();
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            Confirme = false;
            DialogResult = false;
            Close();
        }
    }
}
