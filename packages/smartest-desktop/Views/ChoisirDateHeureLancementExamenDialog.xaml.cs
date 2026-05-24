using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace smartest_desktop.Views
{
    public partial class ChoisirDateHeureLancementExamenDialog : Window
    {
        public DateTime SelectedDateTime { get; private set; }

        public ChoisirDateHeureLancementExamenDialog(DateTime suggested)
        {
            InitializeComponent();
            var local = suggested.Kind == DateTimeKind.Utc ? suggested.ToLocalTime() : suggested;

            DpDate.DisplayDateStart = DateTime.Today;
            DpDate.SelectedDate = local.Date;
            var hours = Enumerable.Range(0, 24).Select(i => i.ToString("D2", CultureInfo.InvariantCulture)).ToArray();
            var minutes = Enumerable.Range(0, 60).Select(i => i.ToString("D2", CultureInfo.InvariantCulture)).ToArray();
            CbHour.ItemsSource = hours;
            CbMinute.ItemsSource = minutes;
            CbHour.SelectedItem = local.Hour.ToString("D2", CultureInfo.InvariantCulture);
            CbMinute.SelectedItem = local.Minute.ToString("D2", CultureInfo.InvariantCulture);
            MettreAJourEtatValidation();
        }

        private void Creneau_Changed(object sender, SelectionChangedEventArgs e) =>
            MettreAJourEtatValidation();

        private void Creneau_Changed(object sender, RoutedEventArgs e) =>
            MettreAJourEtatValidation();

        private void MettreAJourEtatValidation()
        {
            var message = EvaluerMessageErreurCreneau();
            var valide = string.IsNullOrEmpty(message);

            TbErreurCreneau.Text = message;
            TbErreurCreneau.Visibility = valide ? Visibility.Collapsed : Visibility.Visible;
            BtnValider.IsEnabled = valide;
        }

        private string? EvaluerMessageErreurCreneau()
        {
            if (DpDate.SelectedDate is not DateTime datePart)
                return "Choisissez une date pour le lancement.";

            if (datePart.Date < DateTime.Today)
                return "La date sélectionnée est déjà passée. Choisissez une date valide dans le futur.";

            if (CbHour.SelectedItem is not string hs || CbMinute.SelectedItem is not string ms ||
                !int.TryParse(hs, NumberStyles.Integer, CultureInfo.InvariantCulture, out var h) ||
                !int.TryParse(ms, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mi))
                return "Heure invalide.";

            try
            {
                var passage = new DateTime(datePart.Year, datePart.Month, datePart.Day, h, mi, 0, DateTimeKind.Local);
                if (passage <= DateTime.Now)
                    return "La date et l'heure du créneau doivent être dans le futur. Choisissez une date et une heure valides.";
            }
            catch (ArgumentOutOfRangeException)
            {
                return "Date ou heure hors limites.";
            }

            return null;
        }

        private void Valider_Click(object sender, RoutedEventArgs e)
        {
            var message = EvaluerMessageErreurCreneau();
            if (!string.IsNullOrEmpty(message))
            {
                MettreAJourEtatValidation();
                return;
            }

            if (DpDate.SelectedDate is not DateTime datePart ||
                CbHour.SelectedItem is not string hs || CbMinute.SelectedItem is not string ms ||
                !int.TryParse(hs, NumberStyles.Integer, CultureInfo.InvariantCulture, out var h) ||
                !int.TryParse(ms, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mi))
                return;

            SelectedDateTime = new DateTime(datePart.Year, datePart.Month, datePart.Day, h, mi, 0, DateTimeKind.Local);
            DialogResult = true;
        }
    }
}
