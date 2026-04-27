namespace smartest_desktop.ViewModels
{
    /// <summary>
    /// Ligne affichée dans « Activités récentes » (quiz ou examen).
    /// </summary>
    public sealed class DashboardActiviteItem
    {
        public string TypeLabel { get; set; } = "";
        public string Titre { get; set; } = "";
        public string SousTitre { get; set; } = "";
        public string DateFormatee { get; set; } = "";
    }
}
