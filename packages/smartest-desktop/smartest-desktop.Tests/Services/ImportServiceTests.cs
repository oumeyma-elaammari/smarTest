using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using smartest_desktop.Services;
using Xunit;

namespace smartest_desktop.Tests.Services;

public class ImportServiceTests
{
    [Fact]
    public void Import_txt_cree_cours()
    {
        var path = Path.Combine(Path.GetTempPath(), "imp_" + Guid.NewGuid() + ".txt");
        File.WriteAllText(path, "Contenu du cours pour import.");

        try
        {
            using var db = TestDbFactory.CreateInMemoryContext();
            var cours = new LocalCoursService(db);
            var svc = new ImportService(cours);
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
            using var db = TestDbFactory.CreateInMemoryContext();
            var cours = new LocalCoursService(db);
            var svc = new ImportService(cours);
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
            using var db = TestDbFactory.CreateInMemoryContext();
            var cours = new LocalCoursService(db);
            var svc = new ImportService(cours);
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
            using var db = TestDbFactory.CreateInMemoryContext();
            var cours = new LocalCoursService(db);
            var svc = new ImportService(cours);
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
            using var db = TestDbFactory.CreateInMemoryContext();
            var cours = new LocalCoursService(db);
            var svc = new ImportService(cours);
            Assert.Throws<InvalidOperationException>(() => svc.ImporterFichier(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Import_docx_sans_paragraphe_texte_leve_InvalidOperationException()
    {
        // GIVEN : document Word sans texte exploitable (équivalent « colonnes / contenu manquant »)
        var path = Path.Combine(Path.GetTempPath(), "imp_" + Guid.NewGuid() + ".docx");
        using (var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
        {
            var mp = doc.AddMainDocumentPart();
            mp.Document = new Document(new Body());
            mp.Document.Save();
        }

        try
        {
            using var db = TestDbFactory.CreateInMemoryContext();
            var cours = new LocalCoursService(db);
            var svc = new ImportService(cours);
            // WHEN / THEN
            Assert.Throws<InvalidOperationException>(() => svc.ImporterFichier(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Import_pdf_mockParser_ContenuInjecte()
    {
        // GIVEN : PDF dont l’extraction est court-circuitée (test sans PdfPig réel)
        var path = Path.Combine(Path.GetTempPath(), "imp_" + Guid.NewGuid() + ".pdf");
        File.WriteAllBytes(path, new byte[] { 0x25, 0x50, 0x44, 0x46 });

        try
        {
            using var db = TestDbFactory.CreateInMemoryContext();
            var cours = new LocalCoursService(db);
            var svc = new ImportService(cours, _ => "Contenu PDF simulé pour le test unitaire.");

            var c = svc.ImporterFichier(path);

            Assert.Contains("simulé", c.Contenu, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
