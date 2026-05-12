using System;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace smartest_desktop.Helpers
{
    /// <summary>Nettoie les messages techniques avant affichage UI.</summary>
    public static class UserErrorMessage
    {
        public static string FromException(Exception? exception, string fallback)
        {
            if (exception == null)
                return fallback;

            if (exception is TimeoutException || exception is TaskCanceledException)
                return "Le service met trop de temps a repondre. Reessayez dans quelques instants.";

            if (exception is HttpRequestException)
                return "Impossible de contacter le serveur. Verifiez votre connexion puis reessayez.";

            return FromText(exception.Message, fallback);
        }

        public static string FromText(string? raw, string fallback)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return fallback;

            var text = raw.Trim();

            // Supprime traces et signatures techniques
            text = Regex.Replace(text, @"\bat\s+[^\r\n]+", string.Empty, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"[A-Za-z0-9_.]+Exception:?", string.Empty, RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"\s+", " ").Trim();

            var lower = text.ToLowerInvariant();
            if (lower.Contains("401") || lower.Contains("unauthorized"))
                return "Session expirée ou non autorisee. Reconnectez-vous.";
            if (lower.Contains("403") || lower.Contains("forbidden"))
                return "Acces refuse.";
            if (lower.Contains("404") || lower.Contains("not found") || lower.Contains("introuvable"))
                return "Ressource introuvable.";
            if (lower.Contains("500") || lower.Contains("internal server"))
                return "Erreur serveur. Reessayez plus tard.";
            if (lower.Contains("timeout"))
                return "Le service met trop de temps a repondre. Reessayez.";

            if (string.IsNullOrWhiteSpace(text) || text.Length > 220)
                return fallback;

            return text;
        }
    }
}
