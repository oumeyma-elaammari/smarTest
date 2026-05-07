using System.Net;
using System.Text;
using Moq;
using Moq.Protected;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Exceptions;
using smartest_desktop.Services;
using Xunit;

namespace smartest_desktop.Tests;

public class QuizWebPublicationApiServiceTests
{
    private static Mock<HttpMessageHandler> MockHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> respond)
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) => respond(req, ct));
        return mock;
    }

    private static HttpClient ClientFromMock(Mock<HttpMessageHandler> mock) =>
        new(mock.Object) { BaseAddress = new Uri("http://localhost:8081") };

    [Fact]
    public async Task PostPublicationWebAsync_200_ne_leve_pas()
    {
        var handler = MockHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        var svc = new QuizWebPublicationApiService(createClientOverride: _ => ClientFromMock(handler));

        await svc.PostPublicationWebAsync("tok", 1L, new[] { "a@test.fr" });

        handler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(m =>
                m.Method == HttpMethod.Post &&
                m.RequestUri!.AbsolutePath.Contains("/publication-web")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task PostPublicationWebAsync_401_leve_SmartestApiException()
    {
        var handler = MockHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent("\"no\"") });
        var svc = new QuizWebPublicationApiService(createClientOverride: _ => ClientFromMock(handler));

        var ex = await Assert.ThrowsAsync<SmartestApiException>(() =>
            svc.PostPublicationWebAsync("tok", 1L, Array.Empty<string>()));

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
    }

    [Fact]
    public async Task PostPublicationWebAsync_404_leve_SmartestApiException()
    {
        var handler = MockHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{}") });
        var svc = new QuizWebPublicationApiService(createClientOverride: _ => ClientFromMock(handler));

        var ex = await Assert.ThrowsAsync<SmartestApiException>(() =>
            svc.PostPublicationWebAsync("tok", 99L, Array.Empty<string>()));

        Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
    }

    [Fact]
    public async Task PostPublicationWebAsync_422_validation_leve_SmartestApiException_avec_champs()
    {
        const string body = """{"emails":"liste invalide"}""";
        var handler = MockHandler((_, _) =>
            new HttpResponseMessage((HttpStatusCode)422) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        var svc = new QuizWebPublicationApiService(createClientOverride: _ => ClientFromMock(handler));

        var ex = await Assert.ThrowsAsync<SmartestApiException>(() =>
            svc.PostPublicationWebAsync("tok", 1L, Array.Empty<string>()));

        Assert.Equal((HttpStatusCode)422, ex.StatusCode);
        Assert.Contains("emails", ex.ValidationErrors.Keys);
    }

    [Fact]
    public async Task PostPublicationWebAsync_reseau_leve_SmartestNetworkException()
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));
        var svc = new QuizWebPublicationApiService(createClientOverride: _ =>
            new HttpClient(mock.Object) { BaseAddress = new Uri("http://localhost:8081") });

        var ex = await Assert.ThrowsAsync<SmartestNetworkException>(() =>
            svc.PostPublicationWebAsync("tok", 1L, Array.Empty<string>()));

        Assert.Contains("serveur", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateQuizAsync_id_parse_correctement()
    {
        var handler = MockHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":42,"titre":"x"}""")
            });
        var svc = new QuizWebPublicationApiService(createClientOverride: _ => ClientFromMock(handler));

        var id = await svc.CreateQuizAsync("tok", "Titre", 30, 7L);

        Assert.Equal(42L, id);
    }

    [Fact]
    public async Task GetProfesseurIdAsync_parse_id()
    {
        var handler = MockHandler((_, _) =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"id":99,"nom":"N"}""")
            });
        var svc = new QuizWebPublicationApiService(createClientOverride: _ => ClientFromMock(handler));

        var id = await svc.GetProfesseurIdAsync("tok");

        Assert.Equal(99L, id);
    }

    [Fact]
    public async Task DeleteQuizAsync_reseau_leve_SmartestNetworkException()
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("offline"));
        var svc = new QuizWebPublicationApiService(createClientOverride: _ =>
            new HttpClient(mock.Object) { BaseAddress = new Uri("http://localhost:8081") });

        await Assert.ThrowsAsync<SmartestNetworkException>(() =>
            svc.DeleteQuizAsync("tok", 1L));
    }

    [Fact]
    public async Task PostPublicationWebAsync_serialise_questions()
    {
        HttpRequestMessage? captured = null;
        var handler = MockHandler((req, _) =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
        });
        var svc = new QuizWebPublicationApiService(createClientOverride: _ => ClientFromMock(handler));
        var q = new QuestionLocale
        {
            Enonce = "E?",
            OptionA = "a",
            OptionB = "b",
            OptionC = "c",
            OptionD = "d",
            ReponseCorrecte = "A",
            Explication = "xp",
            Difficulte = "Moyen",
            Type = "QCM"
        };

        await svc.PostPublicationWebAsync("tok", 5L, new[] { "s@test.fr" }, new[] { q });

        Assert.NotNull(captured);
        var json = await captured!.Content!.ReadAsStringAsync();
        Assert.Contains("E?", json);
        Assert.Contains("s@test.fr", json);
    }
}
