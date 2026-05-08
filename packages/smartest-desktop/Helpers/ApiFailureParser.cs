using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;

namespace smartest_desktop.Helpers
{
    /// <summary>Construit un message lisible à partir des corps d'erreur de l'API Spring (texte ou JSON de validation).</summary>
    public static class ApiFailureParser
    {
        /// <summary>Essaie de lire un dictionnaire champ → message (ex. MethodArgumentNotValidException).</summary>
        public static IReadOnlyDictionary<string, string>? TryParseValidationMap(string? body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;
            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(body);
                if (dict != null && dict.Count > 0)
                    return dict;
            }
            catch
            {
                // pas un objet champ → chaîne ; ignorer
            }

            return null;
        }

        /// <summary>Message utilisateur agrégé (une ligne ou plusieurs lignes).</summary>
        public static string BuildMessage(HttpStatusCode status, string? body)
        {
            var trimmed = body?.Trim() ?? "";

            var validation = TryParseValidationMap(trimmed);
            if (validation != null && validation.Count > 0)
                return string.Join(Environment.NewLine, validation.Values);

            if (!string.IsNullOrEmpty(trimmed))
            {
                if (trimmed.Length >= 2 && trimmed[0] == '"' && trimmed[^1] == '"')
                    try
                    {
                        var unquoted = JsonConvert.DeserializeObject<string>(trimmed);
                        if (!string.IsNullOrEmpty(unquoted))
                            return unquoted;
                    }
                    catch { /* garder trimmed */ }

                return UserErrorMessage.FromText(trimmed, FallbackForStatus(status));
            }

            return FallbackForStatus(status);
        }

        private static string FallbackForStatus(HttpStatusCode status)
        {
            switch (status)
            {
                case HttpStatusCode.Unauthorized:
                    return "Session expirée ou non autorisée. Reconnectez-vous.";
                case HttpStatusCode.Forbidden:
                    return "Accès refusé par le serveur.";
                case HttpStatusCode.NotFound:
                    return "Ressource introuvable sur le serveur.";
                case HttpStatusCode.BadRequest:
                    return "La requête a été refusée par le serveur.";
                default:
                {
                    int code = (int)status;
                    if (code >= 500)
                        return "Erreur serveur. Réessayez plus tard.";
                    return "Une erreur est survenue. Réessayez.";
                }
            }
        }
    }
}
