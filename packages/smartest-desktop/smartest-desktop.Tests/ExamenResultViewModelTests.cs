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
}
