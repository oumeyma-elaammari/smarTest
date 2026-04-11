using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using smartest_desktop.Models;

namespace smartest_desktop.Services
{
    public class AuthService
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://localhost:8081";

        public AuthService()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl)
            };
            _httpClient.DefaultRequestHeaders
                .Add("Accept", "application/json");
        }

        private StringContent ToJson(object data) =>
            new StringContent(
                JsonConvert.SerializeObject(data),
                Encoding.UTF8,
                "application/json"
            );

        // ══════════════════════════════════════════════
        //  REGISTER PROFESSEUR
        // ══════════════════════════════════════════════
        public async Task<string> RegisterAsync(
            string nom,
            string email,
            string password,
            string confirmPassword)
        {
            try
            {
                var body = new { nom, email, password, confirmPassword };
                var response = await _httpClient
                    .PostAsync("/auth/register", ToJson(body));

                if (response.IsSuccessStatusCode)
                    return null; // ✅ succès

                return (int)response.StatusCode switch
                {
                    409 => "Cet email est déjà utilisé",
                    400 => "Données invalides",
                    _ => "Erreur serveur. Réessayez"
                };
            }
            catch
            {
                return "Impossible de contacter le serveur";
            }
        }

        // ══════════════════════════════════════════════
        //  LOGIN
        // ══════════════════════════════════════════════
        public async Task<(AuthResponse auth, string error)> LoginAsync(
            string email,
            string password)
        {
            try
            {
                var body = new { email, password };
                var response = await _httpClient
                    .PostAsync("/auth/login", ToJson(body));
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var auth = JsonConvert
                        .DeserializeObject<AuthResponse>(content);

                    if (auth.Role != "PROFESSEUR")
                        return (null,
                            "Ce compte n'est pas un compte professeur");

                    return (auth, null);
                }

                string error = (int)response.StatusCode switch
                {
                    401 => "Email ou mot de passe incorrect",
                    403 => "Veuillez confirmer votre email",
                    404 => "Compte introuvable",
                    _ => "Erreur serveur. Réessayez"
                };

                return (null, error);
            }
            catch
            {
                return (null, "Impossible de contacter le serveur");
            }
        }

        // ══════════════════════════════════════════════
        //  FORGOT PASSWORD PROFESSEUR
        // ══════════════════════════════════════════════
        public async Task<string> ForgotPasswordAsync(string email)
        {
            try
            {
                var body = new { email };
                var response = await _httpClient.PostAsync(
                    "/auth/forgot-password/professeur", ToJson(body));

                return response.IsSuccessStatusCode
                    ? null
                    : "Erreur serveur. Réessayez";
            }
            catch
            {
                return "Impossible de contacter le serveur";
            }
        }

        // ══════════════════════════════════════════════
        //  RESET PASSWORD PROFESSEUR
        // ══════════════════════════════════════════════
        public async Task<string> ResetPasswordAsync(
            string token,
            string newPassword,
            string confirmPassword)
        {
            try
            {
                var body = new { token, newPassword, confirmPassword };
                var response = await _httpClient.PostAsync(
                    "/auth/reset-password/professeur", ToJson(body));

                if (response.IsSuccessStatusCode)
                    return null;

                return (int)response.StatusCode switch
                {
                    400 => "Lien invalide ou expiré",
                    _ => "Erreur serveur. Réessayez"
                };
            }
            catch
            {
                return "Impossible de contacter le serveur";
            }
        }
    }
}