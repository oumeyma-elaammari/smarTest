using Microsoft.EntityFrameworkCore;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Services;
using Xunit;

namespace smartest_desktop.Tests.Services;

public class LocalQuizServiceTests
{
    [Fact]
    public async Task GetAllAsync_retourne_quiz()
    {
        // GIVEN
        await using var db = TestDbFactory.CreateInMemoryContext();
        db.Quiz.Add(new QuizLocal { Titre = "Q", Difficulte = "Moyen", NombreQuestions = 1 });
        await db.SaveChangesAsync();
        var svc = new LocalQuizService(db);

        // WHEN
        var list = await svc.GetAllAsync();

        // THEN
        Assert.Single(list);
        Assert.Equal("Q", list[0].Titre);
    }

    [Fact]
    public async Task GetByIdAsync_retourne_null_si_absent()
    {
        await using var db = TestDbFactory.CreateInMemoryContext();
        var svc = new LocalQuizService(db);

        var q = await svc.GetByIdAsync(42);

        Assert.Null(q);
    }

    [Fact]
    public async Task AjouterAsync_SupprimerAsync_cycle()
    {
        await using var db = TestDbFactory.CreateInMemoryContext();
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
        await using var db = TestDbFactory.CreateInMemoryContext();
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
        await using var db = TestDbFactory.CreateInMemoryContext();
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
        await using var db = TestDbFactory.CreateInMemoryContext();
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
        await using var db = TestDbFactory.CreateInMemoryContext();
        var svc = new LocalQuizService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.MettreAJourPublicationWebLocaleAsync(1, 10L, "[]", "Publié"));
    }

    [Fact]
    public async Task MettreAJourContenuAsync_introuvable_leve()
    {
        await using var db = TestDbFactory.CreateInMemoryContext();
        var svc = new LocalQuizService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.MettreAJourContenuAsync(9, "t", "Moyen", "c", "BROUILLON", Array.Empty<QuestionLocale>()));
    }

    [Fact]
    public async Task QueryQuizzesAsync_Pagination_ReturnsSlice()
    {
        // GIVEN : 5 quiz triés par titre
        await using var db = TestDbFactory.CreateInMemoryContext();
        for (int i = 0; i < 5; i++)
        {
            db.Quiz.Add(new QuizLocal
            {
                Titre = $"Z{i}",
                Difficulte = "Moyen",
                NombreQuestions = 1,
                Description = string.Empty,
                DateCreation = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        await db.SaveChangesAsync();
        var svc = new LocalQuizService(db);

        // WHEN : page 2, taille 2, tri titre ascendant
        var page = await svc.QueryQuizzesAsync(null, QuizListSort.Titre, ordreDescendant: false, skip: 2, take: 2);

        // THEN
        Assert.Equal(2, page.Count);
        Assert.Equal("Z2", page[0].Titre);
        Assert.Equal("Z3", page[1].Titre);
    }

    [Fact]
    public async Task QueryQuizzesAsync_SearchTexteLibre_FiltreTitreEtDescription()
    {
        await using var db = TestDbFactory.CreateInMemoryContext();
        db.Quiz.Add(new QuizLocal { Titre = "Algèbre", Difficulte = "Moyen", NombreQuestions = 2, Description = "chapitre 1" });
        db.Quiz.Add(new QuizLocal { Titre = "Géométrie", Difficulte = "Moyen", NombreQuestions = 1, Description = "angles" });
        await db.SaveChangesAsync();
        var svc = new LocalQuizService(db);

        var res = await svc.QueryQuizzesAsync("Alg", QuizListSort.DateCreation, true, 0, 10);

        Assert.Single(res);
        Assert.Equal("Algèbre", res[0].Titre);
    }

    [Fact]
    public async Task QueryQuizzesAsync_TriNombreQuestions_Desc()
    {
        await using var db = TestDbFactory.CreateInMemoryContext();
        db.Quiz.Add(new QuizLocal { Titre = "A", Difficulte = "Moyen", NombreQuestions = 1, Description = "" });
        db.Quiz.Add(new QuizLocal { Titre = "B", Difficulte = "Moyen", NombreQuestions = 9, Description = "" });
        db.Quiz.Add(new QuizLocal { Titre = "C", Difficulte = "Moyen", NombreQuestions = 5, Description = "" });
        await db.SaveChangesAsync();
        var svc = new LocalQuizService(db);

        var res = await svc.QueryQuizzesAsync(null, QuizListSort.NombreQuestions, true, 0, 10);

        Assert.Equal(9, res[0].NombreQuestions);
        Assert.Equal(5, res[1].NombreQuestions);
        Assert.Equal(1, res[2].NombreQuestions);
    }

    [Fact]
    public async Task MarquerServeurOuQrPotentiellementCreeAsync_DefinitHorodateUneSeuleFois()
    {
        await using var db = TestDbFactory.CreateInMemoryContext();
        var q = new QuizLocal { Titre = "Q", Difficulte = "Moyen", NombreQuestions = 0, Description = "" };
        db.Quiz.Add(q);
        await db.SaveChangesAsync();

        var svc = new LocalQuizService(db);
        await svc.MarquerServeurOuQrPotentiellementCreeAsync(q.Id);

        var re = await db.Quiz.SingleAsync();
        Assert.NotNull(re.ServeurOuQrToucheUtc);

        var garde = re.ServeurOuQrToucheUtc;
        await Task.Delay(5);
        await svc.MarquerServeurOuQrPotentiellementCreeAsync(q.Id);
        re = await db.Quiz.SingleAsync();
        Assert.Equal(garde, re.ServeurOuQrToucheUtc);
    }
}
