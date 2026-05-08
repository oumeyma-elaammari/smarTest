using smartest_desktop.Constants;
using smartest_desktop.Helpers;
using smartest_desktop.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace smartest_desktop.ViewModels
{
    // ═══════════════════════════════════════════════════════════════════════════
    // QuizResultViewModel
    //
    // PROCESSUS (étape 3) :
    //   - Affiche toutes les questions générées par Ollama
    //   - Le prof peut : sélectionner, modifier, supprimer chaque QCM
    //   - Valider = sauvegarder en base locale (statut "Validé", prêt à publier)
    //   - Regénérer = retourner à la configuration
    // ═══════════════════════════════════════════════════════════════════════════
    public class QuizResultViewModel : BaseViewModel
    {
        private const string TitreImport = "Import";

        // ── Données du quiz ───────────────────────────────────────────────────
        private string _titreQuiz = string.Empty;
        public string TitreQuiz
        {
            get => _titreQuiz;
            set => SetProperty(ref _titreQuiz, value);
        }

        public string Difficulte { get; }
        public string CoursSourceTitre { get; }

        public ObservableCollection<QuestionQCM> Questions { get; } = new();

        public int NombreQuestions => Questions.Count;

        // ── Emails publication web (édition manuelle + import fichier) ────────

        private string _texteEmailsWeb = string.Empty;

        /// <summary>Une adresse par ligne ; virgules acceptées. Import CSV/Excel remplace le contenu.</summary>
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

        /// <summary>Champ une ligne pour ajouter une adresse à la liste (bouton Ajouter).</summary>
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

        public ICommand ImporterEmailsWebCommand { get; private set; }
        public ICommand EffacerEmailsWebCommand { get; private set; }
        public ICommand AjouterEmailUnitaireWebCommand { get; private set; }
        public ICommand SupprimerEmailsSelectionnesWebCommand { get; private set; }

        /// <summary>Pan droit : aperçu liste emails au lieu du détail QCM.</summary>
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

        /// <summary>Détail question visible (pan droit).</summary>
        public bool AfficherDetailQuestion => HasQuestionSelectionnee && !_afficherListeEtudiants;

        /// <summary>Placeholder « sélectionnez une question » (pan droit).</summary>
        public bool AfficherEtatSansQuestion => !_afficherListeEtudiants && !HasQuestionSelectionnee;

        /// <summary>Liste étudiants en lecture seule (pan droit).</summary>
        public bool AfficherListeEtudiantsDroite => _afficherListeEtudiants;

        public ObservableCollection<PublicationWebEmailRowViewModel> EmailsListeApercu { get; } = new();

        public bool AucunEmailListeApercu => EmailsListeApercu.Count == 0;
        public bool AEmailsSelectionnes => EmailsListeApercu.Any(r => r.IsSelected);

        public ICommand VoirListeEtudiantsCommand { get; private set; }

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

        // ── Question sélectionnée (panneau de droite) ─────────────────────────
        private QuestionQCM? _questionSelectionnee;
        public QuestionQCM? QuestionSelectionnee
        {
            get => _questionSelectionnee;
            set
            {
                // Désélectionner l'ancienne
                if (_questionSelectionnee != null)
                    _questionSelectionnee.IsSelected = false;

                SetProperty(ref _questionSelectionnee, value);

                // Sélectionner la nouvelle
                if (_questionSelectionnee != null)
                    _questionSelectionnee.IsSelected = true;

                OnPropertyChanged(nameof(HasQuestionSelectionnee));
                OnPropertyChanged(nameof(HasNoQuestionSelectionnee));
                NotifierVuesPanneauDroit();
            }
        }

        public bool HasQuestionSelectionnee => QuestionSelectionnee != null;
        public bool HasNoQuestionSelectionnee => QuestionSelectionnee == null;

        private readonly string _empreinteInitiale;

        private readonly Func<Task>? _supprimerQuizPersisteAsync;

        // ── Commandes ─────────────────────────────────────────────────────────
        public ICommand SelectionnerQuestionCommand { get; private set; }
        public ICommand SupprimerQuestionCommand { get; private set; }
        public ICommand AjouterQuestionCommand { get; private set; }
        /// <summary>Paramètre : A, B, C ou D — définit la réponse correcte du QCM sélectionné.</summary>
        public ICommand SetReponseCorrecteCommand { get; private set; }
        public ICommand ValiderQuizCommand { get; private set; }

        private RelayCommand _validerQuizCommandRelay = null!;
        private RelayCommand _supprimerQuestionCommandRelay = null!;
        private RelayCommand _ajouterQuestionCommandRelay = null!;
        private RelayCommand _setReponseCorrecteCommandRelay = null!;
        public ICommand RegenerarCommand { get; private set; }
        public ICommand RetourCommand { get; private set; }

        // ── Événements de navigation ──────────────────────────────────────────
        /// <summary>Dernier argument : JSON du tableau d'emails (publication web), ex. ["a@b.com"].</summary>
        public event Action<ObservableCollection<QuestionQCM>, string, string, string, string, string>? QuizValide;
        public event Action? NavigationRegenerarRequested;
        public event Action? NavigationRetourRequested;

        // ── Constructeur ──────────────────────────────────────────────────────


        // ── Helpers privés ────────────────────────────────────────────────────
        private void RenuméroterQuestions()
        {
            int n = 1;
            foreach (var q in Questions)
                q.Numero = n++;
            OnPropertyChanged(nameof(SousTitreQuestionsListe));
        }

        private string CalculerEmpreinte()
        {
            var payload = Questions.Select(q => new
            {
                q.Numero,
                q.Enonce,
                q.OptionA,
                q.OptionB,
                q.OptionC,
                q.OptionD,
                q.ReponseCorrecte,
                q.Explication
            }).ToList();
            return JsonSerializer.Serialize(new
            {
                TitreQuiz = TitreQuiz?.Trim(),
                Questions = payload,
                Emails = SerialiserEmailsPourBase(TexteEmailsWeb)
            });
        }

        private bool ADesModificationsDepuisOuverture() =>
            CalculerEmpreinte() != _empreinteInitiale;

        public string Statut { get; }

        /// <summary>Vrai si le quiz est publié sur le web : les QCM ne sont plus modifiables (emails publication web restent éditables).</summary>
        public bool QuestionsVerrouilleesParPublication =>
            string.Equals(Statut?.Trim(), "Publié", StringComparison.OrdinalIgnoreCase);

        /// <summary>Id en base si le quiz est ouvert depuis la liste ; sinon première génération.</summary>
        public int? QuizIdExistant { get; }

        public bool IsEditionQuizExistant => QuizIdExistant.HasValue;

        public string TitreFenetre =>
            IsEditionQuizExistant ? "SmarTest — Modifier le quiz" : "SmarTest — Quiz généré";

        public string LibelleBoutonValider
        {
            get
            {
                if (IsValidationEnCours)
                {
                    return IsEditionQuizExistant ? "Enregistrement en cours..." : "Validation en cours...";
                }

                return IsEditionQuizExistant ? "Enregistrer les modifications" : "Valider et sauvegarder";
            }
        }

        private bool _isValidationEnCours;
        public bool IsValidationEnCours
        {
            get => _isValidationEnCours;
            private set
            {
                if (!SetProperty(ref _isValidationEnCours, value))
                    return;
                OnPropertyChanged(nameof(LibelleBoutonValider));
                _validerQuizCommandRelay.RaiseCanExecuteChanged();
            }
        }

        public string SousTitreEtape
        {
            get
            {
                if (QuestionsVerrouilleesParPublication)
                {
                    return "Questions en lecture seule · publication web modifiable";
                }

                return IsEditionQuizExistant
                    ? "Modifiez puis enregistrez dans la base locale"
                    : "Vérifiez et ajustez avant validation";
            }
        }

        public string SousTitreCompteur => IsEditionQuizExistant
            ? $"{NombreQuestions} questions"
            : $"{NombreQuestions} questions générées";

        public string SousTitreQuestionsListe =>
            QuestionsVerrouilleesParPublication
                ? $"{NombreQuestions} questions · Lecture seule (quiz publié)"
                : $"{NombreQuestions} questions · Modifiables directement";

        public string SousTitreSelectionQuestion =>
            QuestionsVerrouilleesParPublication
                ? "Cliquez sur une question dans la liste pour la consulter (non modifiable)."
                : "Cliquez sur une question dans la liste pour la voir et l'ajuster";

        public string MessagePiedPage =>
            QuestionsVerrouilleesParPublication
                ? "Quiz publié : les questions ne sont plus modifiables. Vous pouvez encore ajuster la liste des emails autorisés sur le web."
                : "Cliquez sur une question pour l'éditer directement ou la supprimer";

        public sealed class QuizResultViewModelInit
        {
            public string Titre { get; init; } = string.Empty;
            public string Difficulte { get; init; } = string.Empty;
            public string CoursTitre { get; init; } = string.Empty;
            public string Statut { get; init; } = string.Empty;
            public int? QuizIdExistant { get; init; }
            public Func<Task>? SupprimerQuizPersisteAsync { get; init; }
            public string? EmailsPublicationWebJsonInit { get; init; }
        }

        public QuizResultViewModel(
            List<QuestionQCM> questions,
            QuizResultViewModelInit init)
        {
            QuizIdExistant = init.QuizIdExistant;
            _supprimerQuizPersisteAsync = init.SupprimerQuizPersisteAsync;
            // Initialisation des propriétés
            TitreQuiz = init.Titre;
            Difficulte = init.Difficulte;
            CoursSourceTitre = init.CoursTitre;
            Statut = init.Statut;

            TexteEmailsWeb = InitialiserTexteEmailsDepuisJson(init.EmailsPublicationWebJsonInit);

            InitialiserCommandesEmails();
            VoirListeEtudiantsCommand = new RelayCommand(_ => AfficherListeEtudiants = true);
            InitialiserQuestions(questions);
            RafraichirQualitePedagogique();

            _empreinteInitiale = CalculerEmpreinte();

            InitialiserCommandesQuestionsEtValidation();
            InitialiserCommandesNavigation();
            Questions.CollectionChanged += (_, __) =>
            {
                RebrancherEvenementsQuestions();
                RafraichirQualitePedagogique();
                _validerQuizCommandRelay.RaiseCanExecuteChanged();
                _supprimerQuestionCommandRelay.RaiseCanExecuteChanged();
                _ajouterQuestionCommandRelay.RaiseCanExecuteChanged();
                _setReponseCorrecteCommandRelay.RaiseCanExecuteChanged();
            };
            RebrancherEvenementsQuestions();
        }

        private void InitialiserCommandesEmails()
        {
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
        }

        private void InitialiserQuestions(List<QuestionQCM> questions)
        {
            int n = 1;
            foreach (var q in questions)
            {
                q.Numero = n++;
                Questions.Add(q);
            }

            if (Questions.Count > 0)
                QuestionSelectionnee = Questions[0];
        }

        private void RebrancherEvenementsQuestions()
        {
            foreach (var q in Questions)
            {
                q.PropertyChanged -= Question_PropertyChanged;
                q.PropertyChanged += Question_PropertyChanged;
            }
        }

        private void Question_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(QuestionQCM.Enonce)
                or nameof(QuestionQCM.OptionA)
                or nameof(QuestionQCM.OptionB)
                or nameof(QuestionQCM.OptionC)
                or nameof(QuestionQCM.OptionD)
                or nameof(QuestionQCM.ReponseCorrecte)
                or nameof(QuestionQCM.Explication))
            {
                RafraichirQualitePedagogique();
            }
        }

        private void RafraichirQualitePedagogique()
        {
            foreach (var q in Questions)
            {
                _ = PedagogicalQualityEvaluator.EvaluateQuizQuestion(q);
            }
        }

        private void InitialiserCommandesQuestionsEtValidation()
        {
            InitialiserSelectionQuestionCommande();
            InitialiserSuppressionQuestionCommande();
            InitialiserAjoutQuestionCommande();
            InitialiserSetReponseCommande();
            InitialiserValidationCommande();
        }

        private void InitialiserSelectionQuestionCommande()
        {
            SelectionnerQuestionCommand = new RelayCommand(param =>
            {
                AfficherListeEtudiants = false;
                if (param is QuestionQCM q)
                    QuestionSelectionnee = q;
            });
        }

        private void InitialiserSuppressionQuestionCommande()
        {
            _supprimerQuestionCommandRelay = new RelayCommand(
                param => ExecuterSuppressionQuestion(param),
                _ => !QuestionsVerrouilleesParPublication);
            SupprimerQuestionCommand = _supprimerQuestionCommandRelay;
        }

        private void ExecuterSuppressionQuestion(object? param)
        {
            if (QuestionsVerrouilleesParPublication) return;
            if (param is not QuestionQCM q) return;

            int idx = Questions.IndexOf(q);
            if (idx < 0) return;

            Questions.Remove(q);
            RenuméroterQuestions();
            OnPropertyChanged(nameof(NombreQuestions));
            OnPropertyChanged(nameof(SousTitreCompteur));

            if (Questions.Count > 0)
                QuestionSelectionnee = Questions[Math.Min(idx, Questions.Count - 1)];
            else
                QuestionSelectionnee = null;

            if (Questions.Count == 0 && _supprimerQuizPersisteAsync != null)
                _ = SupprimerQuizVideEtFermerAsync();
        }

        private void InitialiserAjoutQuestionCommande()
        {
            _ajouterQuestionCommandRelay = new RelayCommand(
                _ =>
                {
                    if (QuestionsVerrouilleesParPublication) return;
                    var q = new QuestionQCM
                    {
                        Enonce = "Nouvelle question",
                        ReponseCorrecte = string.Empty,
                    };
                    Questions.Add(q);
                    RenuméroterQuestions();
                    OnPropertyChanged(nameof(NombreQuestions));
                    OnPropertyChanged(nameof(SousTitreCompteur));
                    QuestionSelectionnee = q;
                },
                _ => !QuestionsVerrouilleesParPublication);
            AjouterQuestionCommand = _ajouterQuestionCommandRelay;
        }

        private void InitialiserSetReponseCommande()
        {
            _setReponseCorrecteCommandRelay = new RelayCommand(
                p =>
                {
                    if (QuestionsVerrouilleesParPublication) return;
                    if (p is not string lettre) return;
                    if (QuestionSelectionnee == null) return;
                    QuestionSelectionnee.ReponseCorrecte = lettre.Trim().ToUpperInvariant();
                    OnPropertyChanged(nameof(QuestionSelectionnee));
                },
                _ => !QuestionsVerrouilleesParPublication);
            SetReponseCorrecteCommand = _setReponseCorrecteCommandRelay;
        }

        private void InitialiserValidationCommande()
        {
            _validerQuizCommandRelay = new RelayCommand(
                _ => ExecuterValidationQuiz(),
                _ => Questions.Count > 0 && !IsValidationEnCours);
            ValiderQuizCommand = _validerQuizCommandRelay;
        }

        private void ExecuterValidationQuiz()
        {
            if (IsValidationEnCours)
                return;
            if (Questions.Count == 0)
            {
                MessageBox.Show(
                    "Le quiz ne contient aucune question.",
                    "Quiz vide",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            MessageBoxResult res;
            int nbEmails = CompterEmailsValides(TexteEmailsWeb);
            string ligneEmails =
                $"• Publication web : {nbEmails} email(s) autorisé(s) (max {QuizPublicationLimits.MaxAuthorizedStudentEmails})\n";

            if (IsEditionQuizExistant)
            {
                string verrou = QuestionsVerrouilleesParPublication
                    ? "\nLes questions ne seront pas modifiées (quiz publié). Seules la liste d'emails publication web et les données associées en base seront mises à jour si vous les avez changées.\n"
                    : string.Empty;
                res = MessageBox.Show(
                    $"Enregistrer les modifications du quiz « {TitreQuiz} » ?\n\n" +
                    $"• {Questions.Count} questions\n" +
                    ligneEmails +
                    $"• Difficulté : {Difficulte}\n" +
                    $"• Statut : {Statut}\n" +
                    verrou +
                    "\nLes données en base seront mises à jour.",
                    "Enregistrer les modifications",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
            }
            else
            {
                res = MessageBox.Show(
                    $"Valider et sauvegarder le quiz « {TitreQuiz} » ?\n\n" +
                    $"• {Questions.Count} questions\n" +
                    ligneEmails +
                    $"• Difficulté : {Difficulte}\n" +
                    $"• Statut : {Statut}\n\n" +
                    "Le quiz sera enregistré localement et prêt à être publié.",
                    "Valider et sauvegarder",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
            }

            if (res != MessageBoxResult.Yes) return;

            IsValidationEnCours = true;
            QuizValide?.Invoke(
                Questions,
                TitreQuiz,
                Difficulte,
                CoursSourceTitre,
                Statut,
                SerialiserEmailsPourBase(TexteEmailsWeb));
        }

        private void InitialiserCommandesNavigation()
        {
            RegenerarCommand = new RelayCommand(_ =>
            {
                var res = MessageBox.Show(
                    "Regénérer le quiz ?\nLes questions actuelles seront perdues.",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                    NavigationRegenerarRequested?.Invoke();
            });

            // Commande : retour
            RetourCommand = new RelayCommand(_ =>
            {
                // Pas encore enregistré en base : toujours confirmer (même sans modification d'empreinte)
                if (!IsEditionQuizExistant)
                {
                    var resNouveau = MessageBox.Show(
                        "Quitter sans sauvegarder ?\n" +
                        "Le quiz n'est pas enregistré dans la base locale. Vous perdrez le contenu généré.",
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

        private async Task SupprimerQuizVideEtFermerAsync()
        {
            try
            {
                if (_supprimerQuizPersisteAsync != null)
                    await _supprimerQuizPersisteAsync();
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    NavigationRetourRequested?.Invoke());
            }
            catch (System.Exception ex)
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    MessageBox.Show(
                        UserErrorMessage.FromException(ex, "Impossible de supprimer ce quiz pour le moment."),
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error));
            }
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
            OnPropertyChanged(nameof(AEmailsSelectionnes));
        }

        public void MarquerValidationTerminee()
        {
            IsValidationEnCours = false;
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
                        TitreImport,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                int max = QuizPublicationLimits.MaxAuthorizedStudentEmails;
                if (liste.Count > max)
                {
                    MessageBox.Show(
                        $"Le fichier contient plus de {max} emails. Seuls les {max} premiers sont conservés.",
                        TitreImport,
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
                    TitreImport,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (IOException ex)
            {
                MessageBox.Show(
                    UserErrorMessage.FromException(ex, "Lecture du fichier impossible. Verifiez qu'il n'est pas deja ouvert."),
                    TitreImport,
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    UserErrorMessage.FromException(ex, "Import impossible. Verifiez le format du fichier puis reessayez."),
                    TitreImport,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}