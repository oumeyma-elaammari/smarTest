using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace smartest_desktop.Services;

/// <summary>Complète <see cref="IGroqGenerationClient"/> pour les flux examen (requête unique avec durée).</summary>
public static class GroqGenerationClientExtensions
{
    public static async Task<(string Texte, TimeSpan Duree)> GenererAsync(
        this IGroqGenerationClient client,
        string prompt,
        CancellationToken ct,
        double temperature)
    {
        ArgumentNullException.ThrowIfNull(client);
        var sw = Stopwatch.StartNew();
        var texte = await client.GenererAvecRetryAsync(prompt, ct, temperature).ConfigureAwait(false);
        sw.Stop();
        return (texte, sw.Elapsed);
    }
}
