using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace smartest_desktop.Tests;

internal static class ExcelTestFile
{
    public static void CreateWithEmails(string path, params string[] emails)
    {
        using var spreadsheetDocument = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook);
        var workbookPart = spreadsheetDocument.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();

        var sheetData = new SheetData();
        sheetData.Append(new Row(new Cell(new CellValue("email"))));
        foreach (var e in emails)
            sheetData.Append(new Row(new Cell(new CellValue(e))));

        worksheetPart.Worksheet = new Worksheet(sheetData);

        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        sheets.Append(new Sheet
        {
            Id = spreadsheetDocument.WorkbookPart!.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name = "Feuil1"
        });

        workbookPart.Workbook.Save();
    }

    /// <summary>Feuille unique colonne « email » + N lignes valides.</summary>
    public static void CreateXlsxEmailColumn(string path, int dataRowCount)
    {
        using var spreadsheetDocument = SpreadsheetDocument.Create(path, SpreadsheetDocumentType.Workbook);
        var workbookPart = spreadsheetDocument.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();

        var sheetData = new SheetData();
        sheetData.Append(new Row(new Cell(new CellValue("email"))));
        for (int i = 0; i < dataRowCount; i++)
            sheetData.Append(new Row(new Cell(new CellValue($"perf{i:0000}@xlsx-load.test"))));

        worksheetPart.Worksheet = new Worksheet(sheetData);

        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        sheets.Append(new Sheet
        {
            Id = spreadsheetDocument.WorkbookPart!.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name = "Feuil1"
        });

        workbookPart.Workbook.Save();
    }
}
