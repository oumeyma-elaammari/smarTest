using System.Collections.Generic;

namespace smartest_desktop.Models
{
    public class Question
    {
        public string Texte { get; set; }
        public List<string> Choix { get; set; }
        public string Reponse { get; set; }
    }
}