using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Exceptions;
using smartest_desktop.Helpers;
using smartest_desktop.Services;
using smartest_desktop.Views;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfApp = System.Windows.Application;

namespace smartest_desktop.ViewModels
{
    public sealed class ExamenSessionCardVm : BaseViewModel
    {
        public long BackendId { get; init; }
        public string Titre { get; init; } = "";
        public string StatutAffiche { get; init; } = "";
        public string? DateCreneau { get; init; }
        public int NombreEtudiants { get; set; }

        private bool _estSelectionne;
        public bool EstSelectionne
        {
            get => _estSelectionne;
            set => SetProperty(ref _estSelectionne, value);
        }

        public string SousTitre =>
            (NombreEtudiants > 0 ? $"{NombreEtudiants} participant(s)" : "Aucun passage enregistré")
            + (string.IsNullOrWhiteSpace(DateCreneau) ? "" : $" · {DateCreneau}");

        public int NbParticipants => NombreEtudiants;

        public string DateFormatee => DateCreneau ?? "";

        public void NotifierSousTitre()
        {
            OnPropertyChanged(nameof(SousTitre));
            OnPropertyChanged(nameof(NbParticipants));
        }
    }

    public sealed class EtudiantSessionRowVm
    {
        private const double SeuilNoteReussite = 10;
        private const double EpsilonNote = 0.01;

        public long EtudiantId { get; init; }
        public string Nom { get; init; } = "";
        public string Email { get; init; } = "";
        public double? NoteProposee { get; init; }
        public double? NoteFinale { get; init; }
        public bool ValideeParProf { get; init; }

        public bool EstNoteCorrigee =>
            ValideeParProf
            && NoteFinale.HasValue
            && NoteProposee.HasValue
            && Math.Abs(NoteFinale.Value - NoteProposee.Value) > EpsilonNote;

        public string LibelleNote =>
            !ValideeParProf ? "Note proposée"
            : EstNoteCorrigee ? "Note corrigée"
            : "Note validée";

        public double? NoteAffichee =>
            !ValideeParProf ? NoteProposee : (NoteFinale ?? NoteProposee);

        public string NoteAfficheeText =>
            NoteAffichee.HasValue
                ? $"{NoteAffichee.Value.ToString("0.##", CultureInfo.InvariantCulture)}/20"
                : "—/20";

        public bool EstNoteReussie => NoteAffichee is >= SeuilNoteReussite;

        public string Initiale
        {
            get
            {
                var src = !string.IsNullOrWhiteSpace(Nom) ? Nom.Trim() : Email.Trim();
                if (string.IsNullOrEmpty(src)) return "?";
                return char.ToUpperInvariant(src[0]).ToString();
            }
        }
    }

    public sealed class LigneCorrectionSessionVm : BaseViewModel
    {
        private readonly Action? _onNoteChanged;
        private double _noteFinale;

        public long QuestionId { get; init; }
        public string Enonce { get; init; } = "";
        public string ReponseEtudiantFormatee { get; init; } = "";
        public bool CorrigeParIa { get; init; }
        public double NoteMax { get; init; }

        public double NoteFinale
        {
            get => _noteFinale;
            set
            {
                double borne = NoteMax > 0 ? NoteMax : 20;
                double clamped = Math.Round(Math.Clamp(value, 0, borne), 2, MidpointRounding.AwayFromZero);
                if (!SetProperty(ref _noteFinale, clamped)) return;
                _onNoteChanged?.Invoke();
            }
        }

        public LigneCorrectionSessionVm(Action? onNoteChanged = null) => _onNoteChanged = onNoteChanged;
    }

    public sealed class SessionsActivesViewModel : BaseViewModel
    {
        private const double SeuilNoteReussite = 10;

        private readonly LocalExamenService _examenLocal = new(App.LocalDb);
        private readonly ExamenWebPublicationApiService _api = new();
        private readonly AuthService _auth = new();

        private bool _chargementExamens;
        private bool _chargementEtudiants;
        private bool _busyCorrection;
        private string? _message;
        private int _niveauActuel = 1;
        private ExamenSessionCardVm? _examenSelectionne;
        private EtudiantSessionRowVm? _etudiantSelectionne;
        private double _baremeTotal = 20;

        public ObservableCollection<ExamenSessionCardVm> ExamensTermines { get; } = new();
        public ObservableCollection<EtudiantSessionRowVm> EtudiantsDuExamen { get; } = new();
        public ObservableCollection<LigneCorrectionSessionVm> LignesCorrection { get; } = new();

        public bool ChargementExamens
        {
            get => _chargementExamens;
            private set => SetProperty(ref _chargementExamens, value);
        }

        public bool ChargementEtudiants
        {
            get => _chargementEtudiants;
            private set => SetProperty(ref _chargementEtudiants, value);
        }

        public string? Message
        {
            get => _message;
            private set
            {
                if (!SetProperty(ref _message, value)) return;
                OnPropertyChanged(nameof(AfficherMessage));
            }
        }

        public bool AfficherMessage => !string.IsNullOrWhiteSpace(Message);

        public bool AucunExamen => !ChargementExamens && ExamensTermines.Count == 0;

        public bool AucunEtudiant => NiveauActuel >= 2 && ExamenSelectionne != null && !ChargementEtudiants && EtudiantsDuExamen.Count == 0;

        public int NiveauActuel
        {
            get => _niveauActuel;
            private set => SetProperty(ref _niveauActuel, value);
        }

        public bool BusyCorrection
        {
            get => _busyCorrection;
            private set
            {
                if (!SetProperty(ref _busyCorrection, value)) return;
                if (ValiderCorrectionCommand is RelayCommand v) v.RaiseCanExecuteChanged();
            }
        }

        public ExamenSessionCardVm? ExamenSelectionne
        {
            get => _examenSelectionne;
            private set
            {
                if (!SetProperty(ref _examenSelectionne, value)) return;
                foreach (var c in ExamensTermines)
                    c.EstSelectionne = c == value;
                OnPropertyChanged(nameof(TitreExamenSelectionne));
                OnPropertyChanged(nameof(AucunEtudiant));
            }
        }

        public EtudiantSessionRowVm? EtudiantSelectionne
        {
            get => _etudiantSelectionne;
            private set => SetProperty(ref _etudiantSelectionne, value);
        }

        public string TitreExamenSelectionne =>
            ExamenSelectionne != null ? ExamenSelectionne.Titre : "Sélectionnez un examen";

        public double BaremeTotal
        {
            get => _baremeTotal;
            private set => SetProperty(ref _baremeTotal, value);
        }

        public double NoteTotale =>
            Math.Round(LignesCorrection.Sum(l => l.NoteFinale), 2, MidpointRounding.AwayFromZero);

        public bool EstNoteTotaleReussie => NoteTotale >= SeuilNoteReussite;

        public ICommand RafraichirCommand { get; }
        public ICommand SelectionnerExamenCommand { get; }
        public ICommand SupprimerExamenCommand { get; }
        public ICommand RetourExamensCommand { get; }
        public ICommand VoirCorrectionCommand { get; }
        public ICommand RetourEtudiantsCommand { get; }
        public ICommand ValiderCorrectionCommand { get; }

        public SessionsActivesViewModel()
        {
            RafraichirCommand = new RelayCommand(_ => _ = RafraichirExamensAsync());
            SelectionnerExamenCommand = new RelayCommand(
                p => _ = SelectionnerExamenAsync(p as ExamenSessionCardVm),
                p => p is ExamenSessionCardVm);
            SupprimerExamenCommand = new RelayCommand(
                p => _ = SupprimerExamenDeLaListeAsync(p as ExamenSessionCardVm),
                p => p is ExamenSessionCardVm);
            RetourExamensCommand = new RelayCommand(_ => RetourNiveauExamens());
            VoirCorrectionCommand = new RelayCommand(
                p => _ = VoirCorrectionAsync(p as EtudiantSessionRowVm),
                p => p is EtudiantSessionRowVm);
            RetourEtudiantsCommand = new RelayCommand(_ => RetourNiveauEtudiants());
            ValiderCorrectionCommand = new RelayCommand(
                _ => _ = ValiderCorrectionAsync(),
                _ => !BusyCorrection && LignesCorrection.Count > 0 && ExamenSelectionne != null
                    && EtudiantSelectionne != null && CorrectionNotesValides);
        }

        private void NotifierNoteTotale()
        {
            OnPropertyChanged(nameof(NoteTotale));
            OnPropertyChanged(nameof(EstNoteTotaleReussie));
            OnPropertyChanged(nameof(CorrectionNotesValides));
            if (ValiderCorrectionCommand is RelayCommand v) v.RaiseCanExecuteChanged();
        }

        public bool CorrectionNotesValides =>
            LignesCorrection.Count > 0
            && LignesCorrection.All(l => l.NoteFinale >= 0 && l.NoteFinale <= l.NoteMax + 1e-6)
            && NoteTotale <= BaremeTotal + 1e-6;

        private void RetourNiveauExamens()
        {
            NiveauActuel = 1;
            ExamenSelectionne = null;
            EtudiantSelectionne = null;
            EtudiantsDuExamen.Clear();
            LignesCorrection.Clear();
        }

        private void RetourNiveauEtudiants()
        {
            NiveauActuel = 2;
            EtudiantSelectionne = null;
            LignesCorrection.Clear();
        }

        private async Task SupprimerExamenDeLaListeAsync(ExamenSessionCardVm? carte)
        {
            if (carte == null || carte.BackendId <= 0) return;

            var titre = string.IsNullOrWhiteSpace(carte.Titre) ? $"Examen #{carte.BackendId}" : carte.Titre.Trim();
            var confirm = MessageBox.Show(
                $"Supprimer « {titre} » ?\n\n" +
                "L'examen sera retiré de cette liste et ne sera plus visible par les étudiants sur le web.",
                "Sessions examens",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes)
                return;

            if (!DesktopSessionTokenHelper.TryObtenir(out string? token))
            {
                Message = "Reconnectez-vous pour supprimer l'examen sur le serveur.";
                return;
            }

            try
            {
                await ExecuterAvecRenouvellementJetonAsync(async t =>
                {
                    await _api.DeleteExamenPublieAsync(t, carte.BackendId);
                    return true;
                });
                RetirerExamensParBackendIds(new[] { carte.BackendId }, masquerPourSessions: false);
                Message = null;
            }
            catch (SmartestApiException ex)
            {
                Message = FormaterMessageErreurApi(ex);
            }
            catch (Exception ex)
            {
                Message = ex.Message;
            }
        }

        private async Task SelectionnerExamenAsync(ExamenSessionCardVm? carte)
        {
            if (carte == null) return;
            ExamenSelectionne = carte;
            NiveauActuel = 2;
            EtudiantSelectionne = null;
            LignesCorrection.Clear();
            await ChargerEtudiantsAsync(carte);
        }

        private async Task VoirCorrectionAsync(EtudiantSessionRowVm? etu)
        {
            if (etu == null || ExamenSelectionne == null) return;
            EtudiantSelectionne = etu;

            bool charge = await ChargerCorrectionAsync(ExamenSelectionne.BackendId, etu.EtudiantId);
            if (!charge)
                OuvrirCorrectionDialog();
        }

        /// <summary>Retire immédiatement les cartes (ex. depuis Quiz et examens).</summary>
        public void RetirerExamensParBackendIds(IEnumerable<long> backendIds, bool masquerPourSessions = false)
        {
            if (backendIds == null)
                return;
            var ids = new HashSet<long>(backendIds.Where(id => id > 0));
            if (ids.Count == 0)
                return;

            if (masquerPourSessions)
                SessionsActivesExamensMasques.Masquer(App.LocalDb, ids);
            else
                SessionsActivesExamensMasques.Demasquer(App.LocalDb, ids);

            foreach (long id in ids)
                SessionsActivesPassagesCache.Supprimer(App.LocalDb, id);

            var aRetirer = ExamensTermines.Where(c => ids.Contains(c.BackendId)).ToList();
            foreach (var carte in aRetirer)
            {
                if (ReferenceEquals(ExamenSelectionne, carte))
                {
                    RetourNiveauExamens();
                }
                ExamensTermines.Remove(carte);
            }

            OnPropertyChanged(nameof(AucunExamen));
        }

        public async Task RafraichirExamensAsync()
        {
            ChargementExamens = true;
            Message = null;
            ExamensTermines.Clear();
            ExamenSelectionne = null;
            EtudiantsDuExamen.Clear();
            LignesCorrection.Clear();
            NiveauActuel = 1;

            try
            {
                var masques = SessionsActivesExamensMasques.LireIds(App.LocalDb);
                var cartes = new Dictionary<long, ExamenSessionCardVm>();

                if (DesktopSessionTokenHelper.TryObtenir(out string? token))
                {
                    try
                    {
                        AjouterCartesDepuisPublicationsWeb(
                            cartes,
                            masques,
                            await ExecuterAvecRenouvellementJetonAsync(
                                t => _api.GetMesPublicationsWebAsync(t)));
                    }
                    catch (SmartestApiException)
                    {
                        /* repli sur examens locaux terminés */
                    }
                    catch (Exception)
                    {
                        /* repli sur examens locaux terminés */
                    }
                }

                await AjouterCartesDepuisExamensLocauxAsync(cartes, masques);

                foreach (var c in cartes.Values.OrderByDescending(x => x.DateCreneau ?? ""))
                    ExamensTermines.Add(c);

                foreach (var c in ExamensTermines)
                    HydraterCarteDepuisCacheParticipants(c);

                // Pas de bannière globale : liste vide = texte discret dans la zone scrollable uniquement.
                Message = null;
            }
            catch (Exception)
            {
                /* erreur locale : la liste vide affiche le texte d'aide dans la vue */
            }
            finally
            {
                ChargementExamens = false;
                Message = null;
                OnPropertyChanged(nameof(AucunExamen));
            }
        }

        private async Task<T> ExecuterAvecRenouvellementJetonAsync<T>(Func<string, Task<T>> action)
        {
            if (!DesktopSessionTokenHelper.TryObtenir(out string? token))
                throw new SmartestApiException(HttpStatusCode.Unauthorized, "", "Session absente.");

            for (int tentative = 0; tentative < 2; tentative++)
            {
                try
                {
                    return await action(token);
                }
                catch (SmartestApiException ex) when (
                    ex.StatusCode == HttpStatusCode.Unauthorized && tentative == 0)
                {
                    if (!await DesktopSessionTokenHelper.TryRafraichirDepuisApiAsync(_auth)
                        || !DesktopSessionTokenHelper.TryObtenir(out token))
                    {
                        throw;
                    }
                }
            }

            throw new SmartestApiException(HttpStatusCode.Unauthorized, "", "Session expirée.");
        }

        private static string FormaterMessageErreurApi(SmartestApiException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                return "Session expirée. Déconnectez-vous puis reconnectez-vous pour actualiser les données serveur.";
            }

            return ex.Message;
        }

        private static void AjouterCartesDepuisPublicationsWeb(
            Dictionary<long, ExamenSessionCardVm> cartes,
            HashSet<long> masques,
            IEnumerable<ExamenMesPublicationItem> publications)
        {
            foreach (var p in publications)
            {
                if (p.Id <= 0) continue;
                if (masques.Contains(p.Id)) continue;
                var statut = (p.Statut ?? "").Trim().ToUpperInvariant();
                if (statut != "TERMINE") continue;
                string? date = p.DateDebut.HasValue
                    ? p.DateDebut.Value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)
                    : null;
                cartes[p.Id] = new ExamenSessionCardVm
                {
                    BackendId = p.Id,
                    Titre = string.IsNullOrWhiteSpace(p.Titre) ? $"Examen #{p.Id}" : p.Titre.Trim(),
                    StatutAffiche = "Terminé",
                    DateCreneau = date,
                };
            }
        }

        private async Task AjouterCartesDepuisExamensLocauxAsync(
            Dictionary<long, ExamenSessionCardVm> cartes,
            HashSet<long> masques)
        {
            var locaux = await _examenLocal.GetAllAsync();
            foreach (ExamenLocal ex in locaux)
            {
                if (ex.BackendId is not long bid || bid <= 0) continue;
                if (masques.Contains(bid)) continue;
                string stLoc = ex.Statut?.Trim().ToUpperInvariant() ?? "";
                if (stLoc != "TERMINE")
                    continue;
                if (cartes.ContainsKey(bid)) continue;
                cartes[bid] = new ExamenSessionCardVm
                {
                    BackendId = bid,
                    Titre = ex.Titre?.Trim() ?? $"Examen #{bid}",
                    StatutAffiche = "Terminé",
                    DateCreneau = ex.DatePrevue?.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                };
            }
        }

        private static void HydraterCarteDepuisCacheParticipants(ExamenSessionCardVm c)
        {
            try
            {
                string? raw = SessionsActivesPassagesCache.EssayerCharger(App.LocalDb, c.BackendId);
                if (string.IsNullOrWhiteSpace(raw)) return;
                var list = JsonConvert.DeserializeObject<List<ExamenEtudiantPassageItem>>(raw);
                if (list == null || list.Count == 0) return;
                c.NombreEtudiants = list.Count;
                c.NotifierSousTitre();
            }
            catch
            {
                /* cache absent ou JSON invalide */
            }
        }

        private async Task ChargerEtudiantsAsync(ExamenSessionCardVm examen)
        {
            ChargementEtudiants = true;
            EtudiantsDuExamen.Clear();
            EtudiantSelectionne = null;

            try
            {
                if (!DesktopSessionTokenHelper.TryObtenir(out string? token))
                {
                    if (RemplirEtudiantsDepuisCache(examen, out string? msgCache))
                        Message = msgCache;
                    else
                    {
                        Message =
                            "Session expirée. Reconnectez-vous pour charger les participants depuis le serveur.";
                    }
                    return;
                }

                var rowsList = await ChargerParticipantsListeAsync(token, examen.BackendId);

                if (rowsList.Count == 0)
                {
                    bool consolide = await _api.ForcerConsolidationAsync(token, examen.BackendId);
                    if (consolide)
                    {
                        await Task.Delay(500);
                        rowsList = await ChargerParticipantsListeAsync(token, examen.BackendId);
                    }
                }

                if (rowsList.Count > 0)
                {
                    try
                    {
                        SessionsActivesPassagesCache.Sauvegarder(App.LocalDb, examen.BackendId, JsonConvert.SerializeObject(rowsList));
                    }
                    catch
                    {
                        /* non bloquant */
                    }
                }
                else
                {
                    RemplirEtudiantsDepuisCache(examen, out string? msgCache, ref rowsList);
                    if (!string.IsNullOrWhiteSpace(msgCache))
                        Message = msgCache;
                }

                AjouterEtudiantsDepuisListe(examen, rowsList);

                if (EtudiantsDuExamen.Count == 0)
                {
                    Message =
                        "Aucun participant trouvé sur le serveur pour cet examen.\n\n" +
                        "Vérifiez : (1) le backend a été redémarré après la dernière mise à jour, " +
                        "(2) l'étudiant a rejoint la salle et répondu pendant la session EN_COURS, " +
                        "(3) vous avez cliqué « Terminer » sur la supervision web (même examen, même numéro d'id), " +
                        "(4) le desktop pointe vers la même URL que le web (par défaut http://localhost:8081).";
                }
            }
            catch (SmartestApiException ex)
            {
                var rowsRepli = new List<ExamenEtudiantPassageItem>();
                bool depuisCache = RemplirEtudiantsDepuisCache(examen, out string? msgCache, ref rowsRepli);
                if (depuisCache && rowsRepli.Count > 0)
                {
                    AjouterEtudiantsDepuisListe(examen, rowsRepli);
                    Message = msgCache ?? ex.Message;
                    return;
                }

                if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    Message =
                        "Le serveur ne propose pas encore la route des participants. Redémarrez le backend Spring, " +
                        "puis actualisez.";
                }
                else if (ex.StatusCode == HttpStatusCode.Forbidden)
                {
                    Message =
                        "Accès refusé : connectez-vous avec le compte professeur propriétaire de cet examen " +
                        $"(id serveur {examen.BackendId}).";
                }
                else if (ex.StatusCode == HttpStatusCode.Unauthorized)
                {
                    Message = FormaterMessageErreurApi(ex);
                }
                else
                {
                    Message = ex.Message;
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
            }
            finally
            {
                ChargementEtudiants = false;
                OnPropertyChanged(nameof(AucunEtudiant));
            }
        }

        private static bool RemplirEtudiantsDepuisCache(
            ExamenSessionCardVm examen,
            out string? message,
            ref List<ExamenEtudiantPassageItem> rowsList)
        {
            message = null;
            string? cached = SessionsActivesPassagesCache.EssayerCharger(App.LocalDb, examen.BackendId);
            if (string.IsNullOrWhiteSpace(cached))
                return false;

            var parsed = JsonConvert.DeserializeObject<List<ExamenEtudiantPassageItem>>(cached);
            if (parsed is not { Count: > 0 })
                return false;

            rowsList = parsed;
            message =
                "Liste chargée depuis le cache local : derniers participants connus. " +
                "Reconnectez-vous et actualisez pour les données serveur à jour.";
            return true;
        }

        private bool RemplirEtudiantsDepuisCache(ExamenSessionCardVm examen, out string? message)
        {
            var rows = new List<ExamenEtudiantPassageItem>();
            bool ok = RemplirEtudiantsDepuisCache(examen, out message, ref rows);
            if (!ok || rows.Count == 0)
                return false;

            AjouterEtudiantsDepuisListe(examen, rows);
            return true;
        }

        private void AjouterEtudiantsDepuisListe(
            ExamenSessionCardVm examen,
            List<ExamenEtudiantPassageItem> rowsList)
        {
            foreach (var r in rowsList
                         .Where(x => x.EtudiantId > 0)
                         .OrderBy(x => x.Nom, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(x => x.Email, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(x => x.EtudiantId))
            {
                string email = r.Email?.Trim() ?? "";
                string nom = r.Nom?.Trim() ?? "";
                string principal = !string.IsNullOrEmpty(nom)
                    ? nom
                    : (!string.IsNullOrEmpty(email) ? email : $"Étudiant #{r.EtudiantId}");
                EtudiantsDuExamen.Add(new EtudiantSessionRowVm
                {
                    EtudiantId = r.EtudiantId,
                    Nom = principal,
                    Email = !string.IsNullOrEmpty(email) && principal != email ? email : "",
                    NoteProposee = r.NoteProposee,
                    NoteFinale = r.NoteFinale,
                    ValideeParProf = r.ValideeParProf,
                });
            }

            examen.NombreEtudiants = EtudiantsDuExamen.Count;
            examen.NotifierSousTitre();
        }

        /// <summary>
        /// Charge les participants : service API puis repli HTTP direct (désérialisation JToken tolérante).
        /// Corrige les listes vides silencieuses quand le JSON est valide mais mal mappé.
        /// </summary>
        private async Task<List<ExamenEtudiantPassageItem>> ChargerParticipantsListeAsync(string token, long examenBackendId)
        {
            var fusion = new Dictionary<long, ExamenEtudiantPassageItem>();

            void Ajouter(IEnumerable<ExamenEtudiantPassageItem> items)
            {
                foreach (var item in items)
                {
                    if (item == null || item.EtudiantId <= 0) continue;
                    fusion[item.EtudiantId] = item;
                }
            }

            try
            {
                Ajouter(await _api.GetEtudiantsPassagesAsync(token, examenBackendId));
            }
            catch (SmartestApiException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden)
            {
                /* repli HTTP ci-dessous */
            }

            if (fusion.Count == 0)
                Ajouter(await ChargerParticipantsDepuisApiDirecteAsync(token, examenBackendId));

            return fusion.Values.ToList();
        }

        private static async Task<List<ExamenEtudiantPassageItem>> ChargerParticipantsDepuisApiDirecteAsync(
            string token,
            long examenBackendId)
        {
            var map = new Dictionary<long, ExamenEtudiantPassageItem>();
            string baseUrl = SmartestBackendBaseUrl.Resolve().TrimEnd('/');
            using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            foreach (string suffix in new[] { "etudiants-passages", "participants-soumis" })
            {
                string path = $"/api/examens-publies/{examenBackendId}/supervision/{suffix}";
                HttpResponseMessage response;
                try
                {
                    response = await http.GetAsync(path);
                }
                catch
                {
                    continue;
                }

                string body = await response.Content.ReadAsStringAsync();
                if (response.StatusCode == HttpStatusCode.NotFound)
                    continue;
                if (!response.IsSuccessStatusCode)
                    throw SmartestApiException.FromHttpFailure(response.StatusCode, body, "Liste étudiants examen");

                if (string.IsNullOrWhiteSpace(body))
                    continue;

                FusionnerParticipantsJson(map, body);
                if (map.Count > 0)
                    break;
            }

            return map.Values.ToList();
        }

        private static void FusionnerParticipantsJson(Dictionary<long, ExamenEtudiantPassageItem> map, string jsonBody)
        {
            JToken root;
            try
            {
                root = JToken.Parse(jsonBody);
            }
            catch (JsonException)
            {
                return;
            }

            JArray? array = root as JArray;
            if (array == null && root is JObject obj)
            {
                array = obj["participants"] as JArray
                    ?? obj["etudiants"] as JArray
                    ?? obj["data"] as JArray;
            }

            if (array == null)
                return;

            foreach (JToken tok in array)
            {
                if (tok is not JObject row) continue;
                long? eid = TryReadLong(row["etudiantId"] ?? row["EtudiantId"] ?? row["id"] ?? row["Id"]);
                if (eid == null || eid <= 0) continue;

                double? noteProposee = TryReadDouble(row["noteProposee"] ?? row["NoteProposee"]);
                double? noteFinale = TryReadDouble(row["noteFinale"] ?? row["NoteFinale"]);
                bool validee = row["valideeParProf"]?.Value<bool>()
                    ?? row["ValideeParProf"]?.Value<bool>()
                    ?? false;

                map[eid.Value] = new ExamenEtudiantPassageItem
                {
                    EtudiantId = eid.Value,
                    Email = row["email"]?.Value<string>() ?? row["Email"]?.Value<string>() ?? "",
                    Nom = row["nom"]?.Value<string>() ?? row["Nom"]?.Value<string>() ?? "",
                    NoteProposee = noteProposee,
                    NoteFinale = noteFinale,
                    ValideeParProf = validee,
                };
            }
        }

        private static long? TryReadLong(JToken? tok)
        {
            if (tok == null || tok.Type == JTokenType.Null) return null;
            return tok.Type switch
            {
                JTokenType.Integer => tok.Value<long>(),
                JTokenType.Float => (long)Math.Round(tok.Value<double>(), MidpointRounding.AwayFromZero),
                JTokenType.String => long.TryParse(tok.Value<string>(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var l)
                    ? l
                    : null,
                _ => null,
            };
        }

        private static double? TryReadDouble(JToken? tok)
        {
            if (tok == null || tok.Type == JTokenType.Null) return null;
            return tok.Type switch
            {
                JTokenType.Integer => tok.Value<double>(),
                JTokenType.Float => tok.Value<double>(),
                JTokenType.String => double.TryParse(tok.Value<string>(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
                    ? d
                    : null,
                _ => null,
            };
        }

        private void OuvrirCorrectionDialog()
        {
            if (ExamenSelectionne == null || EtudiantSelectionne == null) return;
            if (!DesktopSessionTokenHelper.TryObtenir(out string? token)) return;

            var dlg = new ExamenCorrectionEtudiantDialog(
                ExamenSelectionne.BackendId,
                ExamenSelectionne.Titre,
                EtudiantSelectionne.EtudiantId,
                token);
            if (WpfApp.Current.MainWindow != null && WpfApp.Current.MainWindow.IsLoaded)
                dlg.Owner = WpfApp.Current.MainWindow;
            if (dlg.ShowDialog() == true)
            {
                Message = "Correction enregistrée.";
                _ = ChargerEtudiantsAsync(ExamenSelectionne);
            }
        }

        private async Task<bool> ChargerCorrectionAsync(long examenId, long etudiantId)
        {
            BusyCorrection = true;
            Message = null;
            LignesCorrection.Clear();
            try
            {
                if (!DesktopSessionTokenHelper.TryObtenir(out _))
                {
                    Message = "Session expirée. Reconnectez-vous pour corriger.";
                    return false;
                }

                var root = await ExecuterAvecRenouvellementJetonAsync(
                    t => _api.GetCorrectionsEtudiantAsync(t, examenId, etudiantId));
                var baremeTok = root["baremeReference"];
                if (baremeTok != null && (baremeTok.Type == JTokenType.Float || baremeTok.Type == JTokenType.Integer))
                    BaremeTotal = baremeTok.Value<double>();

                var arr = root["lignes"] as JArray;
                if (arr == null || arr.Count == 0)
                {
                    Message = "Aucune ligne de correction pour cet étudiant.";
                    return false;
                }

                foreach (var item in arr)
                {
                    if (item is not JObject row) continue;
                    long qid = row["questionId"]?.Value<long>() ?? 0;
                    if (qid <= 0) continue;
                    var spTok = row["scorePartiel"];
                    double? sp = spTok == null || spTok.Type == JTokenType.Null ? null : spTok.Value<double>();
                    var nflTok = row["noteFinaleLigne"];
                    double? nfl = nflTok == null || nflTok.Type == JTokenType.Null ? null : nflTok.Value<double>();
                    double pts = row["pointsQuestionMax"]?.Value<double>() ?? 0;
                    double init = nfl ?? sp ?? 0;
                    LignesCorrection.Add(new LigneCorrectionSessionVm(NotifierNoteTotale)
                    {
                        QuestionId = qid,
                        Enonce = row["enonce"]?.Value<string>() ?? "",
                        ReponseEtudiantFormatee = row["reponseEtudiantLibelle"]?.Value<string>() ?? "",
                        CorrigeParIa = row["corrigeParIa"]?.Value<bool>() ?? false,
                        NoteMax = pts,
                        NoteFinale = init,
                    });
                }

                NiveauActuel = 3;
                NotifierNoteTotale();
                return true;
            }
            catch (SmartestApiException ex)
            {
                Message = FormaterMessageErreurApi(ex);
                return false;
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                return false;
            }
            finally
            {
                BusyCorrection = false;
            }
        }

        private async Task ValiderCorrectionAsync()
        {
            if (ExamenSelectionne == null || EtudiantSelectionne == null || LignesCorrection.Count == 0) return;
            if (!CorrectionNotesValides)
            {
                Message = $"Notes invalides : chaque question est plafonnée à son barème et le total ne peut pas dépasser {BaremeTotal:0.##}.";
                return;
            }
            BusyCorrection = true;
            Message = null;
            try
            {
                if (!DesktopSessionTokenHelper.TryObtenir(out string? token))
                {
                    Message = "Session expirée. Reconnectez-vous.";
                    return;
                }

                var notes = new Dictionary<string, double>();
                foreach (var l in LignesCorrection)
                    notes[l.QuestionId.ToString(CultureInfo.InvariantCulture)] = l.NoteFinale;

                var body = new
                {
                    notesFinales = notes,
                    noteTotale = NoteTotale,
                    remarque = (string?)null,
                };
                await ExecuterAvecRenouvellementJetonAsync(async t =>
                {
                    await _api.ValiderCorrectionsDetailAsync(
                        t,
                        ExamenSelectionne.BackendId,
                        EtudiantSelectionne.EtudiantId,
                        JsonConvert.SerializeObject(body));
                    return true;
                });
                try
                {
                    await ExecuterAvecRenouvellementJetonAsync(async t =>
                    {
                        await _api.SynchroniserNoteWorkbenchAsync(
                            t,
                            ExamenSelectionne.BackendId,
                            EtudiantSelectionne.EtudiantId);
                        return true;
                    });
                }
                catch (SmartestApiException)
                {
                    /* Publication déjà effectuée selon la configuration serveur. */
                }

                Message = "Correction validée. La note est visible côté étudiant web.";
                await ChargerEtudiantsAsync(ExamenSelectionne);
                RetourNiveauEtudiants();
            }
            catch (SmartestApiException ex)
            {
                Message = ex.Message;
            }
            catch (Exception ex)
            {
                Message = ex.Message;
            }
            finally
            {
                BusyCorrection = false;
            }
        }
    }
}
