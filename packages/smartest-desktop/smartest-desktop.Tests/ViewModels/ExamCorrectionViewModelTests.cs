using System.Net;
using System.Text;
using Moq;
using Moq.Protected;
using smartest_desktop.Helpers;
using smartest_desktop.Services;
using smartest_desktop.ViewModels;
using Xunit;

namespace smartest_desktop.Tests.ViewModels;

/// <summary>Tests sur <see cref="ExamenCorrectionEtudiantViewModel"/> (correction examen).</summary>
public class ExamCorrectionViewModelTests
{
    private static ExamenWebPublicationApiService ApiWithHandler(Mock<HttpMessageHandler> handler) =>
        new(createClientOverride: _ => new HttpClient(handler.Object) { BaseAddress = new Uri("http://localhost:8081") });

    [Fact]
    public async Task ChargerAsync_ValidPayload_LoadsLinesAndScore()
    {
        // GIVEN
        var body = """
                   {"baremeReference":20,"lignes":[
                     {"questionId":1,"enonce":"Q1","typeQuestion":"QCM","reponseEtudiantLibelle":"A","scorePartiel":2,"noteFinaleLigne":null,"pointsQuestionMax":5,"corrigeParIa":false,"groqStatut":null},
                     {"questionId":2,"enonce":"Q2","typeQuestion":"QCM","reponseEtudiantLibelle":"B","scorePartiel":3,"noteFinaleLigne":null,"pointsQuestionMax":5,"corrigeParIa":false,"groqStatut":null}
                   ]}
                   """;
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                if (req.Method == HttpMethod.Get)
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

        var api = ApiWithHandler(mock);
        var vm = new ExamenCorrectionEtudiantViewModel(1, "Examen", 9, "tok", api);

        // WHEN
        await vm.ChargerAsync();

        // THEN
        Assert.False(vm.AfficherErreur);
        Assert.Equal(2, vm.Lignes.Count);
        Assert.Equal(5.0, vm.NoteTotale, 3);
        Assert.Equal(20, vm.Bareme);
    }

    [Fact]
    public async Task ChargerAsync_NotFound_SetsError()
    {
        // GIVEN : quiz / copie introuvable côté API
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{}") });

        var vm = new ExamenCorrectionEtudiantViewModel(99, "Examen", 1, "tok", ApiWithHandler(mock));

        // WHEN
        await vm.ChargerAsync();

        // THEN
        Assert.True(vm.AfficherErreur);
        Assert.NotNull(vm.Erreur);
    }

    [Fact]
    public async Task ValiderAsync_ValidNotes_CallsApiAndCompletes()
    {
        // GIVEN : GET corrections puis POST valider + synchro OK
        var json = """{"baremeReference":20,"lignes":[{"questionId":1,"enonce":"Q","typeQuestion":"QCM","reponseEtudiantLibelle":"A","scorePartiel":5,"noteFinaleLigne":null,"pointsQuestionMax":10,"corrigeParIa":false,"groqStatut":null}]}""";
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                var path = req.RequestUri?.AbsolutePath ?? "";
                if (req.Method == HttpMethod.Get && path.Contains("corrections"))
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
                if (req.Method == HttpMethod.Post && path.Contains("valider-corrections"))
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") };
                if (req.Method == HttpMethod.Post && path.Contains("synchroniser-note"))
                    return new HttpResponseMessage(HttpStatusCode.OK);
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            });

        var vm = new ExamenCorrectionEtudiantViewModel(1, "Examen", 2, "tok", ApiWithHandler(mock));
        await vm.ChargerAsync();
        vm.Lignes[0].Note = 8;

        var tcs = new TaskCompletionSource<bool>();
        vm.DemandeFermetureSucces += () => tcs.TrySetResult(true);

        // WHEN
        ((RelayCommand)vm.ValiderCommand).Execute(null);

        // THEN
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(vm.Busy);
    }
}
