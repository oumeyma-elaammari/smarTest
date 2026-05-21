using System.Diagnostics;
using System.Text;
using smartest_desktop.Services;
using smartest_desktop.Tests;
using Xunit;

namespace smartest_desktop.Tests.Performance;

/// <summary>Import massif : XLSX emails (<see cref="ImportEtudiantsService"/>) et TXT cours (<see cref="ImportService"/>).</summary>
public class ImportServicePerformanceTests
{
    [Fact]
    public void ImportEtudiants_Xlsx500Lignes_Under2Seconds()
    {
        var path = Path.Combine(Path.GetTempPath(), "perf_emails_" + Guid.NewGuid() + ".xlsx");
        try
        {
            ExcelTestFile.CreateXlsxEmailColumn(path, 500);

            var sw = Stopwatch.StartNew();
            var r = ImportEtudiantsService.ImporterDepuisFichierDetaille(path);
            sw.Stop();

            Assert.Equal(500, r.EmailsValides.Count);
            Assert.InRange(sw.Elapsed.TotalSeconds, 0, 2);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void ImportService_Txt200Lignes_Under1Second()
    {
        var path = Path.Combine(Path.GetTempPath(), "perf_txt_" + Guid.NewGuid() + ".txt");
        try
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 200; i++)
                sb.AppendLine($"Ligne cours {i} avec un peu de texte pour le volume.");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

            using var db = TestDbFactory.CreateInMemoryContext();
            var cours = new LocalCoursService(db);
            var svc = new ImportService(cours);

            var sw = Stopwatch.StartNew();
            var c = svc.ImporterFichier(path);
            sw.Stop();

            Assert.True(c.Id > 0);
            Assert.InRange(sw.Elapsed.TotalSeconds, 0, 1);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
