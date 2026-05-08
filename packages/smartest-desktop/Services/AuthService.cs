using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using smartest_desktop.Exceptions;
using smartest_desktop.Helpers;
using smartest_desktop.Models;

namespace smartest_desktop.Services
{
    public class AuthService
    {
        public const string EmailNotVerifiedCode = "__EMAIL_NOT_VERIFIED__";

        private readonly HttpClient _httpClient;
        private const string BaseUrl = "http://localhost:8081";

        /// <remarks>Injection optionnelle du <see cref="HttpClient"/> pour les tests.</remarks>
        public AuthService(HttpClient? httpClientOverride = null)
        {
            if (httpClientOverride != null)
            {
                _httpClient = httpClientOverride;
            }
            else
            {
                _httpClient = new HttpClient { BaseAddress = new Uri(BaseUrl) };
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            }
        }

        private StringContent ToJson(object data) =>
            new StringContent(
                JsonConvert.SerializeObject(data),
                Encoding.UTF8,
                "application/json");

        // ══════════════════════════════════════════════
        //  REGISTER
        // ══════════════════════════════════════════════
        public async Task<string> RegisterAsync(
            string nom, string email,
            string password, string confirmPassword,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { nom, email, password, confirmPassword };
                var response = await _httpClient.PostAsync("/auth/register", ToJson(body), cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode) return null!;

                return (int)response.StatusCode switch
                {
                    403 => EmailNotVerifiedCode,
                    409 => "Cet email est déjà utilisé par un autre compte",
                    400 => ParseValidationError(content),
                    500 => "Erreur serveur. Réessayez plus tard",
                    _ => $"Erreur inattendue ({(int)response.StatusCode})"
                };
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                return SmartestNetworkException.ServerUnreachable(ex).Message;
            }
            catch (HttpRequestException ex)
            {
                return SmartestNetworkException.ServerUnreachable(ex).Message;
            }
            catch (Exception ex) { return UserErrorMessage.FromException(ex, "Une erreur est survenue lors de l'inscription."); }
        }

        // ══════════════════════════════════════════════
        //  VERIFY EMAIL PAR CODE 6 CHIFFRES
        // ══════════════════════════════════════════════
        public async Task<string> VerifyEmailByCodeAsync(string email, string code, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/auth/verify-email/code?email={Uri.EscapeDataString(email)}&code={code}";
                var response = await _httpClient.PostAsync(url, null, cancellationToken);

                if (response.IsSuccessStatusCode) return null!;

                return (int)response.StatusCode switch
                {
                    400 => "Code invalide ou expiré. Vérifiez le code reçu par email.",
                    _ => "Erreur lors de la vérification. Réessayez."
                };
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                return SmartestNetworkException.ServerUnreachable(ex).Message;
            }
            catch (HttpRequestException ex)
            {
                return SmartestNetworkException.ServerUnreachable(ex).Message;
            }
            catch (Exception ex) { return UserErrorMessage.FromException(ex, "Une erreur est survenue lors de la verification."); }
        }

        // ══════════════════════════════════════════════
        //  RENVOYER LE CODE
        // ══════════════════════════════════════════════
        public async Task<string> ResendVerificationCodeAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                var url = $"/auth/verify-email/resend?email={Uri.EscapeDataString(email)}";
                var response = await _httpClient.PostAsync(url, null, cancellationToken);

                if (response.IsSuccessStatusCode) return null!;

                return "Impossible de renvoyer le code.";
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                return SmartestNetworkException.ServerUnreachable(ex).Message;
            }
            catch (HttpRequestException ex)
            {
                return SmartestNetworkException.ServerUnreachable(ex).Message;
            }
            catch (Exception ex) { return UserErrorMessage.FromException(ex, "Impossible de renvoyer le code pour le moment."); }
        }

        // ══════════════════════════════════════════════
        //  LOGIN - CORRIGÉ
        // ══════════════════════════════════════════════
        public async Task<(AuthResponse? auth, string error)> LoginAsync(
            string email, string password, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { email, password };
                var response = await _httpClient.PostAsync("/auth/login", ToJson(body), cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    AuthResponse auth;
                    try
                    {
                        auth = JsonConvert.DeserializeObject<AuthResponse>(content);
                    }
                    catch (JsonException)
                    {
                        return (null, "Réponse du serveur illisible après connexion.");
                    }

                    if (auth?.Role != "PROFESSEUR")
                        return (null, "Ce compte n'est pas un compte professeur.");

                    return (auth, null!);
                }

                int status = (int)response.StatusCode;
                string serverMsg = content?.Trim('"').ToLower() ?? "";

                string error;
                if (status == 401)
                {
                    if (serverMsg.Contains("introuvable") || serverMsg.Contains("not found"))
                        error = "Aucun compte trouvé avec cet email";
                    else if (serverMsg.Contains("password") || serverMsg.Contains("mot de passe"))
                        error = "Mot de passe incorrect";
                    else
                        error = "Email ou mot de passe incorrect";
                }
                else if (status == 403)
                    error = EmailNotVerifiedCode;
                else if (status == 400)
                    error = ParseValidationError(content);
                else if (status >= 500)
                    error = "Erreur serveur. Réessayez plus tard";
                else
                    error = $"Erreur inattendue ({status})";

                return (null, error);
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                return (null, SmartestNetworkException.ServerUnreachable(ex).Message);
            }
            catch (HttpRequestException ex)
            {
                return (null, SmartestNetworkException.ServerUnreachable(ex).Message);
            }
            catch (Exception ex) { return (null, UserErrorMessage.FromException(ex, "Une erreur est survenue lors de la connexion.")); }
        }

        // ══════════════════════════════════════════════
        //  FORGOT PASSWORD
        // ══════════════════════════════════════════════
        public async Task<string> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { email };
                var response = await _httpClient.PostAsync(
                    "/auth/forgot-password/professeur", ToJson(body), cancellationToken);

                if (response.IsSuccessStatusCode) return null!;

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                return (int)response.StatusCode switch
                {
                    400 => "Format d'email invalide",
                    500 => "Erreur serveur lors de l'envoi de l'email. Réessayez",
                    _ => "La demande n'a pas pu aboutir. Réessayez."
                };
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                return SmartestNetworkException.ServerUnreachable(ex).Message;
            }
            catch (HttpRequestException ex)
            {
                return SmartestNetworkException.ServerUnreachable(ex).Message;
            }
            catch (Exception ex) { return UserErrorMessage.FromException(ex, "Une erreur est survenue lors de la demande de reinitialisation."); }
        }

        // ══════════════════════════════════════════════
        //  RESET PASSWORD
        // ══════════════════════════════════════════════
        public async Task<string> ResetPasswordAsync(
            string token, string newPassword, string confirmPassword, CancellationToken cancellationToken = default)
        {
            try
            {
                var body = new { token, newPassword, confirmPassword };
                var response = await _httpClient.PostAsync(
                    "/auth/reset-password/professeur", ToJson(body), cancellationToken);

                if (response.IsSuccessStatusCode) return null!;

                var content = await response.Content.ReadAsStringAsync(cancellationToken);
                string serverMsg = content?.Trim('"').ToLower() ?? "";

                return (int)response.StatusCode switch
                {
                    400 when serverMsg.Contains("expir") =>
                        "Ce code a expiré. Faites une nouvelle demande",
                    400 when serverMsg.Contains("correspond") =>
                        "Les mots de passe ne correspondent pas",
                    400 => "Code invalide ou expiré.",
                    500 => "Erreur serveur. Réessayez plus tard",
                    _ => "La demande n'a pas pu aboutir. Réessayez."
                };
            }
            catch (OperationCanceledException ex)
            {
                if (cancellationToken.IsCancellationRequested)
                    throw;
                return SmartestNetworkException.ServerUnreachable(ex).Message;
            }
            catch (HttpRequestException ex)
            {
                return SmartestNetworkException.ServerUnreachable(ex).Message;
            }
            catch (Exception ex) { return UserErrorMessage.FromException(ex, "Une erreur est survenue lors de la reinitialisation du mot de passe."); }
        }

        // ══════════════════════════════════════════════
        //  HELPER
        // ══════════════════════════════════════════════
        private string ParseValidationError(string content)
        {
            if (string.IsNullOrEmpty(content)) return "Données invalides";
            try
            {
                var errors = JsonConvert.DeserializeObject<
                    System.Collections.Generic.Dictionary<string, string>>(content);
                if (errors != null && errors.Count > 0)
                    return string.Join("\n", errors.Values);
            }
            catch { }
            return content.Trim('"');
        }
    }
}
