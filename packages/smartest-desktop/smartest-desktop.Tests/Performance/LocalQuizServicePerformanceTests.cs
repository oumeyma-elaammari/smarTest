using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Services;
using Xunit;

namespace smartest_desktop.Tests.Performance;

public class LocalQuizServicePerformanceTests
{
    [Fact]
    public async Task Insertion_10000Quiz_InMemory_SaveChangesUnique_Under3Seconds()
    {
        // GIVEN : insertion en masse (un seul SaveChanges) — reflète la charge « quiz » en mémoire.
        // Note : <see cref="LocalQuizService.AjouterAsync"/> fait un SaveChanges par quiz (N fois plus lent).
        await using var db = TestDbFactory.CreateInMemoryContext();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < 10_000; i++)
        {
            db.Quiz.Add(new QuizLocal
            {
                Titre = $"Perf-{i}",
                Difficulte = "Moyen",
                NombreQuestions = 1,
                Description = "d"
            });
        }

        await db.SaveChangesAsync();
        sw.Stop();

        Assert.InRange(sw.Elapsed.TotalSeconds, 0, 3);
        Assert.Equal(10_000, db.Quiz.Count());
    }

    [Fact]
    public async Task QueryQuizzesAsync_RechercheSur10000_Under100ms()
    {
        await using var db = TestDbFactory.CreateInMemoryContext();
        for (int i = 0; i < 10_000; i++)
        {
            db.Quiz.Add(new QuizLocal
            {
                Titre = $"Titre-{i}",
                Difficulte = "Moyen",
                NombreQuestions = 1,
                Description = i == 5555 ? "cible-unique-xyz" : "x"
            });
        }

        await db.SaveChangesAsync();
        var svc = new LocalQuizService(db);

        var sw = Stopwatch.StartNew();
        var res = await svc.QueryQuizzesAsync("cible-unique-xyz", QuizListSort.DateCreation, true, 0, 20);
        sw.Stop();

        Assert.Single(res);
        Assert.InRange(sw.Elapsed.TotalMilliseconds, 0, 400);
    }
}
