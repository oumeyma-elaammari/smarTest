using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Models;
using smartest_desktop.Services;
using Xunit;

namespace smartest_desktop.Tests;

public class SessionServiceTests
{
    [Fact]
    public void SauvegarderSession_puis_ChargerSession_retourne_token_en_clair()
    {
        using var db = TestDbFactory.CreateInMemoryContext();
        var svc = new SessionService(db);
        var auth = new AuthResponse
        {
            Token = "jwt-token",
            Nom = "Dupont",
            Email = "p@example.com",
            Role = "PROFESSEUR"
        };

        svc.SauvegarderSession(auth);

        var loaded = svc.ChargerSession();
        Assert.NotNull(loaded);
        Assert.Equal("jwt-token", loaded!.TokenChiffre);
        Assert.Equal("Dupont", loaded.Nom);
        Assert.Equal("p@example.com", loaded.Email);
    }

    [Fact]
    public void ChargerSession_retour_null_si_aucune_session()
    {
        using var db = TestDbFactory.CreateInMemoryContext();
        var svc = new SessionService(db);

        Assert.Null(svc.ChargerSession());
    }

    [Fact]
    public void ChargerSession_session_corrompue_supprime_et_retour_null()
    {
        using var db = TestDbFactory.CreateInMemoryContext();
        db.SessionsLocales.Add(new SessionLocale
        {
            TokenChiffre = "pas-un-chiffrement-valide",
            Nom = "X",
            Email = "x@y.z",
            Role = "PROFESSEUR"
        });
        db.SaveChanges();

        var svc = new SessionService(db);
        var loaded = svc.ChargerSession();

        Assert.Null(loaded);
        Assert.Empty(db.SessionsLocales);
    }
}
