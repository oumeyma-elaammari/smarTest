using System.Net;
using System.Text;
using Moq;
using Moq.Protected;
using smartest_desktop.Models;
using smartest_desktop.Services;
using Xunit;

namespace smartest_desktop.Tests.Services;

public class AuthServiceTests
{
    private static Mock<HttpMessageHandler> HandlerReturning(HttpResponseMessage response)
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        return mock;
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsAuth()
    {
        // GIVEN : serveur renvoie un JWT professeur
        var json = """{"token":"jwt","role":"PROFESSEUR","nom":"N","email":"e@e.fr"}""";
        var mock = HandlerReturning(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var http = new HttpClient(mock.Object) { BaseAddress = new Uri("http://localhost:8081") };
        var auth = new AuthService(http);

        // WHEN
        var (user, err) = await auth.LoginAsync("e@e.fr", "secret");

        // THEN
        Assert.Null(err);
        Assert.NotNull(user);
        Assert.Equal("jwt", user!.Token);
        Assert.Equal("PROFESSEUR", user.Role);
    }

    [Fact]
    public async Task RefreshAccessTokenAsync_ValidToken_ReturnsNewAuth()
    {
        // GIVEN
        var json = """{"token":"new-jwt","role":"PROFESSEUR","nom":"N","email":"e@e.fr"}""";
        var mock = HandlerReturning(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var http = new HttpClient(mock.Object) { BaseAddress = new Uri("http://localhost:8081") };
        var auth = new AuthService(http);

        // WHEN
        var (user, err) = await auth.RefreshAccessTokenAsync("old.jwt.token");

        // THEN
        Assert.Null(err);
        Assert.NotNull(user);
        Assert.Equal("new-jwt", user!.Token);
    }

    [Fact]
    public async Task LoginAsync_Unauthorized_ReturnsError()
    {
        // GIVEN
        var mock = HandlerReturning(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("\"password incorrect\"")
        });
        var http = new HttpClient(mock.Object) { BaseAddress = new Uri("http://localhost:8081") };
        var auth = new AuthService(http);

        // WHEN
        var (user, err) = await auth.LoginAsync("e@e.fr", "wrong");

        // THEN
        Assert.Null(user);
        Assert.Contains("Mot de passe", err);
    }

    [Fact]
    public async Task LoginAsync_NetworkFailure_ReturnsUnreachableMessage()
    {
        // GIVEN
        var m = new Mock<HttpMessageHandler>();
        m.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("no route"));
        var http = new HttpClient(m.Object) { BaseAddress = new Uri("http://localhost:8081") };
        var auth = new AuthService(http);

        // WHEN
        var (user, err) = await auth.LoginAsync("a@b.c", "x");

        // THEN
        Assert.Null(user);
        Assert.Contains("Connexion impossible", err, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SauvegarderSession_AfterLogin_StoresEncryptedTokenLocally()
    {
        // GIVEN : réponse login simulée
        var auth = new AuthResponse
        {
            Token = "jwt-from-login",
            Nom = "Prof",
            Email = "p@test.fr",
            Role = "PROFESSEUR"
        };

        // WHEN / THEN : persistance locale (même flux que LoginViewModel après succès)
        await using (var db = TestDbFactory.CreateInMemoryContext())
        {
            var session = new SessionService(db);
            session.SauvegarderSession(auth);

            var loaded = session.ChargerSession();
            Assert.NotNull(loaded);
            Assert.Equal("jwt-from-login", loaded!.TokenChiffre);
            Assert.Equal("p@test.fr", loaded.Email);
        }
    }
}
