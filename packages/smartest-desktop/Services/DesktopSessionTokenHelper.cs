using smartest_desktop.Data;
using smartest_desktop.Models;
using System;
using System.Threading.Tasks;
using WpfApp = System.Windows.Application;

namespace smartest_desktop.Services
{
    /// <summary>Jeton JWT en mémoire (Properties) et session SQLite locale.</summary>
    public static class DesktopSessionTokenHelper
    {
        public static string Normaliser(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return string.Empty;

            token = token.Trim();
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                token = token.Substring("Bearer ".Length).Trim();
            return token;
        }

        public static bool TryObtenir(out string token)
        {
            token = Normaliser(WpfApp.Current.Properties["Token"]?.ToString());
            if (!string.IsNullOrEmpty(token))
                return true;

            try
            {
                var session = new SessionService(App.LocalDb).ChargerSession();
                if (session == null || string.IsNullOrWhiteSpace(session.TokenChiffre))
                    return false;

                Appliquer(session.TokenChiffre, session.Nom, session.Email);
                token = Normaliser(session.TokenChiffre);
                return !string.IsNullOrEmpty(token);
            }
            catch
            {
                return false;
            }
        }

        public static void Appliquer(AuthResponse auth)
        {
            ArgumentNullException.ThrowIfNull(auth);
            Appliquer(auth.Token, auth.Nom, auth.Email);
            try
            {
                new SessionService(App.LocalDb).SauvegarderSession(auth);
            }
            catch
            {
                /* session locale optionnelle */
            }
        }

        private static void Appliquer(string token, string nom, string email)
        {
            WpfApp.Current.Properties["Token"] = Normaliser(token);
            WpfApp.Current.Properties["Nom"] = nom;
            WpfApp.Current.Properties["Email"] = email;
        }

        public static async Task<bool> TryRafraichirDepuisApiAsync(AuthService auth)
        {
            if (!TryObtenir(out string? token))
                return false;

            var (nouveau, _) = await auth.RefreshAccessTokenAsync(token);
            if (nouveau == null || string.IsNullOrWhiteSpace(nouveau.Token))
                return false;
            if (!string.Equals(nouveau.Role, "PROFESSEUR", StringComparison.OrdinalIgnoreCase))
                return false;

            Appliquer(nouveau);
            return true;
        }
    }
}
