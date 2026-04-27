using smartest_desktop.Data.LocalEntities;

namespace smartest_desktop.ViewModels
{
    /// <summary>
    /// Wrapper UI autour de CoursLocal, avec état de sélection observable.
    /// </summary>
    public class CoursItem : BaseViewModel
    {
        private bool _estSelectionne;

        public int    Id      { get; }
        public string Titre   { get; }
        public string Contenu { get; }

        public bool EstSelectionne
        {
            get => _estSelectionne;
            set => SetProperty(ref _estSelectionne, value);
        }

        public CoursItem(CoursLocal cours)
        {
            Id      = cours.Id;
            Titre   = cours.Titre;
            Contenu = cours.Contenu;
        }

        public override string ToString() => Titre;
    }
}
