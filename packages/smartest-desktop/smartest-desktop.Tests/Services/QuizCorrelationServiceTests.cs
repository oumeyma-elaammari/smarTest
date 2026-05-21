using smartest_desktop.Models;
using smartest_desktop.Services;
using Xunit;

namespace smartest_desktop.Tests.Services;

/// <summary>Corrélation titre / nb questions ↔ id backend (<see cref="MesQuizCorrelation"/>).</summary>
public class QuizCorrelationServiceTests
{
    [Fact]
    public void TryResolveQuizId_TitleAndCountMatch_ReturnsBackendId()
    {
        // GIVEN : liste serveur et quiz local équivalent (titre + nb questions)
        var liste = new List<QuizProfesseurListeItem>
        {
            new() { Id = 100, Titre = "Math", NombreQuestions = 3 },
            new() { Id = 101, Titre = "Physique", NombreQuestions = 1 },
        };

        // WHEN / THEN
        Assert.Equal(100L, MesQuizCorrelation.TryResolveQuizId(liste, "  math ", 3));
    }

    [Fact]
    public void TryResolveQuizId_DuplicateTitleAndCount_ThrowsConflict()
    {
        // GIVEN : conflit — deux entrées serveur indistinguables
        var liste = new List<QuizProfesseurListeItem>
        {
            new() { Id = 1, Titre = "A", NombreQuestions = 2 },
            new() { Id = 2, Titre = "A", NombreQuestions = 2 },
        };

        // WHEN / THEN
        Assert.Throws<InvalidOperationException>(() =>
            MesQuizCorrelation.TryResolveQuizId(liste, "A", 2));
    }

    [Fact]
    public void TryResolveQuizId_BackendListEmpty_ReturnsNull()
    {
        // GIVEN : quiz « supprimé côté backend » → liste vide (plus d'id correspondant)
        var liste = new List<QuizProfesseurListeItem>();

        // WHEN / THEN
        Assert.Null(MesQuizCorrelation.TryResolveQuizId(liste, "Absent", 5));
    }
}
