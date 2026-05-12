namespace smartest_desktop.Constants
{
    /// <summary>
    /// Règles produit : publication web du quiz avec liste d'étudiants autorisés (emails).
    /// La liste est persistée côté serveur au moment de « Publier sur le web » (hors périmètre module).
    /// </summary>
    public static class QuizPublicationLimits
    {
        public const int MaxAuthorizedStudentEmails = 2000;
    }
}
