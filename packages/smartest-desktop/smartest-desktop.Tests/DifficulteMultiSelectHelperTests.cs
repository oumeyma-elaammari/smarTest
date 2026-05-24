using smartest_desktop.ViewModels;
using Xunit;

namespace smartest_desktop.Tests;

public class DifficulteMultiSelectHelperTests
{
    [Fact]
    public void Repartir_deux_niveaux_10_questions()
    {
        var rep = DifficulteMultiSelectHelper.Repartir(10, new[] { "Facile", "Moyen" });
        Assert.Equal(5, rep["Facile"]);
        Assert.Equal(5, rep["Moyen"]);
    }

    [Fact]
    public void Repartir_trois_niveaux_10_questions()
    {
        var rep = DifficulteMultiSelectHelper.Repartir(10, DifficulteMultiSelectHelper.Ordre);
        Assert.Equal(4, rep["Facile"]);
        Assert.Equal(3, rep["Moyen"]);
        Assert.Equal(3, rep["Difficile"]);
    }

    [Fact]
    public void Formater_et_parser_libelle_combine()
    {
        var libelle = DifficulteMultiSelectHelper.FormaterLibelle(new[] { "Facile", "Difficile" });
        Assert.Equal("Facile + Difficile", libelle);
        var parsed = DifficulteMultiSelectHelper.ParserLibelle(libelle);
        Assert.Equal(new[] { "Facile", "Difficile" }, parsed);
    }

    [Fact]
    public void Toggle_ne_supprime_pas_le_dernier_niveau()
    {
        var set = new HashSet<string>(StringComparer.Ordinal) { "Moyen" };
        Assert.False(DifficulteMultiSelectHelper.Toggle(set, "Moyen"));
        Assert.Single(set);
    }
}
