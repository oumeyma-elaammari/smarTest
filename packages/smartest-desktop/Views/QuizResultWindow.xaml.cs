using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Exceptions;
using smartest_desktop.Helpers;
using smartest_desktop.Services;
using smartest_desktop.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace smartest_desktop.Views
{
    public partial class QuizResultWindow : Window
    {
        private bool _fermetureConfirmee;
        private bool _isClosing;
        private bool _navigationInProgress;

        private sealed class QuizSaveContext
        {
            public required ObservableCollection<QuestionQCM> QuestionsValidees { get; init; }
            public required List<QuestionLocale> QuestionsDb { get; init; }
            public required string TitreQuiz { get; init; }
            public required string DifficulteQuiz { get; init; }
            public required string CoursTitreQuiz { get; init; }
            public required string StatutQuiz { get; init; }
            public required string EmailsJson { get; init; }
        }

        public QuizResultWindow(
            List<QuestionQCM> questions,
            string titre,
            string difficulte,
            string coursTitre,
            string statut,
            int? quizIdExistant = null,
            string? emailsPublicationWebJson = null)
        {
            InitializeComponent();
            Closing += QuizResultWindow_Closing;

            Func<Task>? supprimerPersistant = CreerActionSuppressionPersistante(quizIdExistant);

            var vm = new QuizResultViewModel(
                questions,
                new QuizResultViewModel.QuizResultViewModelInit
                {
                    Titre = titre,
                    Difficulte = difficulte,
                    CoursTitre = coursTitre,
                    Statut = statut,
                    QuizIdExistant = quizIdExistant,
                    SupprimerQuizPersisteAsync = supprimerPersistant,
                    EmailsPublicationWebJsonInit = emailsPublicationWebJson
                });
            DataContext = vm;
            ConfigurerNavigationHandlers(vm);
            ConfigurerSauvegardeHandler(vm, quizIdExistant);
        }

        private void ConfigurerNavigationHandlers(QuizResultViewModel vm)
        {
            vm.NavigationRetourRequested += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    _fermetureConfirmee = true;
                    _navigationInProgress = true;
                    App.OuvrirShell(MainShellSection.QuizExamens);
                    if (!_isClosing)
                        Close();
                });
            };

            vm.NavigationRegenerarRequested += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    NaviguerEtFermer(() => new QuizGenerationWindow());
                });
            };
        }

        private void ConfigurerSauvegardeHandler(QuizResultViewModel vm, int? quizIdExistant)
        {
            vm.QuizValide += async (questionsValidees, titreQuiz, difficulteQuiz, coursTitreQuiz, statutQuiz, emailsJson) =>
            {
                try
                {
                    await TraiterQuizValideAsync(
                        quizIdExistant,
                        questionsValidees,
                        titreQuiz,
                        difficulteQuiz,
                        coursTitreQuiz,
                        statutQuiz,
                        emailsJson);
                }
                catch (Exception ex)
                {
                    vm.MarquerValidationTerminee();
                    Dispatcher.Invoke(() =>
                        MessageBox.Show(
                            UserErrorMessage.FromException(ex, "Impossible d'enregistrer le quiz pour le moment."),
                            "Erreur",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error));
                }
            };
        }

        private async Task TraiterQuizValideAsync(
            int? quizIdExistant,
            ObservableCollection<QuestionQCM> questionsValidees,
            string titreQuiz,
            string difficulteQuiz,
            string coursTitreQuiz,
            string statutQuiz,
            string emailsJson)
        {
            var questionsDb = questionsValidees.Select((q, idx) => new QuestionLocale
            {
                Numero = idx + 1,
                Type = string.Equals(q.Type, "VF", StringComparison.OrdinalIgnoreCase) ? "VF" : "QCM",
                Enonce = q.Enonce,
                OptionA = string.Equals(q.Type, "VF", StringComparison.OrdinalIgnoreCase) ? "Vrai" : q.OptionA,
                OptionB = string.Equals(q.Type, "VF", StringComparison.OrdinalIgnoreCase) ? "Faux" : q.OptionB,
                OptionC = string.Equals(q.Type, "VF", StringComparison.OrdinalIgnoreCase) ? string.Empty : q.OptionC,
                OptionD = string.Equals(q.Type, "VF", StringComparison.OrdinalIgnoreCase) ? string.Empty : q.OptionD,
                ReponseCorrecte = string.Equals(q.Type, "VF", StringComparison.OrdinalIgnoreCase)
                    ? (q.ReponseCorrecte?.Trim().ToUpperInvariant() is "B" or "FAUX" or "FALSE" or "2" ? "B" : "A")
                    : q.ReponseCorrecte,
                Explication = q.Explication,
                Difficulte = difficulteQuiz
            }).ToList();
            var context = new QuizSaveContext
            {
                QuestionsValidees = questionsValidees,
                QuestionsDb = questionsDb,
                TitreQuiz = titreQuiz,
                DifficulteQuiz = difficulteQuiz,
                CoursTitreQuiz = coursTitreQuiz,
                StatutQuiz = statutQuiz,
                EmailsJson = emailsJson
            };

            var svc = new LocalQuizService(App.LocalDb);
            if (quizIdExistant is int idExistant)
            {
                await MettreAJourQuizExistantAsync(svc, idExistant, context);
                return;
            }

            await CreerNouveauQuizAsync(context);
        }

        private async Task MettreAJourQuizExistantAsync(
            LocalQuizService svc,
            int idExistant,
            QuizSaveContext context)
        {
            await svc.MettreAJourContenuAsync(
                idExistant,
                context.TitreQuiz,
                context.DifficulteQuiz,
                context.CoursTitreQuiz ?? string.Empty,
                context.StatutQuiz,
                context.QuestionsDb,
                context.EmailsJson);

            var quizRechargé = await svc.GetByIdAsync(idExistant);
            if (quizRechargé != null)
                await SynchroniserVersServeurSiBesoinAsync(quizRechargé, context).ConfigureAwait(false);

            await Dispatcher.InvokeAsync(() =>
            {
                var corps =
                    $"Les modifications du quiz « {context.TitreQuiz} » ont été enregistrées.\n\n" +
                    $"• {context.QuestionsValidees.Count} questions\n" +
                    $"• Liste d’emails (publication web) : enregistrée en local uniquement — " +
                    $"la mise en ligne sur le serveur se fait avec « Publier » dans Mes évaluations.\n" +
                    $"• Difficulté : {context.DifficulteQuiz}\n" +
                    $"• Cours : {context.CoursTitreQuiz}\n" +
                    $"• Statut : {context.StatutQuiz}";

                MessageBox.Show(
                    corps,
                    "Modifications enregistrées",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                OuvrirHubEtFermer();
            });
        }

        /// <summary>
        /// Si le quiz est lié au serveur (publication web et/ou copie QR), pousse les questions — et pour le publié web,
        /// la liste d’emails — pour garder deux lignes MySQL alignées lorsque les deux ids existent.
        /// </summary>
        private static async Task SynchroniserVersServeurSiBesoinAsync(QuizLocal quiz, QuizSaveContext context)
        {
            var token = Application.Current.Properties["Token"]?.ToString();
            if (string.IsNullOrWhiteSpace(token))
                return;

            var api = new QuizWebPublicationApiService();
            var questions = quiz.Questions ?? new List<QuestionLocale>();

            bool publié = string.Equals(quiz.Statut?.Trim(), "Publié", StringComparison.OrdinalIgnoreCase);
            long? idPublication = quiz.BackendQuizIdPublicationWeb ?? quiz.BackendQuizId;

            if (publié && idPublication is long ip && ip > 0)
            {
                List<string> emails = new();
                try
                {
                    emails = JsonConvert.DeserializeObject<List<string>>(context.EmailsJson ?? "[]") ?? new List<string>();
                    emails = emails
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .Select(e => e.Trim().ToLowerInvariant())
                        .Distinct()
                        .ToList();
                }
                catch
                {
                    return;
                }

                if (emails.Count == 0)
                    return;

                try
                {
                    await api.PostPublicationWebAsync(token.Trim(), ip, emails, questions).ConfigureAwait(false);
                }
                catch
                {
                    /* hors flux critique de l’enregistrement local */
                }
            }

            if (quiz.BackendQuizIdQr is long iqr && iqr > 0)
            {
                try
                {
                    await api.SyncQuestionsProfAsync(token.Trim(), iqr, questions).ConfigureAwait(false);
                }
                catch
                {
                    /* idem */
                }
            }
        }

        private async Task CreerNouveauQuizAsync(QuizSaveContext context)
        {
            var db = App.LocalDb;
            var quiz = new QuizLocal
            {
                Titre = context.TitreQuiz,
                Difficulte = context.DifficulteQuiz,
                CoursSourceTitre = context.CoursTitreQuiz ?? string.Empty,
                Statut = context.StatutQuiz,
                NombreQuestions = context.QuestionsValidees.Count,
                DateCreation = DateTime.Now,
                Questions = context.QuestionsDb,
                EmailsPublicationWebJson = context.EmailsJson ?? "[]",
            };

            db.Quiz.Add(quiz);
            await db.SaveChangesAsync();

            await Dispatcher.InvokeAsync(() =>
            {
                MessageBox.Show(
                    $"Le quiz « {context.TitreQuiz} » a été validé et sauvegardé.\n\n" +
                    $"• {context.QuestionsValidees.Count} questions\n" +
                    $"• Liste d’emails (publication web) : enregistrée en local — la publication sur le serveur se fait avec « Publier » dans Mes évaluations.\n" +
                    $"• Difficulté : {context.DifficulteQuiz}\n" +
                    $"• Cours : {context.CoursTitreQuiz}\n" +
                    $"• Statut : {context.StatutQuiz}",
                    "Quiz enregistré",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                OuvrirHubEtFermer();
            });
        }

        private void OuvrirHubEtFermer()
        {
            // Avant OuvrirShell : sinon Closing verra _fermetureConfirmee == false et appellera Shutdown().
            _fermetureConfirmee = true;
            _navigationInProgress = true;
            App.OuvrirShell(MainShellSection.QuizExamens);
            if (!_isClosing)
                Close();
        }

        private void NaviguerEtFermer(Func<Window> nextWindowFactory)
        {
            if (_navigationInProgress)
                return;

            _navigationInProgress = true;
            _fermetureConfirmee = true;

            var nextWindow = nextWindowFactory();
            nextWindow.Show();

            if (!_isClosing)
                Close();
        }

        private void QuizResultWindow_Closing(object? sender, CancelEventArgs e)
        {
            _isClosing = true;
            if (_fermetureConfirmee)
            {
                return;
            }
            _fermetureConfirmee = true;
            _navigationInProgress = true;
            Application.Current.Shutdown();
        }

        private static Func<Task>? CreerActionSuppressionPersistante(int? quizIdExistant)
        {
            if (quizIdExistant is not int idQuiz)
            {
                return null;
            }

            return async () =>
            {
                var svc = new LocalQuizService(App.LocalDb);
                var token = Application.Current.Properties["Token"]?.ToString();
                try
                {
                    await svc.SupprimerLocalesEtServeurAsync(idQuiz, token);
                }
                catch (QuizDeleteLocalOnlyConfirmationRequiredException ex)
                {
                    var r = MessageBox.Show(
                        ex.Message + Environment.NewLine + Environment.NewLine +
                        "Souhaitez-vous supprimer uniquement les données sur cet ordinateur ?",
                        "Aucune copie correspondante sur le serveur",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if (r == MessageBoxResult.Yes)
                        await svc.SupprimerLocalesEtServeurAsync(
                            idQuiz,
                            token,
                            forceLocalDeleteIfNoServerMatch: true);
                }
            };
        }

    }
}