using System.Security.Cryptography;
using smartest_desktop.Services;
using Xunit;

namespace smartest_desktop.Tests.Services;

public class CryptoServiceTests
{
    [Fact]
    public void Chiffrer_DeChiffrer_RoundTrip_Ok()
    {
        // GIVEN / WHEN / THEN
        const string plain = "secret-token-123";
        var encrypted = CryptoService.Chiffrer(plain);
        var decrypted = CryptoService.Dechiffrer(encrypted);
        Assert.Equal(plain, decrypted);
    }

    [Fact]
    public void Chiffrer_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => CryptoService.Chiffrer(null!));
    }

    [Fact]
    public void Dechiffrer_StringVide_Throws()
    {
        Assert.Throws<CryptographicException>(() => CryptoService.Dechiffrer(""));
    }

    [Fact]
    public void Dechiffrer_Base64Invalide_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => CryptoService.Dechiffrer("pas-du-base64!!!"));
    }

    [Fact]
    public void Dechiffrer_Base64TropCourt_Throws()
    {
        var shortB64 = Convert.ToBase64String(new byte[] { 1, 2, 3 });
        Assert.Throws<CryptographicException>(() => CryptoService.Dechiffrer(shortB64));
    }

    [Fact]
    public void Chiffrer_ChaineVide_RoundTrip_Vide()
    {
        // GIVEN : données « vides » mais autorisées (pas null)
        var enc = CryptoService.Chiffrer(string.Empty);

        // WHEN
        var dec = CryptoService.Dechiffrer(enc);

        // THEN
        Assert.Equal(string.Empty, dec);
    }

    [Fact]
    public void Dechiffrer_DonneesAlterees_CleIncorrecte_ThrowsCryptographicException()
    {
        var enc = CryptoService.Chiffrer("secret");
        var bytes = Convert.FromBase64String(enc);
        for (int i = bytes.Length - 1; i >= Math.Max(0, bytes.Length - 8); i--)
            bytes[i] ^= 0xFF;
        var altered = Convert.ToBase64String(bytes);

        var ex = Assert.Throws<CryptographicException>(() => CryptoService.Dechiffrer(altered));
        Assert.Contains("Impossible", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
