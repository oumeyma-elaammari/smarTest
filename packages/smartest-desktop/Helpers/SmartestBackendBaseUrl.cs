using System;

namespace smartest_desktop.Helpers
{
    /// <summary>
    /// URL du backend Spring (JWT, suppression quiz, publication, etc.).
    /// Doit être la même base que pour l’application web configurée contre ce backend.
    /// </summary>
    public static class SmartestBackendBaseUrl
    {
        private const string Fallback = "http://localhost:8081";

        /// <summary>
        /// Retourne une base sans slash final (<c>https://api.exemple.fr</c>).
        /// Priorité : <c>SMARTEST_BACKEND_URL</c>, puis <c>SMARTEST_API_URL</c>, puis localhost développement.
        /// </summary>
        public static string Resolve()
        {
            foreach (var key in new[] { "SMARTEST_BACKEND_URL", "SMARTEST_API_URL" })
            {
                var v = Environment.GetEnvironmentVariable(key);
                if (!string.IsNullOrWhiteSpace(v))
                    return v.Trim().TrimEnd('/');
            }

            return Fallback;
        }
    }
}
