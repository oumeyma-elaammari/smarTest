using Moq;
using smartest_desktop.Helpers;
using smartest_desktop.Services;
using smartest_desktop.ViewModels;
using Xunit;

namespace smartest_desktop.Tests.ViewModels;

/// <summary>Tests sur <see cref="ExamenGenerationViewModel"/> (génération d'examen).</summary>
public class ExamGenerationViewModelTests
{
    private static string JsonTroisQcm =>
        """
        [
          {"type":"QCM","enonce":"E1","optionA":"a","optionB":"b","optionC":"c","optionD":"d","reponseCorrecte":"A","explication":"x"},
          {"type":"QCM","enonce":"E2","optionA":"a","optionB":"b","optionC":"c","optionD":"d","reponseCorrecte":"B","explication":"x"},
          {"type":"QCM","enonce":"E3","optionA":"a","optionB":"b","optionC":"c","optionD":"d","reponseCorrecte":"C","explication":"x"}
        ]
        """;

    [Fact]
    public async Task GenererCommand_ShortCourseThreeQuestions_SuccessAndRaisesEvent()
    {
        // GIVEN : ≤4 questions et cours court → branche sans batching (pas de Dispatcher WPF)
        await using var db = TestDbFactory.CreateInMemoryContext();
        var groq = new Mock<IGroqGenerationClient>();
        groq.Setup(x => x.GenererAvecRetryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<double>()))
            .ReturnsAsync(JsonTroisQcm);

        var vm = new ExamenGenerationViewModel(db, groq.Object)
        {
            ContenuCours = new string('x', 200),
            TitreCours = "Cours",
            TitreExamen = "Examen test",
            NbQCM = 3,
            NbVF = 0,
            NbCheckbox = 0,
            NbRedaction = 0
        };

        List<QuestionExamen>? recu = null;
        vm.ExamenGenereAvecSucces += (qs, _, _, _, _) => recu = qs;

        // WHEN
        ((RelayCommand)vm.GenererCommand).Execute(null);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (recu == null && DateTime.UtcNow < deadline && vm.IsGenerating)
            await Task.Delay(50);

        // THEN
        Assert.False(vm.HasError);
        Assert.NotNull(recu);
        Assert.Equal(3, recu!.Count);
    }

    [Fact]
    public async Task GenererCommand_GroqHttpError_SetsErrorMessage()
    {
        // GIVEN
        await using var db = TestDbFactory.CreateInMemoryContext();
        var groq = new Mock<IGroqGenerationClient>();
        groq.Setup(x => x.GenererAvecRetryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<double>()))
            .ThrowsAsync(new System.Net.Http.HttpRequestException("503 unavailable"));

        var vm = new ExamenGenerationViewModel(db, groq.Object)
        {
            ContenuCours = new string('y', 200),
            TitreCours = "C",
            NbQCM = 3
        };

        // WHEN
        ((RelayCommand)vm.GenererCommand).Execute(null);
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (vm.IsGenerating && DateTime.UtcNow < deadline)
            await Task.Delay(50);

        // THEN
        Assert.True(vm.HasError);
        Assert.NotEmpty(vm.ErrorMessage);
    }

    [Fact]
    public async Task GenererCommand_WhileRunning_IsGeneratingTrue()
    {
        // GIVEN
        await using var db = TestDbFactory.CreateInMemoryContext();
        var tcs = new TaskCompletionSource<bool>();
        var groq = new Mock<IGroqGenerationClient>();
        groq.Setup(x => x.GenererAvecRetryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<double>()))
            .Returns<string, CancellationToken, double>(async (_, ct, _) =>
            {
                await Task.Delay(3000, ct);
                return JsonTroisQcm;
            })
            .Callback(() => tcs.TrySetResult(true));

        var vm = new ExamenGenerationViewModel(db, groq.Object)
        {
            ContenuCours = new string('z', 200),
            TitreCours = "C",
            NbQCM = 3
        };

        // WHEN
        ((RelayCommand)vm.GenererCommand).Execute(null);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // THEN : état chargement observé pendant l'appel long
        Assert.True(vm.IsGenerating);

        var ctsField = typeof(ExamenGenerationViewModel).GetField("_cts", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var cts = (CancellationTokenSource?)ctsField?.GetValue(vm);
        Assert.NotNull(cts);
        cts!.Cancel();

        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (vm.IsGenerating && DateTime.UtcNow < deadline)
            await Task.Delay(50);
    }

    [Fact]
    public async Task AnnulerGenerationCommand_CancelsGeneration_StopsWithoutError()
    {
        // GIVEN
        await using var db = TestDbFactory.CreateInMemoryContext();
        var tcs = new TaskCompletionSource<bool>();
        var groq = new Mock<IGroqGenerationClient>();
        groq.Setup(x => x.GenererAvecRetryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>(), It.IsAny<double>()))
            .Returns<string, CancellationToken, double>(async (_, ct, _) =>
            {
                await Task.Delay(8000, ct);
                return JsonTroisQcm;
            })
            .Callback(() => tcs.TrySetResult(true));

        var vm = new ExamenGenerationViewModel(db, groq.Object)
        {
            ContenuCours = new string('w', 200),
            TitreCours = "C",
            NbQCM = 3
        };

        ((RelayCommand)vm.GenererCommand).Execute(null);
        await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // WHEN
        ((RelayCommand)vm.AnnulerGenerationCommand).Execute(null);

        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (vm.IsGenerating && DateTime.UtcNow < deadline)
            await Task.Delay(50);

        // THEN
        Assert.False(vm.IsGenerating);
    }
}
