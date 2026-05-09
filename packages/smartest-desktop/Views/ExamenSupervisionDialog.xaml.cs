using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;
using smartest_desktop.Exceptions;
using smartest_desktop.Services;
using WpfApp = System.Windows.Application;

namespace smartest_desktop.Views
{
    public partial class ExamenSupervisionDialog : Window
    {
        private readonly long _examenId;
        private readonly ExamenWebPublicationApiService _api = new();
        private readonly DispatcherTimer _timer;
        private string? _token;

        public ExamenSupervisionDialog(long examenBackendId, string titreExamen)
        {
            InitializeComponent();
            _examenId = examenBackendId;
            TxtTitle.Text = titreExamen?.Trim() ?? $"Examen #{examenBackendId}";

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2.5) };
            _timer.Tick += async (_, _) => await RafraichirSilencieuxAsync();
        }

        private string RequireToken()
        {
            if (!string.IsNullOrWhiteSpace(_token)) return _token!;
            var t = WpfApp.Current.Properties["Token"]?.ToString();
            if (string.IsNullOrWhiteSpace(t))
            {
                MessageBox.Show(
                    "Session invalide ou expirée. Reconnectez-vous.",
                    "Supervision",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                throw new InvalidOperationException("no token");
            }
            _token = t;
            return _token;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await RafraichirAsync();
                _timer.Start();
            }
            catch (Exception ex)
            {
                AfficherErreur(ex.Message);
            }
        }

        private async Task RafraichirSilencieuxAsync()
        {
            try
            {
                string tok = RequireToken();
                var snapTask = _api.GetSupervisionSnapshotAsync(tok, _examenId);
                var roomTask = _api.GetSalleAttenteAsync(tok, _examenId);
                await Task.WhenAll(snapTask, roomTask).ConfigureAwait(false);
                var snap = await snapTask.ConfigureAwait(false);
                var room = await roomTask.ConfigureAwait(false);
                await Dispatcher.InvokeAsync(() =>
                {
                    AppliquerSnapshot(snap);
                    AppliquerSalleAttente(room);
                });
            }
            catch
            {
                /* garder le dernier état ; erreurs déjà visibles sur actions */
            }
        }

        private async Task RafraichirAsync()
        {
            string tok = RequireToken();
            var snapTask = _api.GetSupervisionSnapshotAsync(tok, _examenId);
            var roomTask = _api.GetSalleAttenteAsync(tok, _examenId);
            await Task.WhenAll(snapTask, roomTask).ConfigureAwait(false);
            var snap = await snapTask.ConfigureAwait(false);
            var room = await roomTask.ConfigureAwait(false);
            await Dispatcher.InvokeAsync(() =>
            {
                AppliquerSnapshot(snap);
                AppliquerSalleAttente(room);
                TxtErreur.Visibility = Visibility.Collapsed;
            });
        }

        private void AppliquerSalleAttente(JObject room)
        {
            int n = room.Value<int?>("nombreConnectes") ?? room.Value<int?>("NombreConnectes") ?? 0;
            TxtSalleAttenteCount.Text = n <= 0
                ? "Aucun étudiant connecté pour le moment."
                : $"{n} étudiant(s) connecté(s) — en attente ou en session web.";

            ListAttente.Items.Clear();
            var arr = room["connectes"] as JArray ?? room["Connectes"] as JArray;
            if (arr == null || arr.Count == 0)
            {
                ListAttente.Items.Add("—");
                return;
            }

            foreach (var tok in arr)
            {
                if (tok is not JObject o) continue;
                var email = o["email"]?.ToString()?.Trim() ?? o["Email"]?.ToString()?.Trim();
                var sid = o["etudiantId"] ?? o["EtudiantId"];
                var idStr = sid != null ? sid.ToString() : "?";
                var label = string.IsNullOrWhiteSpace(email) ? $"Étudiant #{idStr}" : $"{email} (id {idStr})";
                ListAttente.Items.Add(label);
            }
        }

        private void AppliquerSnapshot(JObject snap)
        {
            string etat = (snap["etat"] ?? snap["Etat"])?.ToString()?.Trim().ToUpperInvariant() ?? "";
            int qIdx = snap.Value<int?>("questionCouranteIndex") ?? snap.Value<int?>("QuestionCouranteIndex") ?? 0;
            int total = snap.Value<int?>("totalQuestions") ?? snap.Value<int?>("TotalQuestions") ?? 0;
            int temps = snap.Value<int?>("tempsRestantMinutes") ?? snap.Value<int?>("TempsRestantMinutes") ?? 0;

            TxtEtat.Text = LibelleEtat(etat);
            if (total > 0)
                TxtQuestion.Text = $"Question {Math.Min(qIdx + 1, total)} / {total}";
            else
                TxtQuestion.Text = "Aucune question sur le serveur.";
            TxtTemps.Text = temps >= 0 ? $"Temps restant (minuteur) : {temps} min" : "";

            var qc = snap["questionCourante"] ?? snap["QuestionCourante"];
            if (qc is JObject q)
            {
                var enonce = q["enonce"]?.ToString() ?? q["Enonce"]?.ToString();
                if (!string.IsNullOrWhiteSpace(enonce))
                    TxtApercu.Text = enonce!.Length > 400 ? enonce[..400] + "…" : enonce;
                else
                    TxtApercu.Text = "";
            }
            else
                TxtApercu.Text = "";

            bool termine = etat == "TERMINE" || etat == "ARRETE";
            BtnDemarrer.Visibility = etat == "PLANIFIE" ? Visibility.Visible : Visibility.Collapsed;
            BtnPause.Visibility = etat == "EN_COURS" ? Visibility.Visible : Visibility.Collapsed;
            BtnReprendre.Visibility = etat == "EN_PAUSE" ? Visibility.Visible : Visibility.Collapsed;
            BtnPrecedente.Visibility = etat == "EN_COURS" ? Visibility.Visible : Visibility.Collapsed;
            BtnSuivante.Visibility = etat == "EN_COURS" ? Visibility.Visible : Visibility.Collapsed;
            BtnTempsMoins.Visibility = (etat == "EN_COURS" || etat == "EN_PAUSE" || etat == "PLANIFIE") && !termine
                ? Visibility.Visible
                : Visibility.Collapsed;
            BtnTempsPlus.Visibility = BtnTempsMoins.Visibility;
            BtnTerminer.Visibility = termine ? Visibility.Collapsed : Visibility.Visible;

            BtnDemarrer.IsEnabled = !termine && etat == "PLANIFIE";
            BtnPause.IsEnabled = !termine && etat == "EN_COURS";
            BtnReprendre.IsEnabled = !termine && etat == "EN_PAUSE";
            BtnPrecedente.IsEnabled = !termine && etat == "EN_COURS" && total > 0 && qIdx > 0;
            BtnSuivante.IsEnabled = !termine && etat == "EN_COURS" && total > 0;
            BtnTempsMoins.IsEnabled = !termine && (etat == "EN_COURS" || etat == "EN_PAUSE" || etat == "PLANIFIE");
            BtnTempsPlus.IsEnabled = BtnTempsMoins.IsEnabled;
            BtnTerminer.IsEnabled = !termine;
        }

        private static string LibelleEtat(string etat)
        {
            return etat switch
            {
                "PLANIFIE" => "Planifié — en attente du démarrage",
                "EN_COURS" => "En cours",
                "EN_PAUSE" => "En pause",
                "TERMINE" => "Terminé",
                "ARRETE" => "Arrêté",
                _ => string.IsNullOrEmpty(etat) ? "—" : etat,
            };
        }

        private void AfficherErreur(string message)
        {
            TxtErreur.Text = message;
            TxtErreur.Visibility = Visibility.Visible;
        }

        private async Task ExecuterAsync(Func<Task<JObject>> action)
        {
            try
            {
                var snap = await action();
                AppliquerSnapshot(snap);
                TxtErreur.Visibility = Visibility.Collapsed;
            }
            catch (SmartestApiException ex)
            {
                AfficherErreur(ex.Message);
            }
            catch (SmartestNetworkException ex)
            {
                AfficherErreur(ex.Message);
            }
            catch (InvalidOperationException)
            {
                /* token manquant */
            }
            catch (Exception ex)
            {
                AfficherErreur(ex.Message);
            }
        }

        private async void BtnDemarrer_Click(object sender, RoutedEventArgs e)
        {
            await ExecuterAsync(async () =>
            {
                string tok = RequireToken();
                return await _api.ControlerExamenAsync(tok, _examenId, "lancer");
            });
        }

        private async void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            await ExecuterAsync(async () =>
            {
                string tok = RequireToken();
                return await _api.ControlerExamenAsync(tok, _examenId, "pause");
            });
        }

        private async void BtnReprendre_Click(object sender, RoutedEventArgs e)
        {
            await ExecuterAsync(async () =>
            {
                string tok = RequireToken();
                return await _api.ControlerExamenAsync(tok, _examenId, "reprendre");
            });
        }

        private async void BtnPrecedente_Click(object sender, RoutedEventArgs e)
        {
            await ExecuterAsync(async () =>
            {
                string tok = RequireToken();
                return await _api.QuestionPrecedenteAsync(tok, _examenId);
            });
        }

        private async void BtnSuivante_Click(object sender, RoutedEventArgs e)
        {
            await ExecuterAsync(async () =>
            {
                string tok = RequireToken();
                return await _api.QuestionSuivanteAsync(tok, _examenId);
            });
        }

        private async void BtnTempsMoins_Click(object sender, RoutedEventArgs e)
        {
            await ExecuterAsync(async () =>
            {
                string tok = RequireToken();
                return await _api.AjusterTempsAsync(tok, _examenId, -5);
            });
        }

        private async void BtnTempsPlus_Click(object sender, RoutedEventArgs e)
        {
            await ExecuterAsync(async () =>
            {
                string tok = RequireToken();
                return await _api.AjusterTempsAsync(tok, _examenId, 5);
            });
        }

        private async void BtnTerminer_Click(object sender, RoutedEventArgs e)
        {
            var conf = MessageBox.Show(
                "Terminer l'examen pour tous les étudiants ? Ils pourront alors envoyer leurs réponses une dernière fois sur la page web (aucune correction immédiate).",
                "Confirmer",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No);
            if (conf != MessageBoxResult.Yes) return;

            await ExecuterAsync(async () =>
            {
                string tok = RequireToken();
                return await _api.ControlerExamenAsync(tok, _examenId, "terminer");
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer.Stop();
            base.OnClosed(e);
        }
    }
}
