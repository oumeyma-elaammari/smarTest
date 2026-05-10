using smartest_desktop.Data.LocalEntities;
using Microsoft.EntityFrameworkCore;
using smartest_desktop.Models;
using smartest_desktop.Services;
using Xunit;

namespace smartest_desktop.Tests;

public sealed class MesQuizCorrelationTests
{
    [Fact]
    public void TryResolveQuizId_titre_et_nombre_exacts()
    {
        var liste = new List<QuizProfesseurListeItem>
        {
            new() { Id = 100, Titre = "Math", NombreQuestions = 3 },
            new() { Id = 101, Titre = "Physique", NombreQuestions = 1 },
        };

        Assert.Equal(
            100L,
            MesQuizCorrelation.TryResolveQuizId(liste, "Math", 3));

        Assert.Equal(
            100L,
            MesQuizCorrelation.TryResolveQuizId(liste, "  math ", 3));
    }

    [Fact]
    public void TryResolveQuizId_pli_titre_seul_si_nb_differents()
    {
        var liste = new List<QuizProfesseurListeItem>
        {
            new() { Id = 200, Titre = "Chimie", NombreQuestions = 0 },
        };

        Assert.Equal(
            200L,
            MesQuizCorrelation.TryResolveQuizId(liste, "Chimie", 5));
    }

    [Fact]
    public void TryResolveQuizId_plusieurs_meme_titre_meme_nb_leve()
    {
        var liste = new List<QuizProfesseurListeItem>
        {
            new() { Id = 1, Titre = "A", NombreQuestions = 2 },
            new() { Id = 2, Titre = "A", NombreQuestions = 2 },
        };

        Assert.Throws<InvalidOperationException>(() =>
            MesQuizCorrelation.TryResolveQuizId(liste, "A", 2));
    }

    [Fact]
    public void TryResolveQuizId_plusieurs_meme_titre_seul_leve_apres_fallback()
    {
        var liste = new List<QuizProfesseurListeItem>
        {
            new() { Id = 1, Titre = "A", NombreQuestions = 1 },
            new() { Id = 2, Titre = "A", NombreQuestions = 2 },
        };

        Assert.Throws<InvalidOperationException>(() =>
            MesQuizCorrelation.TryResolveQuizId(liste, "A", 99));
    }

    [Fact]
    public void TryResolveQuizId_introuvable_renvoie_null()
    {
        var liste = new List<QuizProfesseurListeItem>
        {
            new() { Id = 1, Titre = "Rien", NombreQuestions = 1 },
        };

        Assert.Null(MesQuizCorrelation.TryResolveQuizId(liste, "Absent", 1));
        Assert.Null(MesQuizCorrelation.TryResolveQuizId(liste, "   ", 1));
        Assert.Null(MesQuizCorrelation.TryResolveQuizId(liste, string.Empty, 1));
    }

    [Fact]
    public async Task MarquerServeurOuQrPotentiellementCreeAsync_definit_horodate_une_seule_fois()
    {
        await using var db = TestDbFactory.CreateInMemory();
        var q = new QuizLocal { Titre = "Q", Difficulte = "Moyen", NombreQuestions = 0 };
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
