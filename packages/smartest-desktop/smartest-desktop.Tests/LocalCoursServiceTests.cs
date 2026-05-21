using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Services;
using Xunit;

namespace smartest_desktop.Tests;

public class LocalCoursServiceTests
{
    [Fact]
    public void GetAll_retourne_cours_ordonnes_par_date()
    {
        using var db = TestDbFactory.CreateInMemoryContext();
        db.Cours.Add(new CoursLocal { Titre = "A", Contenu = "c", DateImport = DateTime.UtcNow.AddDays(-1) });
        db.Cours.Add(new CoursLocal { Titre = "B", Contenu = "c", DateImport = DateTime.UtcNow });
        db.SaveChanges();

        var svc = new LocalCoursService(db);
        var list = svc.GetAll();

        Assert.Equal(2, list.Count);
        Assert.Equal("B", list[0].Titre);
    }

    [Fact]
    public void Ajouter_insere_et_retourne_id()
    {
        using var db = TestDbFactory.CreateInMemoryContext();
        var svc = new LocalCoursService(db);

        var c = svc.Ajouter("Titre", "Contenu long");

        Assert.True(c.Id > 0);
        Assert.Single(db.Cours);
    }

    [Fact]
    public void Modifier_met_a_jour_titre()
    {
        using var db = TestDbFactory.CreateInMemoryContext();
        db.Cours.Add(new CoursLocal { Titre = "Old", Contenu = "x" });
        db.SaveChanges();
        var svc = new LocalCoursService(db);

        var updated = svc.Modifier(1, "New", null);

        Assert.Equal("New", updated.Titre);
    }

    [Fact]
    public void Supprimer_retire_le_cours()
    {
        using var db = TestDbFactory.CreateInMemoryContext();
        db.Cours.Add(new CoursLocal { Titre = "X", Contenu = "y" });
        db.SaveChanges();
        var svc = new LocalCoursService(db);

        svc.Supprimer(1);

        Assert.Empty(db.Cours);
    }

    [Fact]
    public void Modifier_cours_introuvable_leve_KeyNotFoundException()
    {
        using var db = TestDbFactory.CreateInMemoryContext();
        var svc = new LocalCoursService(db);

        Assert.Throws<KeyNotFoundException>(() => svc.Modifier(99, "T", null));
    }
}
