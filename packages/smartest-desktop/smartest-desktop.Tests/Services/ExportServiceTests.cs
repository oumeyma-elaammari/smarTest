using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Services;
using Xunit;

namespace smartest_desktop.Tests.Services;

public class ExportServiceTests
{
    [Fact]
    public void ExporterQuizVersXlsx_ContientMetadonneesEtQuestions()
    {
        // GIVEN
        var quiz = new QuizLocal
        {
            Titre = "Quiz export",
            Difficulte = "Moyen",
            Statut = "Validé",
            NombreQuestions = 1,
            Description = "d",
            Questions = new List<QuestionLocale>
            {
                new()
                {
                    Numero = 1,
                    Type = "QCM",
                    Enonce = "2+2 ?",
                    OptionA = "3",
                    OptionB = "4",
                    OptionC = "5",
                    OptionD = "6",
                    ReponseCorrecte = "B",
                    Explication = "arithmétique"
                }
            }
        };

        using var ms = new MemoryStream();
        var export = new ExportService();

        // WHEN
        export.ExporterQuizVersXlsx(quiz, ms);

        // THEN
        ms.Position = 0;
        using var read = SpreadsheetDocument.Open(ms, false);
        var sheet = read.WorkbookPart!.WorksheetParts.First().Worksheet;
        var texts = sheet.Descendants<CellValue>().Select(cv => cv.Text).ToList();
        Assert.True(texts.Any(t => t.Contains("Quiz export", StringComparison.Ordinal)));
        Assert.True(texts.Any(t => t.Contains("2+2", StringComparison.Ordinal)));
        Assert.True(texts.Any(t => t.Contains("arithmétique", StringComparison.Ordinal)));
    }
}
