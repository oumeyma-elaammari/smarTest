using smartest_desktop.Services;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;

namespace smartest_desktop.Views
{
    public partial class GroqKeySetupWindow : Window
    {
        private static readonly SolidColorBrush Rouge  = new(Color.FromRgb(0xdc, 0x26, 0x26));
        private static readonly SolidColorBrush Vert   = new(Color.FromRgb(0x16, 0xa3, 0x4a));
        private static readonly SolidColorBrush Defaut = new(Color.FromRgb(0xe2, 0xe8, 0xf4));

        /// <param name="avertissement">Message affiché si la clé était révoquée (venu de la génération).</param>
        public GroqKeySetupWindow(string? avertissement = null)
        {
            InitializeComponent();
            string? existing = GroqKeyService.LireCle(App.LocalDb);
            if (!string.IsNullOrEmpty(existing))
            {
                TxtCle.Text = existing;
                // Clé révoquée : on la montre mais on force le message d'avertissement
                if (avertissement != null)
                    AfficherEtatManuel(false, avertissement);
                else
                    AfficherEtat(existing);
            }
            else if (avertissement != null)
            {
                AfficherEtatManuel(false, avertissement);
            }
        }

        // ── Validation en temps réel ──────────────────────────────────────────

        private void TxtCle_TextChanged(object sender, TextChangedEventArgs e)
            => AfficherEtat(TxtCle.Text.Trim());

        private void AfficherEtat(string cle)
        {
            if (string.IsNullOrWhiteSpace(cle))
            {
                PanneauEtat.Visibility = Visibility.Collapsed;
                TxtCle.BorderBrush = Defaut;
                return;
            }

            PanneauEtat.Visibility = Visibility.Visible;

            if (GroqKeyService.CleEstValide(cle))
            {
                TxtCle.BorderBrush   = Vert;
                IconeEtat.Text       = "✓";
                IconeEtat.Foreground = Vert;
                TxtErreur.Text       = "Clé valide";
                TxtErreur.Foreground = Vert;
            }
            else if (!cle.StartsWith("gsk_"))
            {
                TxtCle.BorderBrush   = Rouge;
                IconeEtat.Text       = "✗";
                IconeEtat.Foreground = Rouge;
                TxtErreur.Text       = "La clé doit commencer par gsk_";
                TxtErreur.Foreground = Rouge;
            }
            else
            {
                TxtCle.BorderBrush   = Rouge;
                IconeEtat.Text       = "✗";
                IconeEtat.Foreground = Rouge;
                TxtErreur.Text       = "Clé trop courte (minimum 20 caractères)";
                TxtErreur.Foreground = Rouge;
            }
        }

        private void AfficherEtatManuel(bool valide, string message)
        {
            PanneauEtat.Visibility = Visibility.Visible;
            TxtCle.BorderBrush   = valide ? Vert : Rouge;
            IconeEtat.Text       = valide ? "✓" : "✗";
            IconeEtat.Foreground = valide ? Vert : Rouge;
            TxtErreur.Text       = message;
            TxtErreur.Foreground = valide ? Vert : Rouge;
        }

        // ── Boutons ───────────────────────────────────────────────────────────

        private async void BtnTester_Click(object sender, RoutedEventArgs e)
        {
            string cle = TxtCle.Text.Trim();
            if (!GroqKeyService.CleEstValide(cle))
            {
                AfficherEtat(cle);
                return;
            }

            BtnTester.IsEnabled = false;
            BtnTester.Content = "Test en cours…";
            AfficherEtatManuel(true, "Connexion à Groq…");

            var (valide, message) = await GroqKeyService.TesterCleAsync(cle);

            AfficherEtatManuel(valide, message);
            BtnTester.Content = "Tester la clé";
            BtnTester.IsEnabled = true;
        }

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            string cle = TxtCle.Text.Trim();
            if (!GroqKeyService.CleEstValide(cle))
            {
                AfficherEtat(cle);
                TxtCle.Focus();
                return;
            }
            GroqKeyService.SauvegarderCle(App.LocalDb, cle);
            DialogResult = true;
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
            => DialogResult = false;

        private void LienGroq_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}

