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
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
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
            string nom, string email,
            string password, string confirmPassword)
        {
            try
            {
                var body = new { nom, email, password, confirmPassword };
                var response = await _httpClient
                    .PostAsync("/auth/register", ToJson(body));
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return null;

                return (int)response.StatusCode switch
                {
                    409 => "Cet email est déjà utilisé par un autre compte",
                    400 => ParseValidationError(content),
                    500 => "Erreur serveur. Réessayez plus tard",
                    _ => $"Erreur inattendue ({(int)response.StatusCode})"
                };
            }
            catch (HttpRequestException)
            {
                return "Impossible de contacter le serveur. Vérifiez que le backend est lancé";
            }
            catch (Exception ex)
            {
                return $"Erreur : {ex.Message}";
            }
        }

        // ══════════════════════════════════════════════
        //  LOGIN
        // ══════════════════════════════════════════════
        public async Task<(AuthResponse auth, string error)> LoginAsync(
            string email, string password)
        {
            try
            {
                var body = new { email, password };
                var response = await _httpClient
                    .PostAsync("/auth/login", ToJson(body));
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var auth = JsonConvert.DeserializeObject<AuthResponse>(content);

                    if (auth?.Role != "PROFESSEUR")
                        return (null, "Ce compte n'est pas un compte professeur. Utilisez l'application web pour les étudiants");

                    return (auth, null);
                }

                // ✅ Lire le message du backend pour distinguer les erreurs
                int status = (int)response.StatusCode;
                string serverMsg = content?.Trim('"').ToLower() ?? "";

                string error;
                if (status == 401)
                {
                    // Distinguer "mot de passe incorrect" vs "compte introuvable"
                    if (serverMsg.Contains("introuvable") || serverMsg.Contains("not found"))
                        error = "Aucun compte trouvé avec cet email";
                    else if (serverMsg.Contains("password") || serverMsg.Contains("mot de passe"))
                        error = "Mot de passe incorrect";
                    else
                        error = "Email ou mot de passe incorrect";
                }
                else if (status == 403)
                {
                    error = "Email non confirmé. Vérifiez votre boîte mail et cliquez sur le lien de confirmation";
                }
                else if (status == 400)
                {
                    error = ParseValidationError(content);
                }
                else if (status >= 500)
                {
                    error = "Erreur serveur. Réessayez plus tard";
                }
                else
                {
                    error = $"Erreur inattendue ({status})";
                }

                return (null, error);
            }
            catch (HttpRequestException)
            {
                return (null, "Impossible de contacter le serveur.");
            }
            catch (Exception ex)
            {
                return (null, $"Erreur : {ex.Message}");
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
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return null;

                return (int)response.StatusCode switch
                {
                    400 => "Format d'email invalide",
                    500 => "Erreur serveur lors de l'envoi de l'email. Réessayez",
                    _ => $"Erreur ({(int)response.StatusCode}). Réessayez"
                };
            }
            catch (HttpRequestException)
            {
                return "Impossible de contacter le serveur. Vérifiez que le backend est lancé";
            }
            catch (Exception ex)
            {
                return $"Erreur : {ex.Message}";
            }
        }

        // ══════════════════════════════════════════════
        //  RESET PASSWORD PROFESSEUR
        // ══════════════════════════════════════════════
        public async Task<string> ResetPasswordAsync(
            string token, string newPassword, string confirmPassword)
        {
            try
            {
                var body = new { token, newPassword, confirmPassword };
                var response = await _httpClient.PostAsync(
                    "/auth/reset-password/professeur", ToJson(body));
                var content = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                    return null;

                string serverMsg = content?.Trim('"').ToLower() ?? "";

                return (int)response.StatusCode switch
                {
                    400 when serverMsg.Contains("expir") =>
                        "Ce code a expiré. Faites une nouvelle demande de réinitialisation",
                    400 when serverMsg.Contains("correspond") =>
                        "Les mots de passe ne correspondent pas",
                    400 =>
                        "Code invalide ou expiré. Vérifiez le code reçu par email",
                    500 =>
                        "Erreur serveur. Réessayez plus tard",
                    _ =>
                        $"Erreur ({(int)response.StatusCode}). Réessayez"
                };
            }
            catch (HttpRequestException)
            {
                return "Impossible de contacter le serveur. Vérifiez que le backend est lancé";
            }
            catch (Exception ex)
            {
                return $"Erreur : {ex.Message}";
            }
        }

        // ══════════════════════════════════════════════
        //  HELPER — Parser les erreurs de validation
        // ══════════════════════════════════════════════
        private string ParseValidationError(string content)
        {
            if (string.IsNullOrEmpty(content)) return "Données invalides";

            try
            {
                // Le backend retourne un Map<String, String> pour les erreurs de validation
                var errors = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, string>>(content);

                if (errors != null && errors.Count > 0)
                    return string.Join("\n", errors.Values);
            }
            catch { }

            // Si c'est une simple string
            return content.Trim('"');
        }
    }
}