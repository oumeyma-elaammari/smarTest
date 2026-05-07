using System.Globalization;
using System.Text;

namespace smartest_desktop.Tests;

/// <summary>Génère un PDF 1.1 minimal avec Helvetica (police standard PDF 14, aucun fichier .ttf).</summary>
internal static class MinimalPdfFactory
{
    public const string EmbeddedPlainText = "Texte PDF test";

    public static byte[] CreateSinglePageWithHelveticaText(string text)
    {
        var inner = $"BT /F1 24 Tf 100 700 Td ({EscapePdfLiteral(text)}) Tj ET\n";
        var len = Encoding.Latin1.GetByteCount(inner);

        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            $"<< /Length {len} >>\nstream\n{inner}endstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        var sb = new StringBuilder();
        sb.Append("%PDF-1.1\n");

        var xrefOffsets = new List<long>();
        for (var i = 0; i < objects.Length; i++)
        {
            xrefOffsets.Add(sb.Length);
            sb.Append(CultureInfo.InvariantCulture, $"{i + 1} 0 obj\n");
            sb.Append(objects[i]);
            sb.Append("\nendobj\n");
        }

        var xrefPos = sb.Length;
        sb.Append("xref\n");
        sb.Append(CultureInfo.InvariantCulture, $"0 {objects.Length + 1}\n");
        sb.Append("0000000000 65535 f \n");
        foreach (var off in xrefOffsets)
            sb.Append(CultureInfo.InvariantCulture, $"{off:D10} 00000 n \n");

        sb.Append("trailer\n");
        sb.Append(CultureInfo.InvariantCulture,
            $"<< /Size {objects.Length + 1} /Root 1 0 R >>\n");
        sb.Append("startxref\n");
        sb.Append(CultureInfo.InvariantCulture, $"{xrefPos}\n");
        sb.Append("%%EOF\n");

        return Encoding.Latin1.GetBytes(sb.ToString());
    }

    private static string EscapePdfLiteral(string s) =>
        s.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("(", "\\(", StringComparison.Ordinal)
            .Replace(")", "\\)", StringComparison.Ordinal);
}
