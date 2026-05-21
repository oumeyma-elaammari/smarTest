using System;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using smartest_desktop.Data.LocalEntities;

namespace smartest_desktop.Services
{
    /// <summary>Export des quiz locaux vers un classeur Excel (.xlsx).</summary>
    public sealed class ExportService
    {
        public void ExporterQuizVersXlsx(QuizLocal quiz, Stream destination)
        {
            ArgumentNullException.ThrowIfNull(quiz);
            ArgumentNullException.ThrowIfNull(destination);

            using var spreadsheet = SpreadsheetDocument.Create(destination, SpreadsheetDocumentType.Workbook);
            var wbPart = spreadsheet.AddWorkbookPart();
            wbPart.Workbook = new Workbook();
            var wsPart = wbPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();

            uint rowIndex = 1;
            void AppendRow(params string[] cells)
            {
                var row = new Row { RowIndex = rowIndex };
                foreach (var c in cells)
                {
                    row.Append(new Cell
                    {
                        DataType = CellValues.String,
                        CellValue = new CellValue(c ?? string.Empty)
                    });
                }

                sheetData.Append(row);
                rowIndex++;
            }

            AppendRow("Titre quiz", quiz.Titre ?? string.Empty);
            AppendRow("Difficulté", quiz.Difficulte ?? string.Empty);
            AppendRow("Statut", quiz.Statut ?? string.Empty);
            AppendRow("Nombre de questions", quiz.NombreQuestions.ToString(System.Globalization.CultureInfo.InvariantCulture));
            AppendRow(string.Empty);
            AppendRow("N°", "Type", "Énoncé", "A", "B", "C", "D", "Réponse", "Explication");

            var questions = (quiz.Questions ?? Enumerable.Empty<QuestionLocale>())
                .OrderBy(q => q.Numero)
                .ToList();

            foreach (var q in questions)
            {
                AppendRow(
                    q.Numero.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    q.Type ?? string.Empty,
                    q.Enonce ?? string.Empty,
                    q.OptionA ?? string.Empty,
                    q.OptionB ?? string.Empty,
                    q.OptionC ?? string.Empty,
                    q.OptionD ?? string.Empty,
                    q.ReponseCorrecte ?? string.Empty,
                    q.Explication ?? string.Empty);
            }

            wsPart.Worksheet = new Worksheet(sheetData);
            var sheets = wbPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id = spreadsheet.WorkbookPart!.GetIdOfPart(wsPart),
                SheetId = 1,
                Name = "Quiz"
            });
            wbPart.Workbook.Save();
        }
    }
}
