using smartest_desktop.ViewModels;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Services;
using System.Windows;
using System.Text.Json;
using System.Linq;
using System.Collections.Generic;

namespace smartest_desktop.Views
{
    public partial class QuizExamenWindow : Window
    {
        public QuizExamenWindow()
        {
            InitializeComponent();

            if (DataContext is QuizExamenViewModel vm)
            {
                vm.NavigateToQuizGeneration += () =>
                {
                    var quizGen = new QuizGenerationWindow();
                    quizGen.Show();
                    this.Hide();
                };

                vm.NavigateToExamenGeneration += () =>
                {
                    var examenGen = new ExamenGenerationWindow();
                    examenGen.Show();
                    this.Close();
                };

                vm.NavigateToDashboard += () =>
                {
                    var dashboard = new DashboardWindow();
                    dashboard.Show();
                    this.Close();
                };

                vm.OuvrirStatistiques += () =>
                {
                    var win = new StatistiquesProfWindow();
                    win.Show();
                    Application.Current.MainWindow = win;
                    Close();
                };

                vm.NavigateToQuizDetails += async quiz =>
                {
                    var quizService = new LocalQuizService(App.LocalDb);
                    var quizComplet = await quizService.GetByIdAsync(quiz.Id);
                    if (quizComplet == null)
                    {
                        MessageBox.Show("Quiz introuvable.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var questions = quizComplet.Questions
                        .OrderBy(q => q.Numero)
                        .Select(q => new QuestionQCM
                        {
                            Numero = q.Numero,
                            Type = string.Equals(q.Type, "VF", System.StringComparison.OrdinalIgnoreCase) ? "VF" : "QCM",
                            Enonce = q.Enonce,
                            OptionA = string.Equals(q.Type, "VF", System.StringComparison.OrdinalIgnoreCase) ? "Vrai" : q.OptionA,
                            OptionB = string.Equals(q.Type, "VF", System.StringComparison.OrdinalIgnoreCase) ? "Faux" : q.OptionB,
                            OptionC = string.Equals(q.Type, "VF", System.StringComparison.OrdinalIgnoreCase) ? string.Empty : q.OptionC,
                            OptionD = string.Equals(q.Type, "VF", System.StringComparison.OrdinalIgnoreCase) ? string.Empty : q.OptionD,
                            ReponseCorrecte = string.Equals(q.Type, "VF", System.StringComparison.OrdinalIgnoreCase)
                                ? (q.ReponseCorrecte?.Trim().ToUpperInvariant() is "B" or "FAUX" or "FALSE" or "2" ? "B" : "A")
                                : q.ReponseCorrecte,
                            Explication = q.Explication
                        })
                        .ToList();

                    if (questions.Count == 0)
                    {
                        MessageBox.Show("Ce quiz ne contient aucune question.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var resultWindow = new QuizResultWindow(
                        questions,
                        quizComplet.Titre,
                        quizComplet.Difficulte,
                        quizComplet.CoursSourceTitre,
                        quizComplet.Statut,
                        quizComplet.Id,
                        emailsPublicationWebJson: quizComplet.EmailsPublicationWebJson);

                    resultWindow.Show();
                    this.Close();
                };

                vm.NavigateToExamenDetails += async examen =>
                {
                    var examenService = new LocalExamenService(App.LocalDb);
                    var examenComplet = await examenService.GetByIdAsync(examen.Id);
                    if (examenComplet == null)
                    {
                        MessageBox.Show("Examen introuvable.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var questions = examenComplet.Questions
                        .OrderBy(q => q.Numero)
                        .Select(ConvertirQuestionExamen)
                        .ToList();

                    if (questions.Count == 0)
                    {
                        MessageBox.Show("Cet examen ne contient aucune question.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    string coursTitre = examenComplet.Cours.FirstOrDefault()?.Titre ?? "Cours local";
                    string difficulte = examenComplet.Questions
                        .OrderBy(q => q.Numero)
                        .Select(q => q.Difficulte)
                        .FirstOrDefault(d => !string.IsNullOrWhiteSpace(d)) ?? "Moyen";

                    var resultWindow = new ExamenResultWindow(
                        questions,
                        examenComplet.Titre,
                        examenComplet.Duree,
                        difficulte,
                        coursTitre,
                        examenComplet.Id);

                    resultWindow.Show();
                    this.Close();
                };
            }
        }

        private static QuestionExamen ConvertirQuestionExamen(QuestionLocale q)
        {
            bool isVf = string.Equals(q.Type, "VF", System.StringComparison.OrdinalIgnoreCase);
            var vm = new QuestionExamen
            {
                Numero = q.Numero,
                Type = string.IsNullOrWhiteSpace(q.Type) ? "QCM" : q.Type,
                Enonce = q.Enonce,
                Difficulte = string.IsNullOrWhiteSpace(q.Difficulte) ? "Moyen" : q.Difficulte,
                Explication = q.Explication,
                OptionA = isVf ? "Vrai" : q.OptionA,
                OptionB = isVf ? "Faux" : q.OptionB,
                OptionC = isVf ? string.Empty : q.OptionC,
                OptionD = isVf ? string.Empty : q.OptionD,
                ReponseCorrecte = isVf
                    ? (q.ReponseCorrecte?.Trim().ToUpperInvariant() is "B" or "FAUX" or "FALSE" or "2" ? "B" : "A")
                    : q.ReponseCorrecte,
                ReponseModele = q.ReponseModele,
                BaremePoints = q.BaremePoints,
                ImageBase64 = q.ImageBase64,
                ImageType = q.ImageType,
                ImageNom = q.ImageNom
            };

            if (!string.IsNullOrWhiteSpace(q.ReponsesCorrectesJson))
            {
                try
                {
                    var rep = JsonSerializer.Deserialize<List<string>>(q.ReponsesCorrectesJson) ?? new List<string>();
                    vm.OptionACorrecte = rep.Contains("A");
                    vm.OptionBCorrecte = rep.Contains("B");
                    vm.OptionCCorrecte = rep.Contains("C");
                    vm.OptionDCorrecte = rep.Contains("D");
                }
                catch
                {
                    // Ignorer JSON invalide et continuer avec false par défaut.
                }
            }

            return vm;
        }
    }
}