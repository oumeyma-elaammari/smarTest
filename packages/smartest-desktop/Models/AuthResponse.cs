namespace smartest_desktop.Models
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public string Role { get; set; }
        public string Nom { get; set; }
        public string Email { get; set; }
    }
}