using smartest_desktop.Constants;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Services;
using smartest_desktop.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace smartest_desktop.Views
{
    public partial class QuizResultWindow : Window
    {
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

            Func<Task>? supprimerPersistant = null;
            if (quizIdExistant is int idQuiz)
            {
                supprimerPersistant = async () =>
                {
                    var svc = new LocalQuizService(App.LocalDb);
                    await svc.SupprimerAsync(idQuiz);
                };
            }

            var vm = new QuizResultViewModel(
                questions,
                titre,
                difficulte,
                coursTitre,
                statut,
                quizIdExistant,
                supprimerPersistant,
                emailsPublicationWebJson);
            DataContext = vm;

            vm.NavigationRetourRequested += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    var hub = new QuizExamenWindow();
                    hub.Show();
                    Close();
                });
            };

            vm.NavigationRegenerarRequested += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    var quizGen = new QuizGenerationWindow();
                    quizGen.Show();
                    Close();
                });
            };

            vm.QuizValide += async (questionsValidees, titreQuiz, difficulteQuiz, coursTitreQuiz, statutQuiz, emailsJson) =>
            {
                try
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

                    var svc = new LocalQuizService(App.LocalDb);

                    if (quizIdExistant is int idExistant)
                    {
                        await svc.MettreAJourContenuAsync(
                            idExistant,
                            titreQuiz,
                            difficulteQuiz,
                            coursTitreQuiz ?? string.Empty,
                            statutQuiz,
                            questionsDb,
                            emailsJson);

                        var token = Application.Current.Properties["Token"]?.ToString();
                        var errSync = await SyncPublicationWebApresEnregistrementAsync(
                            svc, idExistant, token, emailsJson);

                        Dispatcher.Invoke(() =>
                        {
                            var corps =
                                $"Les modifications du quiz « {titreQuiz} » ont été enregistrées.\n\n" +
                                $"• {questionsValidees.Count} questions\n" +
                                $"• Publication web : emails enregistrés localement\n" +
                                $"• Difficulté : {difficulteQuiz}\n" +
                                $"• Cours : {coursTitreQuiz}\n" +
                                $"• Statut : {statutQuiz}";
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

                            var hub = new QuizExamenWindow();
                            hub.Show();
                            Close();
                        });
                    }
                    else
                    {
                        var db = App.LocalDb;
                        var quiz = new QuizLocal
                        {
                            Titre = titreQuiz,
                            Difficulte = difficulteQuiz,
                            CoursSourceTitre = coursTitreQuiz ?? string.Empty,
                            Statut = statutQuiz,
                            NombreQuestions = questionsValidees.Count,
                            DateCreation = DateTime.Now,
                            Questions = questionsDb,
                            EmailsPublicationWebJson = emailsJson ?? "[]",
                        };

                        db.Quiz.Add(quiz);
                        await db.SaveChangesAsync();

                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(
                                $"Le quiz « {titreQuiz} » a été validé et sauvegardé.\n\n" +
                                $"• {questionsValidees.Count} questions\n" +
                                $"• Publication web : liste d'emails enregistrée\n" +
                                $"• Difficulté : {difficulteQuiz}\n" +
                                $"• Cours : {coursTitreQuiz}\n" +
                                $"• Statut : {statutQuiz}",
                                "Quiz enregistré",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                            var hub = new QuizExamenWindow();
                            hub.Show();
                            Close();
                        });
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                        MessageBox.Show(
                            $"Erreur lors de la sauvegarde :\n{ex.Message}",
                            "Erreur",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error));
                }
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

            var api = new QuizWebPublicationApiService();
            return await api.PostPublicationWebAsync(token, bid, emails);
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