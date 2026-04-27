using smartest_desktop.Helpers;
using System.Collections.ObjectModel;
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

        // ── Question sélectionnée (panneau de droite) ─────────────────────────
        private QuestionQCM? _questionSelectionnee;
        public QuestionQCM? QuestionSelectionnee
        {
            get => _questionSelectionnee;
            set
            {
                // Si on change de question alors qu'une est en cours d'édition → annuler
                if (_questionSelectionnee?.IsEditing == true)
                    AnnulerEditionInterne();

                // Désélectionner l'ancienne
                if (_questionSelectionnee != null)
                    _questionSelectionnee.IsSelected = false;

                SetProperty(ref _questionSelectionnee, value);

                // Sélectionner la nouvelle
                if (_questionSelectionnee != null)
                    _questionSelectionnee.IsSelected = true;

                OnPropertyChanged(nameof(HasQuestionSelectionnee));
                OnPropertyChanged(nameof(HasNoQuestionSelectionnee));
                RefreshEditionCommands();
            }
        }

        public bool HasQuestionSelectionnee => QuestionSelectionnee != null;
        public bool HasNoQuestionSelectionnee => QuestionSelectionnee == null;

        // ── Messages ──────────────────────────────────────────────────────────
        private string _successMessage = string.Empty;
        public string SuccessMessage
        {
            get => _successMessage;
            set { SetProperty(ref _successMessage, value); OnPropertyChanged(nameof(HasSuccess)); }
        }

        public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);

        // ── Sauvegarde pour annulation d'édition ──────────────────────────────
        private QuestionQCM? _backup;

        // ── Commandes ─────────────────────────────────────────────────────────
        public ICommand SelectionnerQuestionCommand { get; }
        public ICommand EditerCommand { get; }
        public ICommand SauvegarderEditionCommand { get; }
        public ICommand AnnulerEditionCommand { get; }
        public ICommand SupprimerQuestionCommand { get; }
        public ICommand ValiderQuizCommand { get; }
        public ICommand RegenerarCommand { get; }
        public ICommand RetourCommand { get; }

        // ── Événements de navigation ──────────────────────────────────────────
        /// <summary>Quiz validé → paramètres : questions, titre, difficulté, coursTitre</summary>
        public event Action<ObservableCollection<QuestionQCM>, string, string, string, string>? QuizValide;
        public event Action? NavigationRegenerarRequested;
        public event Action? NavigationRetourRequested;

        // ── Constructeur ──────────────────────────────────────────────────────
      

        // ── Helpers privés ────────────────────────────────────────────────────
        private void AnnulerEditionInterne()
        {
            if (QuestionSelectionnee == null || _backup == null) return;

            QuestionSelectionnee.Enonce = _backup.Enonce;
            QuestionSelectionnee.OptionA = _backup.OptionA;
            QuestionSelectionnee.OptionB = _backup.OptionB;
            QuestionSelectionnee.OptionC = _backup.OptionC;
            QuestionSelectionnee.OptionD = _backup.OptionD;
            QuestionSelectionnee.ReponseCorrecte = _backup.ReponseCorrecte;
            QuestionSelectionnee.Explication = _backup.Explication;
            QuestionSelectionnee.IsEditing = false;
            _backup = null;
            RefreshEditionCommands();
        }

        private void RefreshEditionCommands()
        {
            ((RelayCommand)EditerCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SauvegarderEditionCommand).RaiseCanExecuteChanged();
            ((RelayCommand)AnnulerEditionCommand).RaiseCanExecuteChanged();
        }

        private void RenuméroterQuestions()
        {
            int n = 1;
            foreach (var q in Questions)
                q.Numero = n++;
        }

        private void AfficherSucces(string message)
        {
            SuccessMessage = message;
            Task.Delay(2500).ContinueWith(_ =>
                System.Windows.Application.Current.Dispatcher.Invoke(
                    () => SuccessMessage = string.Empty));
        }

        private static QuestionQCM CloneQuestion(QuestionQCM src) => new()
        {
            Numero = src.Numero,
            Enonce = src.Enonce,
            OptionA = src.OptionA,
            OptionB = src.OptionB,
            OptionC = src.OptionC,
            OptionD = src.OptionD,
            ReponseCorrecte = src.ReponseCorrecte,
            Explication = src.Explication
        };

        private static string TronquerTexte(string texte, int max) =>
            texte.Length <= max ? texte : texte[..max] + "…";


        public string Statut { get; }



        public QuizResultViewModel(
    List<QuestionQCM> questions,
    string titre,
    string difficulte,
    string coursTitre,
    string statut)
        {
            // Initialisation des propriétés
            TitreQuiz = titre;
            Difficulte = difficulte;
            CoursSourceTitre = coursTitre;
            Statut = statut;

            // Numérotation et ajout des questions
            int n = 1;
            foreach (var q in questions)
            {
                q.Numero = n++;
                Questions.Add(q);
            }

            // Commande : sélectionner une question
            SelectionnerQuestionCommand = new RelayCommand(param =>
            {
                if (param is QuestionQCM q)
                    QuestionSelectionnee = q;
            });

            // Commande : passer en mode édition
            EditerCommand = new RelayCommand(
                _ =>
                {
                    if (QuestionSelectionnee == null) return;
                    _backup = CloneQuestion(QuestionSelectionnee);
                    QuestionSelectionnee.IsEditing = true;
                    RefreshEditionCommands();
                },
                _ => QuestionSelectionnee != null && QuestionSelectionnee.IsNotEditing);

            // Commande : sauvegarder les modifications
            SauvegarderEditionCommand = new RelayCommand(
                _ =>
                {
                    if (QuestionSelectionnee == null) return;
                    QuestionSelectionnee.IsEditing = false;
                    _backup = null;
                    RefreshEditionCommands();
                    AfficherSucces("✅ Question modifiée avec succès");
                },
                _ => QuestionSelectionnee?.IsEditing == true);

            // Commande : annuler l’édition
            AnnulerEditionCommand = new RelayCommand(
                _ => AnnulerEditionInterne(),
                _ => QuestionSelectionnee?.IsEditing == true);

            // Commande : supprimer une question
            SupprimerQuestionCommand = new RelayCommand(param =>
            {
                if (param is not QuestionQCM q) return;

                var res = MessageBox.Show(
                    $"Supprimer la question {q.Numero} ?\n\n\"{TronquerTexte(q.Enonce, 80)}\"",
                    "Supprimer la question",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (res != MessageBoxResult.Yes) return;

                if (QuestionSelectionnee == q)
                    QuestionSelectionnee = null;

                Questions.Remove(q);
                RenuméroterQuestions();
                OnPropertyChanged(nameof(NombreQuestions));
                AfficherSucces("🗑 Question supprimée");
            });

            // Commande : valider le quiz
            ValiderQuizCommand = new RelayCommand(
                _ =>
                {
                    if (Questions.Count == 0)
                    {
                        MessageBox.Show(
                            "Le quiz ne contient aucune question.",
                            "Quiz vide",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    if (QuestionSelectionnee?.IsEditing == true)
                        AnnulerEditionInterne();

                    var res = MessageBox.Show(
                        $"Valider le quiz \"{TitreQuiz}\" ?\n\n" +
                        $"• {Questions.Count} questions\n" +
                        $"• Difficulté : {Difficulte}\n" +
                        $"• Statut : {Statut}\n\n" +
                        $"Le quiz sera sauvegardé et prêt à être publié.",
                        "Valider le quiz",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (res != MessageBoxResult.Yes) return;

                    QuizValide?.Invoke(Questions, TitreQuiz, Difficulte, CoursSourceTitre, Statut);
                },
                _ => Questions.Count > 0);

            // Commande : regénérer
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
                var res = MessageBox.Show(
                    "Quitter sans valider ?\nLe quiz généré sera perdu.",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                    NavigationRetourRequested?.Invoke();
            });
        }

    }


}