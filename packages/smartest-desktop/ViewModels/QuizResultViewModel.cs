using smartest_desktop.Helpers;
using System.Collections.ObjectModel;
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
                // Désélectionner l'ancienne
                if (_questionSelectionnee != null)
                    _questionSelectionnee.IsSelected = false;

                SetProperty(ref _questionSelectionnee, value);

                // Sélectionner la nouvelle
                if (_questionSelectionnee != null)
                    _questionSelectionnee.IsSelected = true;

                OnPropertyChanged(nameof(HasQuestionSelectionnee));
                OnPropertyChanged(nameof(HasNoQuestionSelectionnee));
            }
        }

        public bool HasQuestionSelectionnee => QuestionSelectionnee != null;
        public bool HasNoQuestionSelectionnee => QuestionSelectionnee == null;

        private readonly string _empreinteInitiale;

        private readonly Func<Task>? _supprimerQuizPersisteAsync;

        // ── Commandes ─────────────────────────────────────────────────────────
        public ICommand SelectionnerQuestionCommand { get; }
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
        private void RenuméroterQuestions()
        {
            int n = 1;
            foreach (var q in Questions)
                q.Numero = n++;
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
            return JsonSerializer.Serialize(payload);
        }

        private bool ADesModificationsDepuisOuverture() =>
            CalculerEmpreinte() != _empreinteInitiale;

        public string Statut { get; }

        /// <summary>Id en base si le quiz est ouvert depuis la liste ; sinon première génération.</summary>
        public int? QuizIdExistant { get; }

        public bool IsEditionQuizExistant => QuizIdExistant.HasValue;

        public string TitreFenetre =>
            IsEditionQuizExistant ? "SmarTest — Modifier le quiz" : "SmarTest — Quiz généré";

        public string LibelleBoutonValider =>
            IsEditionQuizExistant ? "Enregistrer les modifications" : "Valider et sauvegarder";

        public string SousTitreEtape =>
            IsEditionQuizExistant
                ? "Modifiez puis enregistrez dans la base locale"
                : "Vérifiez et ajustez avant validation";

        public string SousTitreCompteur =>
            IsEditionQuizExistant
                ? $"{NombreQuestions} questions"
                : $"{NombreQuestions} questions générées";



        public QuizResultViewModel(
    List<QuestionQCM> questions,
    string titre,
    string difficulte,
    string coursTitre,
    string statut,
    int? quizIdExistant = null,
    Func<Task>? supprimerQuizPersisteAsync = null)
        {
            QuizIdExistant = quizIdExistant;
            _supprimerQuizPersisteAsync = supprimerQuizPersisteAsync;
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

            if (Questions.Count > 0)
                QuestionSelectionnee = Questions[0];

            _empreinteInitiale = CalculerEmpreinte();

            // Commande : sélectionner une question
            SelectionnerQuestionCommand = new RelayCommand(param =>
            {
                if (param is QuestionQCM q)
                    QuestionSelectionnee = q;
            });

            // Commande : supprimer une question
            SupprimerQuestionCommand = new RelayCommand(param =>
            {
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

                    MessageBoxResult res;
                    if (IsEditionQuizExistant)
                    {
                        res = MessageBox.Show(
                            $"Enregistrer les modifications du quiz « {TitreQuiz} » ?\n\n" +
                            $"• {Questions.Count} questions\n" +
                            $"• Difficulté : {Difficulte}\n" +
                            $"• Statut : {Statut}\n\n" +
                            "Les données en base seront mises à jour.",
                            "Enregistrer les modifications",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                    }
                    else
                    {
                        res = MessageBox.Show(
                            $"Valider et sauvegarder le quiz « {TitreQuiz} » ?\n\n" +
                            $"• {Questions.Count} questions\n" +
                            $"• Difficulté : {Difficulte}\n" +
                            $"• Statut : {Statut}\n\n" +
                            "Le quiz sera enregistré localement et prêt à être publié.",
                            "Valider et sauvegarder",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                    }

                    if (res != MessageBoxResult.Yes) return;

                    QuizValide?.Invoke(Questions, TitreQuiz, Difficulte, CoursSourceTitre, Statut);
                },
                _ => Questions.Count > 0);

            // Commande : regénérer (masquée en édition d’un quiz déjà enregistré)
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
                if (!ADesModificationsDepuisOuverture())
                {
                    NavigationRetourRequested?.Invoke();
                    return;
                }

                string msg = IsEditionQuizExistant
                    ? "Quitter sans enregistrer les modifications ?\nLes changements seront perdus."
                    : "Quitter sans sauvegarder ?\nLe quiz généré sera perdu.";

                var res = MessageBox.Show(
                    msg,
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
                        $"Impossible de supprimer le quiz vide :\n{ex.Message}",
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error));
            }
        }

    }


}