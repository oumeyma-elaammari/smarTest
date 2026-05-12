using Microsoft.EntityFrameworkCore;
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
    public async Task SupprimerAsync_efface_questions_reponses_et_liaison_cours()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var cours = new CoursLocal { Titre = "C1" };
        db.Cours.Add(cours);
        await db.SaveChangesAsync();

        var quiz = new QuizLocal { Titre = "Q1", Difficulte = "Moyen", NombreQuestions = 1, BackendQuizId = 99 };
        quiz.Cours.Add(cours);
        db.Quiz.Add(quiz);
        await db.SaveChangesAsync();

        var question = new QuestionLocale
        {
            QuizLocalId = quiz.Id,
            Numero = 1,
            Enonce = "Q?",
            Type = "QCM",
        };
        db.Questions.Add(question);
        await db.SaveChangesAsync();

        db.Reponses.Add(new ReponseLocale { QuestionId = question.Id, Contenu = "A", EstCorrecte = true });
        await db.SaveChangesAsync();

        var svc = new LocalQuizService(db);
        await svc.SupprimerAsync(quiz.Id);

        Assert.Empty(await db.Quiz.ToListAsync());
        Assert.Empty(await db.Questions.ToListAsync());
        Assert.Empty(await db.Reponses.ToListAsync());
        var coursRecharge = await db.Cours.Include(c => c.Quiz).FirstAsync();
        Assert.Empty(coursRecharge.Quiz);
    }

    [Fact]
    public async Task DefinirBackendQuizIdSiAbsenteAsync_remplit_si_absent()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var q = new QuizLocal { Titre = "T", Difficulte = "Moyen", NombreQuestions = 1 };
        db.Quiz.Add(q);
        await db.SaveChangesAsync();

        var svc = new LocalQuizService(db);
        await svc.DefinirBackendQuizIdSiAbsenteAsync(q.Id, 42L);

        var re = await db.Quiz.FirstAsync();
        Assert.Equal(42L, re.BackendQuizIdPublicationWeb);
        Assert.Equal(42L, re.BackendQuizId);
    }

    [Fact]
    public async Task DefinirBackendQuizIdQrSiAbsenteAsync_remplit_si_absent()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var q = new QuizLocal { Titre = "T", Difficulte = "Moyen", NombreQuestions = 1 };
        db.Quiz.Add(q);
        await db.SaveChangesAsync();

        var svc = new LocalQuizService(db);
        await svc.DefinirBackendQuizIdQrSiAbsenteAsync(q.Id, 77L);

        var re = await db.Quiz.FirstAsync();
        Assert.Equal(77L, re.BackendQuizIdQr);
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
