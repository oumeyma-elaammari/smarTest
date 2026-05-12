using Microsoft.Win32;
using smartest_desktop.Constants;
using smartest_desktop.Helpers;
using smartest_desktop.Services;
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace smartest_desktop.ViewModels
{
    public class ExamenResultViewModel : BaseViewModel
    {
        private int _dureeMinutes;

        private readonly string _empreinteInitiale;

        private readonly Func<Task>? _supprimerExamenPersisteAsync;

        public string TitreExamen
        {
            get => _titreExamen;
            set => SetProperty(ref _titreExamen, value);
        }

        private string _titreExamen = string.Empty;

        public int Duree => _dureeMinutes;

        /// <summary>Durée modifiable avant publication (minutes).</summary>
        public int DureeMinutesEditable
        {
            get => _dureeMinutes;
            set
            {
                int v = Math.Clamp(value, 1, 600);
                if (!SetProperty(ref _dureeMinutes, v))
                    return;
                OnPropertyChanged(nameof(Duree));
                OnPropertyChanged(nameof(SousTitreCompteur));
                OnPropertyChanged(nameof(LibelleCreneauResume));
            }
        }

        public string Difficulte { get; }

        public string CoursSourceLabel { get; }

        public ObservableCollection<QuestionExamen> Questions { get; } = new();

        public int NombreQuestions => Questions.Count;

        private QuestionExamen? _questionSelectionnee;

        public QuestionExamen? QuestionSelectionnee
        {
            get => _questionSelectionnee;
            set
            {
                SetProperty(ref _questionSelectionnee, value);
                OnPropertyChanged(nameof(HasQuestion));
                OnPropertyChanged(nameof(NoQuestion));
                NotifierVuesPanneauDroit();
            }
        }

        public bool HasQuestion => QuestionSelectionnee != null;
        public bool NoQuestion => QuestionSelectionnee == null;

        public int? ExamenIdExistant { get; }

        public bool IsEditionExistant => ExamenIdExistant.HasValue;

        /// <summary>BROUILLON, PUBLIE, etc.</summary>
        public string StatutExamen { get; }

        public bool QuestionsVerrouilleesParPublication =>
            string.Equals(StatutExamen?.Trim(), "PUBLIE", StringComparison.OrdinalIgnoreCase);

        public string TitreFenetre =>
            IsEditionExistant ? "SmarTest — Modifier l'examen" : "SmarTest — Révision de l'examen";

        public string LibelleBoutonValider =>
            IsEditionExistant ? "Enregistrer les modifications" : "Valider et sauvegarder";

        public string SousTitreEtape =>
            QuestionsVerrouilleesParPublication
                ? "Questions en lecture seule · publication web et créneau modifiables"
                : IsEditionExistant
                    ? "Modifiez puis enregistrez dans la base locale"
                    : "Vérifiez et ajustez avant validation";

        public string SousTitreCompteur =>
            IsEditionExistant
                ? $"{NombreQuestions} questions · {Duree} min · {Difficulte} · {CoursSourceLabel}"
                : $"{NombreQuestions} questions générées · {Duree} min · {Difficulte} · {CoursSourceLabel}";

        // ── Publication web (emails) ───────────────────────────────────────────

        private string _texteEmailsWeb = string.Empty;

        public string TexteEmailsWeb
        {
            get => _texteEmailsWeb;
            set
            {
                if (!SetProperty(ref _texteEmailsWeb, value ?? string.Empty))
                    return;

                OnPropertyChanged(nameof(LibelleEmailsPublicationWeb));
                RafraichirEmailsListeApercu();
                if (EffacerEmailsWebCommand is RelayCommand re)
                    re.RaiseCanExecuteChanged();
            }
        }

        public string LibelleEmailsPublicationWeb =>
            $"{CompterEmailsValides(TexteEmailsWeb)} valide(s) — plafond {QuizPublicationLimits.MaxAuthorizedStudentEmails}";

        private string _nouvelEmailWeb = string.Empty;

        public string NouvelEmailWeb
        {
            get => _nouvelEmailWeb;
            set
            {
                if (!SetProperty(ref _nouvelEmailWeb, value ?? string.Empty))
                    return;
                if (AjouterEmailUnitaireWebCommand is RelayCommand r)
                    r.RaiseCanExecuteChanged();
            }
        }

        public ICommand ImporterEmailsWebCommand { get; }
        public ICommand EffacerEmailsWebCommand { get; }
        public ICommand AjouterEmailUnitaireWebCommand { get; }
        public ICommand SupprimerEmailsSelectionnesWebCommand { get; }
        public ICommand VoirListeEtudiantsCommand { get; }

        private bool _afficherListeEtudiants;

        public bool AfficherListeEtudiants
        {
            get => _afficherListeEtudiants;
            set
            {
                if (!SetProperty(ref _afficherListeEtudiants, value))
                    return;
                NotifierVuesPanneauDroit();
                if (value)
                    RafraichirEmailsListeApercu();
            }
        }

        public bool AfficherDetailQuestion => HasQuestion && !_afficherListeEtudiants;

        public bool AfficherEtatSansQuestion => !_afficherListeEtudiants && !HasQuestion;

        public bool AfficherListeEtudiantsDroite => _afficherListeEtudiants;

        public ObservableCollection<PublicationWebEmailRowViewModel> EmailsListeApercu { get; } = new();

        public bool AucunEmailListeApercu => EmailsListeApercu.Count == 0;

        public bool AEmailsSelectionnes => EmailsListeApercu.Any(r => r.IsSelected);

        // ── Créneau ─────────────────────────────────────────────────────────────

        public IReadOnlyList<string> ItemsHeures { get; }
        public IReadOnlyList<string> ItemsMinutes { get; }

        private DateTime _dateExamen;

        /// <summary>Date du jour de passage (sans composante heure significative pour le picker).</summary>
        public DateTime DateExamen
        {
            get => _dateExamen;
            set
            {
                if (!SetProperty(ref _dateExamen, value.Date))
                    return;
                OnPropertyChanged(nameof(LibelleCreneauResume));
            }
        }

        private string _heureSelectionnee = "09";

        public string HeureSelectionnee
        {
            get => _heureSelectionnee;
            set
            {
                if (!SetProperty(ref _heureSelectionnee, value ?? "09"))
                    return;
                OnPropertyChanged(nameof(LibelleCreneauResume));
            }
        }

        private string _minuteSelectionnee = "00";

        public string MinuteSelectionnee
        {
            get => _minuteSelectionnee;
            set
            {
                if (!SetProperty(ref _minuteSelectionnee, value ?? "00"))
                    return;
                OnPropertyChanged(nameof(LibelleCreneauResume));
            }
        }

        public string LibelleCreneauResume
        {
            get
            {
                if (!TryParseHeure(out int h, out int m))
                    return "Créneau : —";
                try
                {
                    var dt = new DateTime(DateExamen.Year, DateExamen.Month, DateExamen.Day, h, m, 0, DateTimeKind.Local);
                    return $"Créneau : {dt:dddd d MMMM yyyy à HH\\hmm} ({Duree} min)";
                }
                catch
                {
                    return "Créneau : —";
                }
            }
        }

        public IReadOnlyList<double> ValeursBareme { get; } =
            Enumerable.Range(0, 81).Select(i => i * ExamenBaremeHelper.Pas).ToList();

        public double TotalBareme => Math.Round(Questions.Sum(q => q.BaremePoints), 2);
        public double EcartBareme => Math.Round(ExamenBaremeHelper.TotalCible - TotalBareme, 2);
        public bool BaremeValide => Math.Abs(EcartBareme) < 1e-9 && Questions.All(q => ExamenBaremeHelper.EstAuPas(q.BaremePoints));
        public string ResumeBareme => $"Barème : {ExamenBaremeHelper.FormatPoints(TotalBareme)} / {ExamenBaremeHelper.FormatPoints(ExamenBaremeHelper.TotalCible)}";

        public ICommand SelectionnerCommand { get; }
        public ICommand SupprimerCommand { get; }
        /// <summary>Paramètre : type de question — QCM, VF, CHECKBOX ou REDACTION.</summary>
        public ICommand AjouterQuestionCommand { get; }
        public ICommand AttacherImageCommand { get; }
        public ICommand SupprimerImageCommand { get; }
        public ICommand ValiderExamenCommand { get; }
        public ICommand RegenerarCommand { get; }
        public ICommand RetourCommand { get; }

        public ICommand SetReponseCorrecteCommand { get; }
        private readonly RelayCommand _validerExamenCommand;

        /// <summary>
        /// emailsJson : tableau JSON ; datePrevuePassage : début local pour la publication web.
        /// </summary>
        public event Action<List<QuestionExamen>, string, int, string, string, string?, DateTime?>? ExamenValide;

        public event Action? NavigationRegenerarRequested;
        public event Action? NavigationRetourRequested;

        public ExamenResultViewModel(
            List<QuestionExamen> questions,
            string titre,
            int duree,
            string difficulte,
            string coursTitre,
            int? examenIdExistant = null,
            Func<Task>? supprimerExamenPersisteAsync = null,
            string statutExamen = "BROUILLON",
            string? emailsPublicationWebJsonInit = null,
            DateTime? datePrevueInit = null)
        {
            ExamenIdExistant = examenIdExistant;
            _supprimerExamenPersisteAsync = supprimerExamenPersisteAsync;
            StatutExamen = statutExamen ?? "BROUILLON";

            ItemsHeures = Enumerable.Range(0, 24).Select(i => i.ToString("D2", CultureInfo.InvariantCulture)).ToList();
            ItemsMinutes = Enumerable.Range(0, 60).Select(i => i.ToString("D2", CultureInfo.InvariantCulture)).ToList();

            _dureeMinutes = Math.Clamp(duree, 1, 600);
            TitreExamen = titre;
            Difficulte = difficulte;
            CoursSourceLabel = coursTitre;

            TexteEmailsWeb = InitialiserTexteEmailsDepuisJson(emailsPublicationWebJsonInit);

            var creneau = datePrevueInit ?? DateTime.Now.AddDays(1).Date.AddHours(9);
            if (creneau.Kind == DateTimeKind.Utc)
                creneau = creneau.ToLocalTime();
            _dateExamen = creneau.Date;
            _heureSelectionnee = creneau.Hour.ToString("D2", CultureInfo.InvariantCulture);
            _minuteSelectionnee = creneau.Minute.ToString("D2", CultureInfo.InvariantCulture);

            ImporterEmailsWebCommand = new RelayCommand(_ => ImporterEmailsWebDepuisFichier());
            EffacerEmailsWebCommand = new RelayCommand(
                _ => TexteEmailsWeb = string.Empty,
                _ => !string.IsNullOrWhiteSpace(TexteEmailsWeb));
            AjouterEmailUnitaireWebCommand = new RelayCommand(
                _ => AjouterEmailUnitaireWeb(),
                _ => !string.IsNullOrWhiteSpace(NouvelEmailWeb?.Trim()));
            SupprimerEmailsSelectionnesWebCommand = new RelayCommand(
                _ => SupprimerEmailsSelectionnesWeb(),
                _ => EmailsListeApercu.Any(r => r.IsSelected));

            VoirListeEtudiantsCommand = new RelayCommand(_ => AfficherListeEtudiants = true);

            int n = 1;
            foreach (var q in questions)
            {
                q.Numero = n++;
                Questions.Add(q);
            }

            if (Questions.Count > 0)
            {
                Questions[0].IsSelected = true;
                QuestionSelectionnee = Questions[0];
            }

            if (Questions.Sum(q => q.BaremePoints) <= 0.001)
                ExamenBaremeHelper.AppliquerBaremeParDefaut(Questions.ToList());
            RafraichirQualitePedagogique();
            _empreinteInitiale = CalculerEmpreinte();

            SelectionnerCommand = new RelayCommand(p =>
            {
                AfficherListeEtudiants = false;
                if (p is not QuestionExamen q) return;

                foreach (var item in Questions)
                    item.IsSelected = false;

                q.IsSelected = true;
                QuestionSelectionnee = q;
            });

            SupprimerCommand = new RelayCommand(p =>
            {
                if (QuestionsVerrouilleesParPublication) return;
                if (p is not QuestionExamen q) return;

                int idx = Questions.IndexOf(q);
                if (idx < 0) return;

                Questions.Remove(q);
                Renuméroter();
                OnPropertyChanged(nameof(NombreQuestions));
                OnPropertyChanged(nameof(SousTitreCompteur));
                ActualiserEtatBareme();
                _validerExamenCommand.RaiseCanExecuteChanged();

                if (Questions.Count > 0)
                    QuestionSelectionnee = Questions[Math.Min(idx, Questions.Count - 1)];
                else
                    QuestionSelectionnee = null;

                foreach (var item in Questions)
                    item.IsSelected = item == QuestionSelectionnee;
                if (QuestionSelectionnee != null)
                    QuestionSelectionnee.IsSelected = true;

                if (Questions.Count == 0 && _supprimerExamenPersisteAsync != null)
                    _ = SupprimerExamenVideEtFermerAsync();
            }, _ => !QuestionsVerrouilleesParPublication);

            AjouterQuestionCommand = new RelayCommand(p =>
            {
                if (QuestionsVerrouilleesParPublication) return;
                string type = p as string ?? "QCM";
                if (type != "QCM" && type != "VF" && type != "CHECKBOX" && type != "REDACTION")
                    type = "QCM";

                var q = new QuestionExamen
                {
                    Type = type,
                    Difficulte = Difficulte,
                    Enonce = "Nouvelle question",
                    BaremePoints = ExamenBaremeHelper.Pas
                };

                if (type == "QCM")
                {
                    q.ReponseCorrecte = string.Empty;
                }
                else if (type == "VF")
                {
                    q.OptionA = "Vrai";
                    q.OptionB = "Faux";
                    q.OptionC = string.Empty;
                    q.OptionD = string.Empty;
                    q.ReponseCorrecte = "A";
                }
                else if (type == "CHECKBOX")
                {
                    q.OptionACorrecte = false;
                    q.OptionBCorrecte = false;
                    q.OptionCCorrecte = false;
                    q.OptionDCorrecte = false;
                }
                else
                {
                    q.ReponseModele = string.Empty;
                }

                int idx = CalculerIndexInsertionParType(Questions, type);
                Questions.Insert(idx, q);
                Renuméroter();
                OnPropertyChanged(nameof(NombreQuestions));
                OnPropertyChanged(nameof(SousTitreCompteur));
                ActualiserEtatBareme();
                _validerExamenCommand.RaiseCanExecuteChanged();

                foreach (var item in Questions)
                    item.IsSelected = false;
                q.IsSelected = true;
                QuestionSelectionnee = q;
            }, _ => !QuestionsVerrouilleesParPublication);

            SetReponseCorrecteCommand = new RelayCommand(p =>
            {
                if (QuestionsVerrouilleesParPublication) return;
                if (p is not string lettre) return;
                if (QuestionSelectionnee == null) return;
                if (!QuestionSelectionnee.IsQCM && !QuestionSelectionnee.IsVF) return;

                QuestionSelectionnee.ReponseCorrecte = lettre.ToUpperInvariant();
                OnPropertyChanged(nameof(QuestionSelectionnee));
            }, _ => !QuestionsVerrouilleesParPublication);

            AttacherImageCommand = new RelayCommand(_ =>
            {
                if (QuestionsVerrouilleesParPublication) return;
                if (QuestionSelectionnee == null) return;

                var dlg = new OpenFileDialog
                {
                    Title = "Sélectionner une image",
                    Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp"
                };

                if (dlg.ShowDialog() != true) return;

                try
                {
                    byte[] bytes = File.ReadAllBytes(dlg.FileName);
                    string base64 = Convert.ToBase64String(bytes);
                    string ext = Path.GetExtension(dlg.FileName).TrimStart('.').ToLowerInvariant();

                    QuestionSelectionnee.ImageBase64 = base64;
                    QuestionSelectionnee.ImageType = ext;
                    QuestionSelectionnee.ImageNom = Path.GetFileName(dlg.FileName);

                    OnPropertyChanged(nameof(QuestionSelectionnee));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(UserErrorMessage.FromException(ex, "Impossible de lire cette image."),
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }, _ => !QuestionsVerrouilleesParPublication);

            SupprimerImageCommand = new RelayCommand(_ =>
            {
                if (QuestionsVerrouilleesParPublication) return;
                if (QuestionSelectionnee == null) return;
                QuestionSelectionnee.ImageBase64 = string.Empty;
                QuestionSelectionnee.ImageType = string.Empty;
                QuestionSelectionnee.ImageNom = string.Empty;
                OnPropertyChanged(nameof(QuestionSelectionnee));
            }, _ => !QuestionsVerrouilleesParPublication);

            _validerExamenCommand = new RelayCommand(
                _ =>
                {
                    if (Questions.Count == 0)
                    {
                        MessageBox.Show(
                            "L'examen ne contient aucune question.",
                            "Examen vide",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    if (!BaremeValide)
                    {
                        var msg = Questions.Any(q => !ExamenBaremeHelper.EstAuPas(q.BaremePoints))
                            ? "Les points doivent etre saisis par pas de 0,25."
                            : $"Le barème total doit être de 20 points (actuel: {ExamenBaremeHelper.FormatPoints(TotalBareme)}).";
                        MessageBox.Show(
                            msg,
                            "Barème  invalide",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    int nbEmails = CompterEmailsValides(TexteEmailsWeb);
                    string ligneEmails =
                        $"• Publication web : {nbEmails} email(s) autorisé(s) (max {QuizPublicationLimits.MaxAuthorizedStudentEmails})\n";
                    string ligneCreneau = $"• Créneau : {LibelleCreneauResume}\n";

                    MessageBoxResult res;
                    if (IsEditionExistant)
                    {
                        string verrou = QuestionsVerrouilleesParPublication
                            ? "\nLes questions ne seront pas modifiées (examen publié). Les emails et le créneau seront mis à jour en base si vous les avez changés.\n"
                            : string.Empty;
                        res = MessageBox.Show(
                            $"Enregistrer les modifications de l'examen « {TitreExamen} » ?\n\n" +
                            $"• {Questions.Count} questions\n" +
                            ligneEmails +
                            ligneCreneau +
                            $"• Difficulté : {Difficulte}\n" +
                            $"• Durée : {Duree} min\n" +
                            verrou +
                            "\nLes données en base seront mises à jour.",
                            "Enregistrer les modifications",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                    }
                    else
                    {
                        res = MessageBox.Show(
                            $"Valider et sauvegarder l'examen « {TitreExamen} » ?\n\n" +
                            $"• {Questions.Count} questions\n" +
                            ligneEmails +
                            ligneCreneau +
                            $"• Difficulté : {Difficulte}\n" +
                            $"• Durée : {Duree} min\n\n" +
                            "L'examen sera enregistré localement. Vous pourrez publier sur le web depuis la liste des examens.",
                            "Valider et sauvegarder",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                    }

                    if (res != MessageBoxResult.Yes) return;

                    ExamenValide?.Invoke(
                        Questions.ToList(),
                        TitreExamen,
                        Duree,
                        Difficulte,
                        CoursSourceLabel,
                        SerialiserEmailsPourBase(TexteEmailsWeb),
                        ObtenirDatePassageLocal());
                },
                _ => Questions.Count > 0);
            ValiderExamenCommand = _validerExamenCommand;

            Questions.CollectionChanged += (_, __) =>
            {
                RebrancherEvenementsQuestions();
                ActualiserEtatBareme();
                RafraichirQualitePedagogique();
                _validerExamenCommand.RaiseCanExecuteChanged();
            };
            RebrancherEvenementsQuestions();
            ActualiserEtatBareme();

            RegenerarCommand = new RelayCommand(_ =>
            {
                var res = MessageBox.Show(
                    "Regénérer l'examen ?\nLes questions actuelles seront perdues.",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                    NavigationRegenerarRequested?.Invoke();
            }, _ => !IsEditionExistant);

            RetourCommand = new RelayCommand(_ =>
            {
                if (!IsEditionExistant)
                {
                    var resNouveau = MessageBox.Show(
                        "Quitter sans sauvegarder ?\n" +
                        "L'examen n'est pas enregistré dans la base locale. Vous perdrez le contenu généré.",
                        "Quitter sans sauvegarder",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if (resNouveau == MessageBoxResult.Yes)
                        NavigationRetourRequested?.Invoke();
                    return;
                }

                if (!ADesModificationsDepuisOuverture())
                {
                    NavigationRetourRequested?.Invoke();
                    return;
                }

                var res = MessageBox.Show(
                    "Quitter sans enregistrer les modifications ?\nLes changements seront perdus.",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                    NavigationRetourRequested?.Invoke();
            });
        }

        private void NotifierVuesPanneauDroit()
        {
            OnPropertyChanged(nameof(AfficherDetailQuestion));
            OnPropertyChanged(nameof(AfficherEtatSansQuestion));
            OnPropertyChanged(nameof(AfficherListeEtudiantsDroite));
            if (VoirListeEtudiantsCommand is RelayCommand vl)
                vl.RaiseCanExecuteChanged();
        }

        private void RafraichirEmailsListeApercu()
        {
            EmailsListeApercu.Clear();
            foreach (var e in ExtraireEmailsValides(TexteEmailsWeb))
            {
                EmailsListeApercu.Add(new PublicationWebEmailRowViewModel(e, () =>
                {
                    if (SupprimerEmailsSelectionnesWebCommand is RelayCommand cmd)
                        cmd.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(AEmailsSelectionnes));
                },
                SynchroniserTexteEmailsDepuisListeApercu));
            }

            OnPropertyChanged(nameof(AucunEmailListeApercu));
            OnPropertyChanged(nameof(AEmailsSelectionnes));
            if (SupprimerEmailsSelectionnesWebCommand is RelayCommand rs)
                rs.RaiseCanExecuteChanged();
        }

        private void SynchroniserTexteEmailsDepuisListeApercu()
        {
            var normalises = ExtraireEmailsValides(string.Join(
                Environment.NewLine,
                EmailsListeApercu.Select(r => r.Email)));

            TexteEmailsWeb = string.Join(Environment.NewLine, normalises);
        }

        private DateTime? ObtenirDatePassageLocal()
        {
            if (!TryParseHeure(out int h, out int mi))
                return null;
            try
            {
                return new DateTime(DateExamen.Year, DateExamen.Month, DateExamen.Day, h, mi, 0, DateTimeKind.Local);
            }
            catch
            {
                return null;
            }
        }

        private bool TryParseHeure(out int h, out int m)
        {
            h = 0;
            m = 0;
            return int.TryParse(HeureSelectionnee, NumberStyles.Integer, CultureInfo.InvariantCulture, out h)
                   && int.TryParse(MinuteSelectionnee, NumberStyles.Integer, CultureInfo.InvariantCulture, out m)
                   && h >= 0 && h <= 23 && m >= 0 && m <= 59;
        }

        private static string InitialiserTexteEmailsDepuisJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return string.Empty;
            try
            {
                var arr = JsonSerializer.Deserialize<List<string>>(json.Trim());
                if (arr == null || arr.Count == 0) return string.Empty;
                return string.Join(
                    Environment.NewLine,
                    arr.Where(static e => !string.IsNullOrWhiteSpace(e))
                        .Select(e => e.Trim().ToLowerInvariant()));
            }
            catch
            {
                return string.Empty;
            }
        }

        private static List<string> ExtraireEmailsValides(string texte)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(texte)) return new List<string>();

            foreach (var rawLine in texte.Replace("\r\n", "\n").Split('\n'))
            {
                foreach (var part in rawLine.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var e = part.Trim().ToLowerInvariant();
                    if (!ImportEtudiantsService.EstEmail(e) || !set.Add(e))
                        continue;
                    if (set.Count >= QuizPublicationLimits.MaxAuthorizedStudentEmails)
                        return set.ToList();
                }
            }

            return set.ToList();
        }

        private static int CompterEmailsValides(string texte) => ExtraireEmailsValides(texte).Count;

        private static string SerialiserEmailsPourBase(string texte) =>
            JsonSerializer.Serialize(ExtraireEmailsValides(texte));

        private void SupprimerEmailsSelectionnesWeb()
        {
            var aSupprimer = EmailsListeApercu
                .Where(r => r.IsSelected)
                .Select(r => r.Email)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (aSupprimer.Count == 0)
                return;

            var garder = ExtraireEmailsValides(TexteEmailsWeb)
                .Where(e => !aSupprimer.Contains(e))
                .ToList();
            TexteEmailsWeb = string.Join(Environment.NewLine, garder);
        }

        private void AjouterEmailUnitaireWeb()
        {
            var raw = NouvelEmailWeb?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(raw))
                return;

            var email = raw.ToLowerInvariant();
            if (!ImportEtudiantsService.EstEmail(email))
            {
                MessageBox.Show(
                    "Adresse email invalide.",
                    "Publication web",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var existants = ExtraireEmailsValides(TexteEmailsWeb);
            if (existants.Any(e => string.Equals(e, email, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show(
                    "Cet email est déjà dans la liste.",
                    "Publication web",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            int max = QuizPublicationLimits.MaxAuthorizedStudentEmails;
            if (existants.Count >= max)
            {
                MessageBox.Show(
                    $"Le nombre maximum d'emails autorisés est {max}.",
                    "Publication web",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            TexteEmailsWeb = string.IsNullOrWhiteSpace(TexteEmailsWeb)
                ? email
                : TexteEmailsWeb.TrimEnd() + Environment.NewLine + email;
            NouvelEmailWeb = string.Empty;
        }

        private void ImporterEmailsWebDepuisFichier()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "CSV ou Excel|*.csv;*.xlsx|CSV (*.csv)|*.csv|Excel (*.xlsx)|*.xlsx",
                Title = "Importer les emails (publication web)"
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                var detail = ImportEtudiantsService.ImporterDepuisFichierDetaille(dlg.FileName);
                var liste = detail.EmailsValides;
                if (liste.Count == 0)
                {
                    MessageBox.Show(
                        "Aucun email valide dans le fichier.",
                        "Import",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                int max = QuizPublicationLimits.MaxAuthorizedStudentEmails;
                if (liste.Count > max)
                {
                    MessageBox.Show(
                        $"Le fichier contient plus de {max} emails. Seuls les {max} premiers sont conservés.",
                        "Import",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    liste = liste.Take(max).ToList();
                }

                TexteEmailsWeb = string.Join(Environment.NewLine, liste);
            }
            catch (NotSupportedException)
            {
                MessageBox.Show(
                    "Format non supporté. Utilisez un fichier .csv ou .xlsx.",
                    "Import",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (IOException ex)
            {
                MessageBox.Show(
                    $"Lecture du fichier impossible : {ex.Message}",
                    "Import",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Import impossible : {ex.Message}",
                    "Import",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private string CalculerEmpreinte()
        {
            var qs = Questions.Select(q => new
            {
                q.Numero,
                q.Type,
                q.Enonce,
                q.Difficulte,
                q.OptionA,
                q.OptionB,
                q.OptionC,
                q.OptionD,
                q.ReponseCorrecte,
                q.OptionACorrecte,
                q.OptionBCorrecte,
                q.OptionCCorrecte,
                q.OptionDCorrecte,
                q.ReponseModele,
                q.BaremePoints,
                q.Explication,
                ImgLen = q.ImageBase64?.Length ?? 0,
                q.ImageNom,
                q.ImageType
            }).ToList();

            string creneau =
                ObtenirDatePassageLocal()?.ToString("O", CultureInfo.InvariantCulture) ?? "";
            return JsonSerializer.Serialize(new
            {
                TitreExamen,
                Duree = _dureeMinutes,
                creneau,
                Emails = SerialiserEmailsPourBase(TexteEmailsWeb),
                questions = qs
            });
        }

        private bool ADesModificationsDepuisOuverture() =>
            CalculerEmpreinte() != _empreinteInitiale;

        private void Renuméroter()
        {
            int n = 1;
            foreach (var q in Questions)
                q.Numero = n++;
        }

        private void RebrancherEvenementsQuestions()
        {
            foreach (var q in Questions)
            {
                q.PropertyChanged -= Question_PropertyChanged;
                q.PropertyChanged += Question_PropertyChanged;
            }
        }

        private void Question_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(QuestionExamen.BaremePoints))
            {
                ActualiserEtatBareme();
                _validerExamenCommand.RaiseCanExecuteChanged();
            }
            if (e.PropertyName is nameof(QuestionExamen.Enonce)
                or nameof(QuestionExamen.OptionA)
                or nameof(QuestionExamen.OptionB)
                or nameof(QuestionExamen.OptionC)
                or nameof(QuestionExamen.OptionD)
                or nameof(QuestionExamen.ReponseCorrecte)
                or nameof(QuestionExamen.ReponseModele)
                or nameof(QuestionExamen.Explication))
            {
                RafraichirQualitePedagogique();
            }
        }

        private void ActualiserEtatBareme()
        {
            OnPropertyChanged(nameof(TotalBareme));
            OnPropertyChanged(nameof(EcartBareme));
            OnPropertyChanged(nameof(BaremeValide));
            OnPropertyChanged(nameof(ResumeBareme));
        }

        private void RafraichirQualitePedagogique()
        {
            foreach (var q in Questions)
            {
                _ = PedagogicalQualityEvaluator.EvaluateExamQuestion(q);
            }
        }

        /// <summary>
        /// Ordre d'affichage aligné sur la génération : QCM, puis CHECKBOX, puis RÉDACTION, puis IMAGE.
        /// La nouvelle question est insérée dans le bloc de son type (après les questions du même type déjà présentes).
        /// </summary>
        private static int TypeOrder(string t) => t switch
        {
            "QCM" => 0,
            "VF" => 1,
            "CHECKBOX" => 2,
            "REDACTION" => 3,
            "IMAGE" => 4,
            _ => 0
        };

        private static int CalculerIndexInsertionParType(ObservableCollection<QuestionExamen> questions, string type)
        {
            int want = TypeOrder(type);
            int insertAt = 0;
            for (int i = 0; i < questions.Count; i++)
            {
                int o = TypeOrder(questions[i].Type);
                if (o > want)
                    return i;
                insertAt = i + 1;
            }

            return insertAt;
        }

        private async Task SupprimerExamenVideEtFermerAsync()
        {
            try
            {
                if (_supprimerExamenPersisteAsync != null)
                    await _supprimerExamenPersisteAsync();
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    NavigationRetourRequested?.Invoke());
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    MessageBox.Show(
                        UserErrorMessage.FromException(ex, "Impossible de supprimer cet examen pour le moment."),
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error));
            }
        }
    }
}
