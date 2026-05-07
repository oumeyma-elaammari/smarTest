using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Services;
using Xunit;

namespace smartest_desktop.Tests;

public class LocalQuizServiceTests
{
    [Fact]
    public async Task GetAllAsync_retourne_quiz()
    {
        await using var db = TestDbFactory.CreateInMemory();
        db.Quiz.Add(new QuizLocal { Titre = "Q", Difficulte = "Moyen", NombreQuestions = 1 });
        await db.SaveChangesAsync();
        var svc = new LocalQuizService(db);

        var list = await svc.GetAllAsync();

        Assert.Single(list);
        Assert.Equal("Q", list[0].Titre);
    }

    [Fact]
    public async Task GetByIdAsync_retourne_null_si_absent()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var svc = new LocalQuizService(db);

        var q = await svc.GetByIdAsync(42);

        Assert.Null(q);
    }

    [Fact]
    public async Task AjouterAsync_SupprimerAsync_cycle()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var svc = new LocalQuizService(db);
        var quiz = new QuizLocal { Titre = "T", Difficulte = "Facile", NombreQuestions = 0 };

        await svc.AjouterAsync(quiz);
        Assert.True(quiz.Id > 0);

        await svc.SupprimerAsync(quiz.Id);
        Assert.Empty(db.Quiz);
    }

    [Fact]
    public async Task MettreAJourPublicationWebLocaleAsync_introuvable_leve()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var svc = new LocalQuizService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.MettreAJourPublicationWebLocaleAsync(1, 10L, "[]", "Publié"));
    }

    [Fact]
    public async Task MettreAJourContenuAsync_introuvable_leve()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var svc = new LocalQuizService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.MettreAJourContenuAsync(9, "t", "Moyen", "c", "BROUILLON", Array.Empty<QuestionLocale>()));
    }
}
