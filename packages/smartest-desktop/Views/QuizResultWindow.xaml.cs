using smartest_desktop.Constants;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Exceptions;
using smartest_desktop.Services;
using smartest_desktop.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace smartest_desktop.Views
{
    public partial class QuizResultWindow : Window
    {
        private bool _fermetureConfirmee;

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
                    var hub = new QuizExamenWindow();
                    hub.Show();
                    Close();
                });
            };

            vm.NavigationRegenerarRequested += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    _fermetureConfirmee = true;
                    var quizGen = new QuizGenerationWindow();
                    quizGen.Show();
                    Close();
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
                            $"Erreur lors de la sauvegarde :\n{ex.Message}",
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
                Type = "QCM",
                Enonce = q.Enonce,
                OptionA = q.OptionA,
                OptionB = q.OptionB,
                OptionC = q.OptionC,
                OptionD = q.OptionD,
                ReponseCorrecte = q.ReponseCorrecte,
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

            var token = Application.Current.Properties["Token"]?.ToString();
            var errSync = await SyncPublicationWebApresEnregistrementAsync(
                svc, idExistant, token, context.EmailsJson);

            await Dispatcher.InvokeAsync(() =>
            {
                var corps =
                    $"Les modifications du quiz « {context.TitreQuiz} » ont été enregistrées.\n\n" +
                    $"• {context.QuestionsValidees.Count} questions\n" +
                    $"• Publication web : emails enregistrés localement\n" +
                    $"• Difficulté : {context.DifficulteQuiz}\n" +
                    $"• Cours : {context.CoursTitreQuiz}\n" +
                    $"• Statut : {context.StatutQuiz}";
                if (!string.IsNullOrWhiteSpace(errSync))
                    corps += "\n\n— Serveur publication web —\n" + errSync;

                MessageBox.Show(
                    corps,
                    string.IsNullOrWhiteSpace(errSync)
                        ? "Modifications enregistrées"
                        : "Modifications enregistrées (avertissement serveur)",
                    MessageBoxButton.OK,
                    string.IsNullOrWhiteSpace(errSync)
                        ? MessageBoxImage.Information
                        : MessageBoxImage.Warning);

                OuvrirHubEtFermer();
            });
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
                    $"• Publication web : liste d'emails enregistrée\n" +
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
            _fermetureConfirmee = true;
            var hub = new QuizExamenWindow();
            hub.Show();
            Close();
        }

        private void QuizResultWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (_fermetureConfirmee)
            {
                return;
            }

            if (DataContext is QuizResultViewModel vm &&
                vm.RetourCommand?.CanExecute(null) == true)
            {
                e.Cancel = true;
                vm.RetourCommand.Execute(null);
            }
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
                await svc.SupprimerAsync(idQuiz);
            };
        }

        /// <summary>Si le quiz a un ID serveur et des emails valides, renvoie la liste sur le backend (même route que « Publier »).</summary>
        private static async Task<string?> SyncPublicationWebApresEnregistrementAsync(
            LocalQuizService svc,
            int quizLocalId,
            string? token,
            string? emailsJson)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var quiz = await svc.GetByIdAsync(quizLocalId);
            if (quiz?.BackendQuizId is not long bid || bid <= 0)
                return null;

            var emails = ParseEmailsPublicationJson(emailsJson);
            if (emails.Count == 0)
                return null;

            try
            {
                var api = new QuizWebPublicationApiService();
                await api.PostPublicationWebAsync(token, bid, emails);
                return null;
            }
            catch (SmartestApiException ex)
            {
                return ex.Message;
            }
            catch (SmartestNetworkException ex)
            {
                return ex.Message;
            }
        }

        private static List<string> ParseEmailsPublicationJson(string? json)
        {
            var liste = new List<string>();
            if (string.IsNullOrWhiteSpace(json))
                return liste;
            try
            {
                var raw = JsonConvert.DeserializeObject<List<string>>(json.Trim()) ?? new List<string>();
                var vu = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in raw.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    var t = e.Trim().ToLowerInvariant();
                    if (!ImportEtudiantsService.EstEmail(t) || !vu.Add(t))
                        continue;
                    liste.Add(t);
                    if (liste.Count >= QuizPublicationLimits.MaxAuthorizedStudentEmails)
                        break;
                }
            }
            catch
            {
                // ignoré
            }

            return liste;
        }
    }
}