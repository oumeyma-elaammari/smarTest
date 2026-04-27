using smartest_desktop.Data.LocalEntities;
using smartest_desktop.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace smartest_desktop.Views
{
    public partial class QuizResultWindow : Window
    {
        public QuizResultWindow(List<QuestionQCM> questions, string titre, string difficulte, string coursTitre, string statut)
        {
            InitializeComponent();

            var vm = new QuizResultViewModel(questions, titre, difficulte, coursTitre, statut);
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

            vm.QuizValide += async (questionsValidees, titreQuiz, difficulteQuiz, coursTitreQuiz, statutQuiz) =>
            {
                try
                {
                    var db = App.LocalDb;

                    var questionsDb = questionsValidees.Select((q, idx) => new QuestionLocale
                    {
                        Numero          = idx + 1,
                        Type            = "QCM",
                        Enonce          = q.Enonce,
                        OptionA         = q.OptionA,
                        OptionB         = q.OptionB,
                        OptionC         = q.OptionC,
                        OptionD         = q.OptionD,
                        ReponseCorrecte = q.ReponseCorrecte,
                        Explication     = q.Explication,
                        Difficulte      = difficulteQuiz
                    }).ToList();

                    var quiz = new QuizLocal
                    {
                        Titre             = titreQuiz,
                        Difficulte        = difficulteQuiz,
                        CoursSourceTitre  = coursTitreQuiz ?? string.Empty,
                        Statut            = statutQuiz,
                        NombreQuestions   = questionsValidees.Count,
                        DateCreation      = DateTime.Now,
                        Questions         = questionsDb
                    };

                    db.Quiz.Add(quiz);
                    await db.SaveChangesAsync();

                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show(
                            $"✅ Le quiz \"{titreQuiz}\" a été sauvegardé !\n\n" +
                            $"• {questionsValidees.Count} questions\n" +
                            $"• Difficulté : {difficulteQuiz}\n" +
                            $"• Cours : {coursTitreQuiz}\n" +
                            $"• Statut : {statutQuiz}",
                            "Quiz sauvegardé",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        var dashboard = new DashboardWindow();
                        dashboard.Show();
                        Close();
                    });
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
    }
}