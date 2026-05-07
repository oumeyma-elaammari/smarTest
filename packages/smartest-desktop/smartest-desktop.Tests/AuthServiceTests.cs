using System.Net;
using System.Text;
using Moq;
using Moq.Protected;
using smartest_desktop.Models;
using smartest_desktop.Services;
using Xunit;

namespace smartest_desktop.Tests;

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
    public async Task LoginAsync_succes_retourne_auth()
    {
        var json = """{"token":"jwt","role":"PROFESSEUR","nom":"N","email":"e@e.fr"}""";
        var mock = HandlerReturning(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });
        var http = new HttpClient(mock.Object) { BaseAddress = new Uri("http://localhost:8081") };
        var auth = new AuthService(http);

        var (user, err) = await auth.LoginAsync("e@e.fr", "secret");

        Assert.Null(err);
        Assert.NotNull(user);
        Assert.Equal("jwt", user!.Token);
        Assert.Equal("PROFESSEUR", user.Role);
    }

    [Fact]
    public async Task LoginAsync_mauvais_mot_de_passe()
    {
        var mock = HandlerReturning(new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            Content = new StringContent("\"password incorrect\"")
        });
        var http = new HttpClient(mock.Object) { BaseAddress = new Uri("http://localhost:8081") };
        var auth = new AuthService(http);

        var (user, err) = await auth.LoginAsync("e@e.fr", "wrong");

        Assert.Null(user);
        Assert.Contains("Mot de passe", err);
    }

    [Fact]
    public async Task LoginAsync_reseau_inaccessible()
    {
        var m = new Mock<HttpMessageHandler>();
        m.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("no route"));
        var http = new HttpClient(m.Object) { BaseAddress = new Uri("http://localhost:8081") };
        var auth = new AuthService(http);

        var (user, err) = await auth.LoginAsync("a@b.c", "x");

        Assert.Null(user);
        Assert.Contains("serveur", err, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LoginAsync_reponse_malformee()
    {
        var mock = HandlerReturning(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{not-json", Encoding.UTF8, "application/json")
        });
        var http = new HttpClient(mock.Object) { BaseAddress = new Uri("http://localhost:8081") };
        var auth = new AuthService(http);

        var (user, err) = await auth.LoginAsync("a@b.c", "p");

        Assert.Null(user);
        Assert.Contains("illisible", err);
    }
}
