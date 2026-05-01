using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Services;
using smartest_desktop.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace smartest_desktop.Views
{
    public partial class ExamenResultWindow : Window
    {
        public ExamenResultWindow(
            List<QuestionExamen> questions,
            string titre,
            int duree,
            string difficulte,
            string coursTitre,
            int? examenIdExistant = null)
        {
            InitializeComponent();

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
                supprimerPersistant);
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
                    var examenGen = new ExamenGenerationWindow();
                    examenGen.Show();
                    Close();
                });
            };

            vm.ExamenValide += async (questionsValidees, titreExamen, dureeExamen, difficulteExamen, coursTitreExamen) =>
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
                            questionsValidees.ToList());

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

                            var hub = new QuizExamenWindow();
                            hub.Show();
                            Close();
                        });
                    }
                    else
                    {
                        var examen = new ExamenLocal
                        {
                            Titre = titreExamen,
                            Duree = dureeExamen,
                            Statut = "BROUILLON",
                            DateCreation = DateTime.Now
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
                                $"• Cours : {coursTitreExamen}",
                                "Examen enregistré",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);

                            var dashboard = new DashboardWindow();
                            dashboard.Show();
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
    }
}
