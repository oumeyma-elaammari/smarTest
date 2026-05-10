using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace smartest_desktop.Helpers
{
    /// <summary>URL de l’application web (passage quiz-live / QR) pour ouvrir le navigateur système.</summary>
    public static class FrontendPublicUrl
    {
        private const string Fallback = "http://localhost:5173";

        /// <summary>
        /// Priorité : <c>SMARTEST_WEB_URL</c>, puis <c>appsettings.json</c> → <c>FrontendUrl</c>, puis défaut local.
        /// </summary>
        public static string Resolve()
        {
            var env = Environment.GetEnvironmentVariable("SMARTEST_WEB_URL");
            if (!string.IsNullOrWhiteSpace(env))
                return env.Trim().TrimEnd('/');

            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (File.Exists(path))
                {
                    var jo = JObject.Parse(File.ReadAllText(path));
                    var u = jo["FrontendUrl"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(u))
                        return u.Trim().TrimEnd('/');
                }
            }
            catch
            {
                /* ignore */
            }

            return Fallback;
        }
    }
}
