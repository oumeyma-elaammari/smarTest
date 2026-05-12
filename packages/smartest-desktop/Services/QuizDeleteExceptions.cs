using System;

namespace smartest_desktop.Services
{
    /// <summary>
    /// Le prof est connecté mais aucun quiz serveur ne correspond ; l’utilisateur peut confirmer une suppression locale seule.
    /// </summary>
    public sealed class QuizDeleteLocalOnlyConfirmationRequiredException : Exception
    {
        public QuizDeleteLocalOnlyConfirmationRequiredException(string message)
            : base(message)
        {
        }
    }
}
