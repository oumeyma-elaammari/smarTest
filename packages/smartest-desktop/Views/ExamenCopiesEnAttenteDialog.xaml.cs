using smartest_desktop.Exceptions;
using Newtonsoft.Json.Linq;
using smartest_desktop.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace smartest_desktop.Views
{
    public partial class ExamenCopiesEnAttenteDialog : Window
    {
        private sealed class EtudiantLigne
        {
            public long EtudiantId { get; init; }
            public double? NoteProposee { get; init; }
            public string Libelle =>
                NoteProposee.HasValue
                    ? $"Étudiant #{EtudiantId} — note proposée {NoteProposee.Value.ToString("0.##", CultureInfo.InvariantCulture)}/20"
                    : $"Étudiant #{EtudiantId}";
        }

        private readonly long _examenBackendId;
        private readonly string _titreExamen;
        private readonly string _token;
        private readonly ExamenWebPublicationApiService _api = new();

        public ExamenCopiesEnAttenteDialog(long examenBackendId, string titreExamen, string bearerToken)
        {
            InitializeComponent();
            _examenBackendId = examenBackendId;
            _titreExamen = titreExamen?.Trim() ?? $"Examen #{examenBackendId}";
            _token = bearerToken;
            TxtTitre.Text = _titreExamen;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await ChargerListeAsync();
        }

        private async Task ChargerListeAsync()
        {
            TxtChargement.Visibility = Visibility.Visible;
            TxtVide.Visibility = Visibility.Collapsed;
            ListeEtudiants.Visibility = Visibility.Collapsed;
            TxtErreur.Visibility = Visibility.Collapsed;
            ListeEtudiants.ItemsSource = null;

            try
            {
                JArray attente = await _api.GetResultatsEnAttenteAsync(_token, _examenBackendId);
                var lignes = new List<EtudiantLigne>();
                if (attente != null)
                {
                    foreach (var tok in attente)
                    {
                        if (tok is not JObject row) continue;
                        long? eid = row["etudiantId"]?.Type == JTokenType.Integer
                            ? row["etudiantId"]!.Value<long>()
                            : null;
                        if (eid == null || eid <= 0) continue;
                        var npTok = row["noteProposee"];
                        double? np = npTok != null && (npTok.Type == JTokenType.Float || npTok.Type == JTokenType.Integer)
                            ? npTok.Value<double>()
                            : null;
                        lignes.Add(new EtudiantLigne { EtudiantId = eid.Value, NoteProposee = np });
                    }
                }

                TxtChargement.Visibility = Visibility.Collapsed;
                if (lignes.Count == 0)
                {
                    TxtVide.Text =
                        "Aucune copie en attente.\n\n" +
                        "Vérifiez que la session est terminée et que les étudiants ont bien soumis leur examen .";
                    TxtVide.Visibility = Visibility.Visible;
                    return;
                }

                ListeEtudiants.ItemsSource = lignes;
                ListeEtudiants.Visibility = Visibility.Visible;
                ListeEtudiants.SelectedIndex = 0;
            }
            catch (SmartestApiException ex)
            {
                TxtChargement.Visibility = Visibility.Collapsed;
                TxtErreur.Text = ex.Message;
                TxtErreur.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                TxtChargement.Visibility = Visibility.Collapsed;
                TxtErreur.Text = ex.Message;
                TxtErreur.Visibility = Visibility.Visible;
            }
        }

        private void ListeEtudiants_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OuvrirCorrectionSelectionnee();
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void BtnOuvrirCorrection_Click(object sender, RoutedEventArgs e)
        {
            OuvrirCorrectionSelectionnee();
        }

        private void OuvrirCorrectionSelectionnee()
        {
            if (ListeEtudiants.SelectedItem is not EtudiantLigne etu)
            {
                MessageBox.Show(
                    "Sélectionnez un étudiant dans la liste.",
                    "Correction",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var dlg = new ExamenCorrectionEtudiantDialog(_examenBackendId, _titreExamen, etu.EtudiantId, _token);
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                _ = ChargerListeAsync();
            }
        }
    }
}
