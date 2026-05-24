using System.Linq;
using smartest_desktop.ViewModels;
using Xunit;

namespace smartest_desktop.Tests;

public class ExamenResultViewModelTests
{
    [Fact]
    public void Constructeur_applique_un_bareme_auto_total_20_si_absent()
    {
        var questions = new[]
        {
            new QuestionExamen { Type = "QCM", Difficulte = "Moyen", Enonce = "Q1" },
            new QuestionExamen { Type = "VF", Difficulte = "Facile", Enonce = "Q2" },
            new QuestionExamen { Type = "REDACTION", Difficulte = "Difficile", Enonce = "Q3" },
            new QuestionExamen { Type = "CHECKBOX", Difficulte = "Moyen", Enonce = "Q4" },
        }.ToList();

        var vm = new ExamenResultViewModel(questions, "Ex", 90, "Moyen", "Cours");

        Assert.True(vm.BaremeValide);
        Assert.Equal(20.0, vm.TotalBareme, 6);
        Assert.All(vm.Questions, q => Assert.Equal(0, (q.BaremePoints * 4) % 1, 6));
    }

    [Fact]
    public void Bareme_invalide_autorise_le_clic_mais_reste_invalide()
    {
        var questions = new[]
        {
            new QuestionExamen { Type = "QCM", Difficulte = "Moyen", Enonce = "Q1", BaremePoints = 10 },
            new QuestionExamen { Type = "QCM", Difficulte = "Moyen", Enonce = "Q2", BaremePoints = 10 },
        }.ToList();

        var vm = new ExamenResultViewModel(questions, "Ex", 60, "Moyen", "Cours");
        Assert.True(vm.BaremeValide);
        Assert.True(vm.ValiderExamenCommand.CanExecute(null));

        vm.Questions[0].BaremePoints = 9.75;

        Assert.False(vm.BaremeValide);
        Assert.True(vm.ValiderExamenCommand.CanExecute(null));
    }

    [Fact]
    public void Constructeur_repartit_les_temps_indicatifs_sur_la_duree_totale()
    {
        var questions = new[]
        {
            new QuestionExamen { Type = "QCM", Enonce = "Q1" },
            new QuestionExamen { Type = "QCM", Enonce = "Q2" },
        }.ToList();

        var vm = new ExamenResultViewModel(questions, "Ex", 90, "Moyen", "Cours");

        Assert.Equal(90 * 60, vm.TotalSecondesIndicatifQuestions);
        Assert.True(vm.DureeQuestionsValide);
        Assert.Equal(45 * 60, questions[0].DureeSecondesIndicative);
        Assert.Equal(45 * 60, questions[1].DureeSecondesIndicative);
    }

    [Fact]
    public void Changement_duree_examen_repartit_les_temps_indicatifs()
    {
        var questions = new[]
        {
            new QuestionExamen { Type = "QCM", Enonce = "Q1" },
            new QuestionExamen { Type = "QCM", Enonce = "Q2" },
            new QuestionExamen { Type = "QCM", Enonce = "Q3" },
        }.ToList();

        var vm = new ExamenResultViewModel(questions, "Ex", 60, "Moyen", "Cours");
        Assert.Equal(60 * 60, vm.TotalSecondesIndicatifQuestions);

        vm.DureeMinutesEditable = 61;
        Assert.Equal(61 * 60, vm.TotalSecondesIndicatifQuestions);
        Assert.Equal(61 * 60, questions.Sum(q => q.DureeSecondesIndicative));
    }

    [Fact]
    public void Date_passee_affiche_erreur_et_bloque_la_validation()
    {
        var questions = new[]
        {
            new QuestionExamen { Type = "QCM", Enonce = "Q1" },
        }.ToList();

        var vm = new ExamenResultViewModel(questions, "Ex", 60, "Moyen", "Cours");
        Assert.True(vm.CreneauEstValide);
        Assert.True(vm.ValiderExamenCommand.CanExecute(null));

        vm.DateExamen = DateTime.Today.AddDays(-1);

        Assert.False(vm.CreneauEstValide);
        Assert.True(vm.AfficherErreurCreneau);
        Assert.Contains("passée", vm.MessageErreurCreneau, StringComparison.Ordinal);
        Assert.False(vm.ValiderExamenCommand.CanExecute(null));
    }

    [Fact]
    public void Changement_duree_d_une_question_reequilibre_les_autres()
    {
        var questions = new[]
        {
            new QuestionExamen { Type = "QCM", Enonce = "Q1" },
            new QuestionExamen { Type = "QCM", Enonce = "Q2" },
        }.ToList();

        var vm = new ExamenResultViewModel(questions, "Ex", 90, "Moyen", "Cours");
        vm.Questions[0].DureeSecondesIndicative = 60 * 60;

        Assert.Equal(90 * 60, vm.TotalSecondesIndicatifQuestions);
        Assert.Equal(30 * 60, vm.Questions[1].DureeSecondesIndicative);
    }
}
