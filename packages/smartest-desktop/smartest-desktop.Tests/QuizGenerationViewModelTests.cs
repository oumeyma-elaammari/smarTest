using System.Net.Http;
using System.Reflection;
using Moq;
using smartest_desktop.Helpers;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Services;
using smartest_desktop.ViewModels;
using Xunit;

namespace smartest_desktop.Tests;

public class QuizGenerationViewModelTests
{
    private static string JsonTroisQuestions =>
        """
        [
          {"enonce":"Q1","optionA":"a","optionB":"b","optionC":"c","optionD":"d","reponseCorrecte":"A","explication":"e"},
          {"enonce":"Q2","optionA":"a","optionB":"b","optionC":"c","optionD":"d","reponseCorrecte":"B","explication":"e"},
          {"enonce":"Q3","optionA":"a","optionB":"b","optionC":"c","optionD":"d","reponseCorrecte":"C","explication":"e"}
        ]
        """;

    [Fact]
    public void Etat_initial_cours_vide_pas_erreur()
    {
        using var db = TestDbFactory.CreateInMemory();
        var groq = new Mock<IGroqGenerationClient>(MockBehavior.Strict);
        var vm = new QuizGenerationViewModel(db, groq.Object);

        Assert.False(vm.HasCours);
        Assert.False(vm.HasError);
        Assert.Equal(string.Empty, vm.ErrorMessage);
    }

    [Fact]
    public async Task Generation_declenchee_parse_questions_et_succes()
    {
        using var db = TestDbFactory.CreateInMemory();
        var groq = new Mock<IGroqGenerationClient>();
        groq.Setup(x => x.GenererAvecRetryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<double>()))
            .ReturnsAsync(JsonTroisQuestions);

        var vm = new QuizGenerationViewModel(db, groq.Object)
        {
            ContenuCours = new string('x', 200),
            TitreCours = "Mon cours",
            TitreQuiz = "Quiz test",
            NombreQuestions = 3
        };

        List<QuestionQCM>? recu = null;
        vm.QuizGenereAvecSucces += (qs, _, _, _, _, _) => recu = qs;

        var cmd = (RelayCommand)vm.GenererCommand;
        cmd.Execute(null);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (recu == null && DateTime.UtcNow < deadline && vm.IsGenerating)
            await Task.Delay(50);

        Assert.False(vm.HasError);
        Assert.NotNull(recu);
        Assert.Equal(3, recu!.Count);
    }

    [Fact]
    public async Task Erreur_reseau_Groq_remplit_ErrorMessage()
    {
        using var db = TestDbFactory.CreateInMemory();
        var groq = new Mock<IGroqGenerationClient>();
        groq.Setup(x => x.GenererAvecRetryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<double>()))
            .ThrowsAsync(new HttpRequestException("timeout"));

        var vm = new QuizGenerationViewModel(db, groq.Object)
        {
            ContenuCours = new string('y', 200),
            TitreCours = "C",
            NombreQuestions = 3
        };

        ((RelayCommand)vm.GenererCommand).Execute(null);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (vm.IsGenerating && DateTime.UtcNow < deadline)
            await Task.Delay(50);

        Assert.True(vm.HasError);
        Assert.Contains("Impossible de contacter le serveur", vm.ErrorMessage);
    }

    [Fact]
    public async Task Annulation_utilisateur_arrete_sans_erreur()
    {
        using var db = TestDbFactory.CreateInMemory();
        var tcs = new TaskCompletionSource<bool>();
        var groq = new Mock<IGroqGenerationClient>();
        groq.Setup(x => x.GenererAvecRetryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<double>()))
            .Returns<string, CancellationToken, double>(async (_, ct, _) =>
            {
                await Task.Delay(5000, ct);
                return JsonTroisQuestions;
            })
            .Callback(() => tcs.TrySetResult(true));

        var vm = new QuizGenerationViewModel(db, groq.Object)
        {
            ContenuCours = new string('z', 200),
            TitreCours = "C",
            NombreQuestions = 3
        };

        ((RelayCommand)vm.GenererCommand).Execute(null);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

        var ctsField = typeof(QuizGenerationViewModel).GetField("_cts", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(ctsField);
        var cts = (CancellationTokenSource?)ctsField!.GetValue(vm);
        Assert.NotNull(cts);
        cts!.Cancel();

        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (vm.IsGenerating && DateTime.UtcNow < deadline)
            await Task.Delay(50);

        Assert.False(vm.IsGenerating);
        Assert.False(vm.HasError);
        Assert.Contains("annul", vm.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Groq_json_vide_leve_message_erreur()
    {
        using var db = TestDbFactory.CreateInMemory();
        var groq = new Mock<IGroqGenerationClient>();
        groq.Setup(x => x.GenererAvecRetryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<double>()))
            .ReturnsAsync("[]");

        var vm = new QuizGenerationViewModel(db, groq.Object)
        {
            ContenuCours = new string('w', 200),
            TitreCours = "C",
            NombreQuestions = 3
        };

        ((RelayCommand)vm.GenererCommand).Execute(null);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (vm.IsGenerating && DateTime.UtcNow < deadline)
            await Task.Delay(50);

        Assert.True(vm.HasError);
        Assert.Contains("JSON", vm.ErrorMessage);
    }
}
