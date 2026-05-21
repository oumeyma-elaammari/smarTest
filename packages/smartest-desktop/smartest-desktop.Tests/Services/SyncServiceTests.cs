using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.Protected;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Exceptions;
using smartest_desktop.Services;
using Xunit;

namespace smartest_desktop.Tests.Services;

public class SyncServiceTests
{
    private static QuizWebPublicationApiService ApiMock(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> respond)
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) => respond(req, ct));
        return new QuizWebPublicationApiService(
            "http://localhost:8081",
            _ => new HttpClient(mock.Object) { BaseAddress = new Uri("http://localhost:8081") });
    }

    [Fact]
    public async Task PousserQuizVersBackendAsync_CreeSurServeur_MetAJourIdLocal()
    {
        // GIVEN
        await using var db = TestDbFactory.CreateInMemoryContext();
        db.Quiz.Add(new QuizLocal { Titre = "SyncMe", Difficulte = "Moyen", NombreQuestions = 0, Description = "" });
        await db.SaveChangesAsync();
        var local = new LocalQuizService(db);

        var api = ApiMock((req, _) =>
        {
            var path = req.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("/api/professeur/profil"))
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"id":7}""", Encoding.UTF8, "application/json")
                };
            if (path == "/api/quizs" && req.Method == HttpMethod.Post)
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"id":555,"professeurId":7}""", Encoding.UTF8, "application/json")
                };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var sync = new SyncService(local, api);

        // WHEN
        long id = await sync.PousserQuizVersBackendAsync(1, "fake-token");

        // THEN
        Assert.Equal(555L, id);
        var q = await db.Quiz.SingleAsync();
        Assert.Equal(555L, q.BackendQuizIdPublicationWeb);
    }

    [Fact]
    public async Task PousserQuizVersBackendAsync_Conflit409_SmartestApiException()
    {
        await using var db = TestDbFactory.CreateInMemoryContext();
        db.Quiz.Add(new QuizLocal { Titre = "Dup", Difficulte = "Moyen", NombreQuestions = 0, Description = "" });
        await db.SaveChangesAsync();
        var local = new LocalQuizService(db);

        var api = ApiMock((req, _) =>
        {
            var path = req.RequestUri?.AbsolutePath ?? "";
            if (path.Contains("/api/professeur/profil"))
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""{"id":1}""", Encoding.UTF8, "application/json")
                };
            if (path == "/api/quizs" && req.Method == HttpMethod.Post)
                return new HttpResponseMessage(HttpStatusCode.Conflict)
                {
                    Content = new StringContent("\"doublon\"", Encoding.UTF8, "application/json")
                };
            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var sync = new SyncService(local, api);

        var ex = await Assert.ThrowsAsync<SmartestApiException>(() => sync.PousserQuizVersBackendAsync(1, "tok"));
        Assert.True(SyncService.EstConflitCreation(ex));
    }

    [Fact]
    public async Task ReconcilierApresSuppressionServeurAsync_404EffaceLiensLocaux()
    {
        await using var db = TestDbFactory.CreateInMemoryContext();
        db.Quiz.Add(new QuizLocal
        {
            Titre = "Orphelin",
            Difficulte = "Moyen",
            NombreQuestions = 0,
            Description = "",
            BackendQuizId = 999,
            BackendQuizIdPublicationWeb = 999
        });
        await db.SaveChangesAsync();
        var local = new LocalQuizService(db);

        var api = ApiMock((req, _) =>
        {
            if (req.RequestUri?.AbsolutePath?.Contains("/api/quizs/999") == true)
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });

        var sync = new SyncService(local, api);

        await sync.ReconcilierApresSuppressionServeurAsync(1, "tok");

        var q = await db.Quiz.SingleAsync();
        Assert.Null(q.BackendQuizId);
        Assert.Null(q.BackendQuizIdPublicationWeb);
    }
}
