using System.Windows;

namespace smartest_desktop.Views
{
    public enum SuppressionExamenMode
    {
        Annule,
        BureauUniquement,
        BureauEtWeb
    }

    public partial class SuppressionExamenChoixDialog : Window
    {
        public SuppressionExamenMode Mode { get; private set; } = SuppressionExamenMode.Annule;

        public SuppressionExamenChoixDialog(string titreExamen, bool peutSupprimerWeb, string? raisonWebIndisponible)
        {
            InitializeComponent();
            TxtTitre.Text = $"Supprimer « {titreExamen} » ?";
            TxtBureauSeul.Text =
                "Retire l'examen de Quiz et examens et de Sessions examens sur ce poste. "
                + "Les étudiants peuvent encore y accéder sur le web si l'examen y est publié.";
            TxtBureauEtWeb.Text =
                "Supprime l'examen sur ce poste et sur la plateforme web : les étudiants n'y auront plus accès.";

            if (!peutSupprimerWeb)
            {
                BtnBureauEtWeb.IsEnabled = false;
                BtnBureauEtWeb.Opacity = 0.55;
                if (!string.IsNullOrWhiteSpace(raisonWebIndisponible))
                {
                    TxtAvertissementWeb.Text = raisonWebIndisponible;
                    TxtAvertissementWeb.Visibility = Visibility.Visible;
                }
            }
        }

        public static SuppressionExamenMode Afficher(
            Window? owner,
            string titreExamen,
            bool peutSupprimerWeb,
            string? raisonWebIndisponible = null)
        {
            var dlg = new SuppressionExamenChoixDialog(titreExamen, peutSupprimerWeb, raisonWebIndisponible)
            {
                Owner = owner ?? Application.Current?.MainWindow
            };
            dlg.ShowDialog();
            return dlg.Mode;
        }

        private void BtnBureauSeul_Click(object sender, RoutedEventArgs e)
        {
            Mode = SuppressionExamenMode.BureauUniquement;
            DialogResult = true;
            Close();
        }

        private void BtnBureauEtWeb_Click(object sender, RoutedEventArgs e)
        {
            Mode = SuppressionExamenMode.BureauEtWeb;
            DialogResult = true;
            Close();
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            Mode = SuppressionExamenMode.Annule;
            DialogResult = false;
            Close();
        }
    }
}
