using System;

namespace smartest_desktop.Data.LocalEntities
{
    public class SessionLocale
    {
        public int Id { get; set; }
        public string TokenChiffre { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime DateConnexion { get; set; }
    }
}