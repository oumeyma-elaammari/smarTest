using System;
using System.Globalization;
using System.Linq;
using System.Windows;

namespace smartest_desktop.Views
{
    public partial class ChoisirDateHeureLancementExamenDialog : Window
    {
        public DateTime SelectedDateTime { get; private set; }

        public ChoisirDateHeureLancementExamenDialog(DateTime suggested)
        {
            InitializeComponent();
            var local = suggested.Kind == DateTimeKind.Utc ? suggested.ToLocalTime() : suggested;

            DpDate.SelectedDate = local.Date;
            var hours = Enumerable.Range(0, 24).Select(i => i.ToString("D2", CultureInfo.InvariantCulture)).ToArray();
            var minutes = Enumerable.Range(0, 60).Select(i => i.ToString("D2", CultureInfo.InvariantCulture)).ToArray();
            CbHour.ItemsSource = hours;
            CbMinute.ItemsSource = minutes;
            CbHour.SelectedItem = local.Hour.ToString("D2", CultureInfo.InvariantCulture);
            CbMinute.SelectedItem = local.Minute.ToString("D2", CultureInfo.InvariantCulture);
        }

        private void Valider_Click(object sender, RoutedEventArgs e)
        {
            if (DpDate.SelectedDate is not DateTime datePart)
            {
                MessageBox.Show(
                    "Choisissez une date pour le lancement.",
                    "Publication web examen",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (CbHour.SelectedItem is not string hs || CbMinute.SelectedItem is not string ms ||
                !int.TryParse(hs, NumberStyles.Integer, CultureInfo.InvariantCulture, out var h) ||
                !int.TryParse(ms, NumberStyles.Integer, CultureInfo.InvariantCulture, out var mi))
            {
                MessageBox.Show(
                    "Heure invalide.",
                    "Publication web examen",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                SelectedDateTime = new DateTime(datePart.Year, datePart.Month, datePart.Day, h, mi, 0, DateTimeKind.Local);
            }
            catch (ArgumentOutOfRangeException)
            {
                MessageBox.Show(
                    "Date ou heure hors limites.",
                    "Publication web examen",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }
    }
}
