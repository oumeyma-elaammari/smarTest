using System;
using System.Security.Cryptography;
using smartest_desktop.Services;
using Xunit;

namespace smartest_desktop.Tests;

public class CryptoServiceTests
{
    [Fact]
    public void Chiffrer_DeChiffrer_roundtrip_ok()
    {
        const string plain = "secret-token-123";
        var encrypted = CryptoService.Chiffrer(plain);
        var decrypted = CryptoService.Dechiffrer(encrypted);
        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public void Chiffrer_leve_si_null()
    {
        Assert.Throws<ArgumentNullException>(() => CryptoService.Chiffrer(null!));
    }

    [Fact]
    public void Dechiffrer_string_vide_leve()
    {
        Assert.Throws<CryptographicException>(() => CryptoService.Dechiffrer(""));
    }

    [Fact]
    public void Dechiffrer_base64_invalide_leve_FormatException()
    {
        Assert.Throws<FormatException>(() => CryptoService.Dechiffrer("pas-du-base64!!!"));
    }

    [Fact]
    public void Dechiffrer_base64_trop_court_leve()
    {
        var shortB64 = Convert.ToBase64String(new byte[] { 1, 2, 3 });
        Assert.Throws<CryptographicException>(() => CryptoService.Dechiffrer(shortB64));
    }
}
