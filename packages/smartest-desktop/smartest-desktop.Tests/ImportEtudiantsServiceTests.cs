using smartest_desktop.Services;
using Xunit;

namespace smartest_desktop.Tests;

public class ImportEtudiantsServiceTests
{
    [Fact]
    public void Csv_valide_extrait_emails()
    {
        var path = Path.Combine(Path.GetTempPath(), "stu_" + Guid.NewGuid() + ".csv");
        File.WriteAllText(path, "email\na@test.fr\nb@test.fr");

        try
        {
            var r = ImportEtudiantsService.ImporterDepuisFichierDetaille(path);
            Assert.Equal(2, r.EmailsValides.Count);
            Assert.Contains("a@test.fr", r.EmailsValides);
            Assert.Contains("b@test.fr", r.EmailsValides);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Csv_doublons_dedoublonne()
    {
        var path = Path.Combine(Path.GetTempPath(), "stu_" + Guid.NewGuid() + ".csv");
        File.WriteAllText(path, "dup@test.fr\ndup@test.fr");

        try
        {
            var list = ImportEtudiantsService.ImporterDepuisFichier(path);
            Assert.Single(list);
            Assert.Equal("dup@test.fr", list[0]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Csv_vide_retour_liste_vide()
    {
        var path = Path.Combine(Path.GetTempPath(), "stu_" + Guid.NewGuid() + ".csv");
        File.WriteAllText(path, "");

        try
        {
            var r = ImportEtudiantsService.ImporterDepuisFichierDetaille(path);
            Assert.Empty(r.EmailsValides);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Csv_sans_email_valide_retour_vide()
    {
        var path = Path.Combine(Path.GetTempPath(), "stu_" + Guid.NewGuid() + ".csv");
        File.WriteAllText(path, "foo\nbar\nnot-an-email");

        try
        {
            var r = ImportEtudiantsService.ImporterDepuisFichierDetaille(path);
            Assert.Empty(r.EmailsValides);
            Assert.True(r.LignesAvecDuContenu >= 1);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Format_non_supporte_leve_NotSupportedException()
    {
        var path = Path.Combine(Path.GetTempPath(), "stu_" + Guid.NewGuid() + ".bin");
        File.WriteAllText(path, "x");

        try
        {
            Assert.Throws<NotSupportedException>(() => ImportEtudiantsService.ImporterDepuisFichier(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Xlsx_lit_colonne_email()
    {
        var path = Path.Combine(Path.GetTempPath(), "stu_" + Guid.NewGuid() + ".xlsx");
        ExcelTestFile.CreateWithEmails(path, "xlsx1@test.fr", "xlsx2@test.fr");

        try
        {
            var r = ImportEtudiantsService.ImporterDepuisFichierDetaille(path);
            Assert.Equal(2, r.EmailsValides.Count);
            Assert.Contains("xlsx1@test.fr", r.EmailsValides);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
