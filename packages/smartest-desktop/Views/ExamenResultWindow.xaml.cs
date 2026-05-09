using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Helpers;
using smartest_desktop.Services;
using smartest_desktop.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace smartest_desktop.Views
{
    public partial class ExamenResultWindow : Window
    {
        private bool _fermetureConfirmee;
        private bool _isClosing;
        private bool _navigationInProgress;

        public ExamenResultWindow(
            List<QuestionExamen> questions,
            string titre,
            int duree,
            string difficulte,
            string coursTitre,
            int? examenIdExistant = null,
            string statutExamen = "BROUILLON",
            string? emailsPublicationWebJson = null,
            DateTime? datePrevue = null)
        {
            InitializeComponent();
            Closing += ExamenResultWindow_Closing;

            Func<Task>? supprimerPersistant = null;
            if (examenIdExistant is int idEx)
            {
                supprimerPersistant = async () =>
                {
                    var svc = new LocalExamenService(App.LocalDb);
                    await svc.SupprimerAsync(idEx);
                };
            }

            var vm = new ExamenResultViewModel(
                questions,
                titre,
                duree,
                difficulte,
                coursTitre,
                examenIdExistant,
                supprimerPersistant,
                statutExamen,
                emailsPublicationWebJson,
                datePrevue);
            DataContext = vm;

            vm.NavigationRetourRequested += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    NaviguerEtFermer(() => new QuizExamenWindow());
                });
            };

            vm.NavigationRegenerarRequested += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    NaviguerEtFermer(() => new ExamenGenerationWindow());
                });
            };

            vm.ExamenValide += async (
                questionsValidees,
                titreExamen,
                dureeExamen,
                difficulteExamen,
                coursTitreExamen,
                emailsJson,
                datePrevuePassage) =>
            {
                try
                {
                    var svcPersist = new ExamenLocalService(App.LocalDb);

                    if (examenIdExistant is int idExistant)
                    {
                        await svcPersist.MettreAJourContenuAsync(
                            idExistant,
                            titreExamen,
                            dureeExamen,
                            questionsValidees.ToList(),
                            emailsJson,
                            datePrevuePassage);

                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(
                                $"Les modifications de l'examen « {titreExamen} » ont été enregistrées.\n\n" +
                                $"• {questionsValidees.Count} questions\n" +
                                $"• Difficulté : {difficulteExamen}\n" +
                                $"• Durée : {dureeExamen} min\n" +
                                $"• Cours : {coursTitreExamen}",
                                "Modifications enregistrées",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                            OuvrirHubEtFermer();
                        });
                    }
                    else
                    {
                        var examen = new ExamenLocal
                        {
                            Titre = titreExamen,
                            Duree = dureeExamen,
                            Statut = "BROUILLON",
                            DateCreation = DateTime.Now,
                            EmailsPublicationWebJson = emailsJson ?? "[]",
                            DatePrevue = datePrevuePassage
                        };

                        await svcPersist.SauvegarderAsync(
                            examen,
                            questionsValidees.ToList(),
                            coursTitreExamen ?? string.Empty);

                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(
                                $"L'examen « {titreExamen} » a été validé et sauvegardé.\n\n" +
                                $"• {questionsValidees.Count} questions\n" +
                                $"• Difficulté : {difficulteExamen}\n" +
                                $"• Durée : {dureeExamen} min\n" +
                                $"• Cours : {coursTitreExamen}\n\n" +
                                "Renseignez la publication web et le créneau dans l'écran de révision, puis utilisez « Publier sur le web » dans la liste des examens.",
                                "Examen enregistré",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                            OuvrirHubEtFermer();
                        });
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                        MessageBox.Show(
                            UserErrorMessage.FromException(ex, "Impossible d'enregistrer l'examen pour le moment."),
                            "Erreur",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error));
                }
            };
        }

        private void OuvrirHubEtFermer()
        {
            NaviguerEtFermer(() => new QuizExamenWindow());
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

        private void ExamenResultWindow_Closing(object? sender, CancelEventArgs e)
        {
            _isClosing = true;
            if (_fermetureConfirmee)
                return;

            _fermetureConfirmee = true;
            _navigationInProgress = true;
            Application.Current.Shutdown();
        }
    }
}
