using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace smartest_desktop.Helpers;

/// <summary>
/// Extraction PDF alignée sur les recommandations PdfPig : éviter <see cref="Page.Text"/> seul,
/// qui concatène souvent les glyphes sans espaces. Utilise les mots (voisins / ordre de contenu)
/// puis choisit le candidat le plus « aéré » (plus d’espaces = mieux séparé).
/// </summary>
internal static class PdfTextImport
{
    public static string ExtraireTexteBrut(string chemin)
    {
        using var doc = PdfDocument.Open(chemin);
        var sb = new StringBuilder();
        foreach (var page in doc.GetPages())
        {
            var t = ExtraireTextePage(page);
            if (!string.IsNullOrWhiteSpace(t))
                sb.AppendLine(t);
        }

        return sb.ToString();
    }

    private static string ExtraireTextePage(Page page)
    {
        var candidats = new List<string>();

        void AddWords(IEnumerable<Word> words)
        {
            var s = string.Join(" ", words.Select(w => w.Text).Where(x => !string.IsNullOrWhiteSpace(x)));
            if (!string.IsNullOrWhiteSpace(s))
                candidats.Add(s);
        }

        AddWords(page.GetWords(NearestNeighbourWordExtractor.Instance));

        var contentOrder = ContentOrderTextExtractor.GetText(page);
        if (!string.IsNullOrWhiteSpace(contentOrder))
            candidats.Add(contentOrder.Trim());

        AddWords(page.GetWords());

        var fallback = (page.Text ?? "").Trim();
        if (string.IsNullOrEmpty(fallback))
            fallback = string.Concat(page.Letters.Select(l => l.Value));
        if (!string.IsNullOrWhiteSpace(fallback))
            candidats.Add(fallback);

        if (candidats.Count == 0)
            return string.Empty;

        var meilleur = candidats
            .OrderByDescending(c => c.Count(ch => ch == ' '))
            .ThenByDescending(c => c.Length)
            .First();

        return ReparerMotsCollesPdf(meilleur);
    }

    /// <summary>Espace entre une lettre minuscule et une majuscule (ex. « SwitchCisco »).</summary>
    private static string ReparerMotsCollesPdf(string texte)
    {
        if (string.IsNullOrEmpty(texte))
            return texte;
        return Regex.Replace(texte, @"(\p{Ll})(\p{Lu})", "$1 $2");
    }
}
