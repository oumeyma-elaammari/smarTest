using smartest_desktop.Services;
using Xunit;

namespace smartest_desktop.Tests;

public class ImportServiceTests
{
    private static ImportService CreateService(out LocalCoursService cours)
    {
        var db = TestDbFactory.CreateInMemory();
        cours = new LocalCoursService(db);
        return new ImportService(cours);
    }

    [Fact]
    public void Import_txt_cree_cours()
    {
        var path = Path.Combine(Path.GetTempPath(), "imp_" + Guid.NewGuid() + ".txt");
        File.WriteAllText(path, "Contenu du cours pour import.");

        try
        {
            var svc = CreateService(out _);
            var c = svc.ImporterFichier(path);

            Assert.True(c.Id > 0);
            Assert.Contains("Contenu du cours", c.Contenu);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Import_pdf_valide_cree_cours()
    {
        var path = Path.Combine(Path.GetTempPath(), "imp_" + Guid.NewGuid() + ".pdf");
        File.WriteAllBytes(path, MinimalPdfFactory.CreateSinglePageWithHelveticaText(MinimalPdfFactory.EmbeddedPlainText));

        try
        {
            var svc = CreateService(out _);
            var c = svc.ImporterFichier(path);
            Assert.False(string.IsNullOrWhiteSpace(c.Contenu));
            Assert.Contains("Texte", c.Contenu, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Import_pdf_corrompu_leve_InvalidOperationException()
    {
        var path = Path.Combine(Path.GetTempPath(), "imp_" + Guid.NewGuid() + ".pdf");
        File.WriteAllBytes(path, new byte[] { 0x25, 0x50, 0x44, 0x46, 0xFF, 0xFF });

        try
        {
            var svc = CreateService(out _);
            Assert.Throws<InvalidOperationException>(() => svc.ImporterFichier(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Import_extension_non_supportee_leve()
    {
        var path = Path.Combine(Path.GetTempPath(), "imp_" + Guid.NewGuid() + ".xyz");
        File.WriteAllText(path, "x");

        try
        {
            var svc = CreateService(out _);
            Assert.Throws<NotSupportedException>(() => svc.ImporterFichier(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Import_txt_vide_leve_InvalidOperationException()
    {
        var path = Path.Combine(Path.GetTempPath(), "imp_" + Guid.NewGuid() + ".txt");
        File.WriteAllText(path, "   \n  ");

        try
        {
            var svc = CreateService(out _);
            Assert.Throws<InvalidOperationException>(() => svc.ImporterFichier(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
