using Microsoft.Win32;
using smartest_desktop.Data;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Helpers;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfApp = System.Windows.Application;

namespace smartest_desktop.ViewModels
{
    // ═══════════════════════════════════════════════════════════════════════════
    // CategorieItem
    // ═══════════════════════════════════════════════════════════════════════════
    public class CategorieItem : BaseViewModel
    {
        private string _nom = string.Empty;
        public string Nom
        {
            get => _nom;
            set
            {
                SetProperty(ref _nom, value);
                OnPropertyChanged(nameof(PeutSupprimer));
            }
        }

        private bool _isActive;
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public bool PeutSupprimer => Nom != "Tous les cours";
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // CoursViewModel
    // ═══════════════════════════════════════════════════════════════════════════
    public class CoursViewModel : BaseViewModel
    {
        private readonly LocalDbContext _db;

        // ── Prof info ─────────────────────────────────────────────────────────
        private string _professeurNom = string.Empty;
        public string ProfesseurNom
        {
            get => _professeurNom;
            set => SetProperty(ref _professeurNom, value);
        }

        private string _professeurEmail = string.Empty;
        public string ProfesseurEmail
        {
            get => _professeurEmail;
            set => SetProperty(ref _professeurEmail, value);
        }

        // ── Categories ────────────────────────────────────────────────────────
        public ObservableCollection<CategorieItem> Categories { get; set; } = new();
        public Dictionary<string, ObservableCollection<CoursLocal>> CoursParCategorie { get; set; } = new();

        private string _categorieSelectionnee = "Tous les cours";
        public string CategorieSelectionnee
        {
            get => _categorieSelectionnee;
            set
            {
                foreach (var cat in Categories)
                    cat.IsActive = cat.Nom == value;
                SetProperty(ref _categorieSelectionnee, value);
                RefreshCoursDisplay();
            }
        }

        private string _nouvelleCategorie = string.Empty;
        public string NouvelleCategorie
        {
            get => _nouvelleCategorie;
            set => SetProperty(ref _nouvelleCategorie, value);
        }

        // ── Cours list ────────────────────────────────────────────────────────
        private ObservableCollection<CoursLocal> _coursFiltres = new();
        public ObservableCollection<CoursLocal> CoursFiltres
        {
            get => _coursFiltres;
            set
            {
                SetProperty(ref _coursFiltres, value);
                OnPropertyChanged(nameof(HasAnyCours));
                OnPropertyChanged(nameof(HasNoCours));
            }
        }

        private CoursLocal? _coursSelectionne;
        public CoursLocal? CoursSelectionne
        {
            get => _coursSelectionne;
            set
            {
                SetProperty(ref _coursSelectionne, value);
                OnPropertyChanged(nameof(HasCoursSelectionne));
                OnPropertyChanged(nameof(HasNoCoursSelectionne));
            }
        }

        public bool HasCoursSelectionne => CoursSelectionne != null;
        public bool HasNoCoursSelectionne => CoursSelectionne == null;
        public bool HasAnyCours => CoursFiltres.Count > 0;
        public bool HasNoCours => CoursFiltres.Count == 0;

        // ── State ─────────────────────────────────────────────────────────────
        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); }
        }

        private string _successMessage = string.Empty;
        public string SuccessMessage
        {
            get => _successMessage;
            set { SetProperty(ref _successMessage, value); OnPropertyChanged(nameof(HasSuccess)); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(IsNotLoading)); }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);
        public bool IsNotLoading => !IsLoading;

        // ── Commands ──────────────────────────────────────────────────────────
        public ICommand SelectionnerCategorieCommand { get; }
        public ICommand SelectionnerCoursCommand { get; }
        public ICommand AjouterCategorieCommand { get; }
        public ICommand SupprimerCategorieCommand { get; }
        public ICommand ImporterCommand { get; }
        public ICommand SupprimerCommand { get; }
        public ICommand RetourDashboardCommand { get; }
        public ICommand LogoutCommand { get; }

        public event Action? NavigateToDashboardRequested;

        // ── Constructor ───────────────────────────────────────────────────────
        public CoursViewModel()
        {
            _db = App.LocalDb;

            ProfesseurNom = WpfApp.Current.Properties["Nom"]?.ToString() ?? "Professeur";
            ProfesseurEmail = WpfApp.Current.Properties["Email"]?.ToString() ?? string.Empty;

            SelectionnerCategorieCommand = new RelayCommand(param =>
            {
                if (param is CategorieItem item)
                    CategorieSelectionnee = item.Nom;
            });

            SelectionnerCoursCommand = new RelayCommand(
                param => CoursSelectionne = param as CoursLocal);

            AjouterCategorieCommand = new RelayCommand(
                _ => AjouterCategorie(),
                _ => !string.IsNullOrWhiteSpace(NouvelleCategorie));

            SupprimerCategorieCommand = new RelayCommand(
                param =>
                {
                    if (param is CategorieItem item)
                        _ = SupprimerCategorie(item.Nom);
                },
                param => param is CategorieItem item && item.PeutSupprimer);

            ImporterCommand = new RelayCommand(
                async _ => await ImporterCours(),
                _ => IsNotLoading);

            SupprimerCommand = new RelayCommand(
                async _ => await SupprimerCours(),
                _ => CoursSelectionne != null && IsNotLoading);

            RetourDashboardCommand = new RelayCommand(
                _ => NavigateToDashboardRequested?.Invoke());

            LogoutCommand = new RelayCommand(_ => ExecuteLogout());

            Categories.Add(new CategorieItem { Nom = "Tous les cours", IsActive = true });
            CoursParCategorie["Tous les cours"] = new ObservableCollection<CoursLocal>();
            CoursFiltres = new ObservableCollection<CoursLocal>();

            _ = ChargerCours();
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private CategorieItem? TrouverCategorie(string nom)
            => Categories.FirstOrDefault(c => c.Nom == nom);

        private void AjouterCategorieInterne(string nom)
        {
            if (TrouverCategorie(nom) != null) return;
            Categories.Add(new CategorieItem { Nom = nom, IsActive = false });
            CoursParCategorie[nom] = new ObservableCollection<CoursLocal>();
        }

        // ── Data loading ──────────────────────────────────────────────────────
        private async Task ChargerCours()
        {
            IsLoading = true;
            ErrorMessage = string.Empty;
            try
            {
                var liste = await Task.Run(() => _db.Cours.ToList());

                foreach (var cat in CoursParCategorie.Keys.ToList())
                    CoursParCategorie[cat].Clear();

                foreach (var cat in Categories.Where(c => c.Nom != "Tous les cours").ToList())
                    Categories.Remove(cat);

                foreach (var cours in liste)
                {
                    CoursParCategorie["Tous les cours"].Add(cours);
                    string cat = string.IsNullOrWhiteSpace(cours.Categorie) ? "Non catégorisé" : cours.Categorie;
                    AjouterCategorieInterne(cat);
                    CoursParCategorie[cat].Add(cours);
                }

                RefreshCoursDisplay();
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Erreur chargement : {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void RefreshCoursDisplay()
        {
            CoursFiltres.Clear();
            if (CoursParCategorie.TryGetValue(CategorieSelectionnee, out var collection))
                foreach (var c in collection)
                    CoursFiltres.Add(c);

            OnPropertyChanged(nameof(HasAnyCours));
            OnPropertyChanged(nameof(HasNoCours));
        }

        // ── Category management ───────────────────────────────────────────────
        private void AjouterCategorie()
        {
            if (string.IsNullOrWhiteSpace(NouvelleCategorie)) return;
            string nom = NouvelleCategorie.Trim();

            if (TrouverCategorie(nom) != null)
            {
                ErrorMessage = "Cette catégorie existe déjà !";
                SuccessMessage = string.Empty;
                return;
            }

            AjouterCategorieInterne(nom);
            SuccessMessage = $"Catégorie \"{nom}\" créée !";
            ErrorMessage = string.Empty;
            NouvelleCategorie = string.Empty;
        }

        private async Task SupprimerCategorie(string nomCategorie)
        {
            if (nomCategorie == "Tous les cours") return;

            var result = MessageBox.Show(
                $"Supprimer la catégorie \"{nomCategorie}\" ?\nLes cours seront déplacés vers \"Non catégorisé\".",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            const string fallback = "Non catégorisé";
            AjouterCategorieInterne(fallback);

            foreach (var cours in CoursParCategorie[nomCategorie].ToList())
            {
                cours.Categorie = fallback;
                CoursParCategorie[fallback].Add(cours);
                var coursDb = await _db.Cours.FindAsync(cours.Id);
                if (coursDb != null) coursDb.Categorie = fallback;
            }

            var item = TrouverCategorie(nomCategorie);
            if (item != null) Categories.Remove(item);
            CoursParCategorie.Remove(nomCategorie);

            CategorieSelectionnee = "Tous les cours";
            SuccessMessage = "Catégorie supprimée.";
            ErrorMessage = string.Empty;

            await _db.SaveChangesAsync();
        }

        // ── Import ────────────────────────────────────────────────────────────
        private async Task ImporterCours()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Importer un cours",
                Filter = "Documents|*.pdf;*.docx;*.txt|PDF|*.pdf|Word|*.docx|Texte|*.txt",
                Multiselect = false
            };

            if (dialog.ShowDialog() != true) return;

            IsLoading = true;
            ErrorMessage = string.Empty;
            SuccessMessage = string.Empty;

            try
            {
                string chemin = dialog.FileName;
                string nom = Path.GetFileNameWithoutExtension(chemin);
                string extension = Path.GetExtension(chemin).ToLower();
                long taille = new FileInfo(chemin).Length;
                string contenu = string.Empty;

                await Task.Run(() =>
                {
                    contenu = extension switch
                    {
                        ".txt" => File.ReadAllText(chemin),
                        ".pdf" => ExtraireTextePdf(chemin),
                        ".docx" => ExtraireTexteDocx(chemin),
                        _ => File.ReadAllText(chemin)
                    };
                });

                string categorieCible = CategorieSelectionnee != "Tous les cours"
                    ? CategorieSelectionnee
                    : "Non catégorisé";

                var cours = new CoursLocal
                {
                    Titre = nom,
                    Contenu = contenu,
                    CheminFichier = chemin,
                    DateImport = System.DateTime.Now,
                    TypeFichier = extension.TrimStart('.').ToUpper(),
                    TailleFichier = taille,
                    Categorie = categorieCible
                };

                _db.Cours.Add(cours);
                await _db.SaveChangesAsync();

                CoursParCategorie["Tous les cours"].Add(cours);
                AjouterCategorieInterne(categorieCible);
                CoursParCategorie[categorieCible].Add(cours);

                RefreshCoursDisplay();
                SuccessMessage = $"Cours \"{nom}\" importé dans \"{categorieCible}\" !";
                ErrorMessage = string.Empty;
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Erreur import : {ex.Message}";
                SuccessMessage = string.Empty;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ── Delete ────────────────────────────────────────────────────────────
        private async Task SupprimerCours()
        {
            if (CoursSelectionne == null) return;

            var result = MessageBox.Show(
                $"Supprimer le cours \"{CoursSelectionne.Titre}\" ?",
                "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;
            try
            {
                var toDelete = CoursSelectionne;
                _db.Cours.Remove(toDelete);
                await _db.SaveChangesAsync();

                foreach (var kvp in CoursParCategorie)
                    kvp.Value.Remove(toDelete);

                CoursFiltres.Remove(toDelete);
                CoursSelectionne = null;
                SuccessMessage = "Cours supprimé.";
                ErrorMessage = string.Empty;

                OnPropertyChanged(nameof(HasAnyCours));
                OnPropertyChanged(nameof(HasNoCours));
            }
            catch (System.Exception ex)
            {
                ErrorMessage = $"Erreur suppression : {ex.Message}";
                SuccessMessage = string.Empty;
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ── Logout ────────────────────────────────────────────────────────────
        private void ExecuteLogout()
        {
            var result = MessageBox.Show(
                "Voulez-vous vraiment vous déconnecter ?",
                "Déconnexion", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            WpfApp.Current.Properties["Token"] = null;
            WpfApp.Current.Properties["Nom"] = null;
            WpfApp.Current.Properties["Email"] = null;

            var login = new Views.LoginWindow();
            login.Show();

            foreach (Window w in WpfApp.Current.Windows)
            {
                if (w is Views.CoursWindow) { w.Close(); break; }
            }
        }

        // ── Text extractors ───────────────────────────────────────────────────
        private string ExtraireTextePdf(string chemin)
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                using var doc = UglyToad.PdfPig.PdfDocument.Open(chemin);
                foreach (var page in doc.GetPages())
                    sb.AppendLine(page.Text);
                return sb.ToString();
            }
            catch { return $"[Contenu PDF — {Path.GetFileName(chemin)}]"; }
        }

        private string ExtraireTexteDocx(string chemin)
        {
            try
            {
                using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(chemin, false);
                var body = doc.MainDocumentPart?.Document?.Body;
                return body?.InnerText ?? string.Empty;
            }
            catch { return $"[Contenu DOCX — {Path.GetFileName(chemin)}]"; }
        }
    }
}