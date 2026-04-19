using Microsoft.Win32;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Helpers;
using smartest_desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace smartest_desktop.ViewModels
{
    public class CoursViewModel : BaseViewModel
    {
        // ══════════════════════════════════════════════════════════
        //  Services
        // ══════════════════════════════════════════════════════════
        private readonly LocalCoursService _coursService;
        private readonly ImportService _importService;

        // ══════════════════════════════════════════════════════════
        //  PROPRIÉTÉS — Liste des cours
        // ══════════════════════════════════════════════════════════

        private ObservableCollection<CoursLocal> _cours = new();
        public ObservableCollection<CoursLocal> Cours
        {
            get => _cours;
            set => SetProperty(ref _cours, value);
        }

        private CoursLocal? _coursSelectionne;
        public CoursLocal? CoursSelectionne
        {
            get => _coursSelectionne;
            set
            {
                SetProperty(ref _coursSelectionne, value);
                // Mise à jour automatique de l'aperçu
                OnPropertyChanged(nameof(ContenuApercu));
                OnPropertyChanged(nameof(NombreQuestions));
                OnPropertyChanged(nameof(StatistiquesTexte));
                OnPropertyChanged(nameof(HasCoursSelectionne));
            }
        }

        // ══════════════════════════════════════════════════════════
        //  PROPRIÉTÉS — Aperçu et infos
        // ══════════════════════════════════════════════════════════

        /// <summary>Aperçu du contenu du cours sélectionné</summary>
        public string ContenuApercu
        {
            get
            {
                if (CoursSelectionne == null)
                    return "Sélectionnez un cours pour voir son contenu.";

                // Charger le contenu complet depuis SQLite
                var cours = _coursService.GetById(CoursSelectionne.Id);
                return cours?.Contenu ?? "(contenu vide)";
            }
        }

        /// <summary>Nombre de questions du cours sélectionné</summary>
        public int NombreQuestions =>
            CoursSelectionne == null
                ? 0
                : _coursService.NombreQuestions(CoursSelectionne.Id);

        /// <summary>Statistiques du contenu (mots, lignes)</summary>
        public string StatistiquesTexte
        {
            get
            {
                if (CoursSelectionne == null) return string.Empty;
                var cours = _coursService.GetById(CoursSelectionne.Id);
                if (cours == null) return string.Empty;

                var (mots, lignes, chars) = ImportService.Statistiques(cours.Contenu);
                return $"{mots} mots  •  {lignes} lignes  •  {chars} caractères";
            }
        }

        public bool HasCoursSelectionne => CoursSelectionne != null;

        // ══════════════════════════════════════════════════════════
        //  PROPRIÉTÉS — Recherche
        // ══════════════════════════════════════════════════════════

        private string _recherche = string.Empty;
        public string Recherche
        {
            get => _recherche;
            set
            {
                SetProperty(ref _recherche, value);
                ExecuteRecherche();
            }
        }

        // ══════════════════════════════════════════════════════════
        //  PROPRIÉTÉS — Import en cours
        // ══════════════════════════════════════════════════════════

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                OnPropertyChanged(nameof(IsNotLoading));
            }
        }
        public bool IsNotLoading => !_isLoading;

        private string _messageStatut = string.Empty;
        public string MessageStatut
        {
            get => _messageStatut;
            set => SetProperty(ref _messageStatut, value);
        }

        private bool _hasError;
        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        // ══════════════════════════════════════════════════════════
        //  COMMANDES
        // ══════════════════════════════════════════════════════════

        public ICommand ImporterPdfCommand { get; }
        public ICommand ImporterWordCommand { get; }
        public ICommand ImporterFichierCommand { get; }  // tous formats
        public ICommand SupprimerCommand { get; }
        public ICommand ModifierTitreCommand { get; }
        public ICommand RafraichirCommand { get; }
        public ICommand OuvrirQuestionsCommand { get; }

        // ══════════════════════════════════════════════════════════
        //  CONSTRUCTEUR
        // ══════════════════════════════════════════════════════════

        public CoursViewModel()
        {
            _coursService = new LocalCoursService();
            _importService = new ImportService();

            // Initialiser les commandes
            ImporterPdfCommand = new RelayCommand(_ => ExecuteImporterPdf(), _ => IsNotLoading);
            ImporterWordCommand = new RelayCommand(_ => ExecuteImporterWord(), _ => IsNotLoading);
            ImporterFichierCommand = new RelayCommand(_ => ExecuteImporterFichier(), _ => IsNotLoading);
            SupprimerCommand = new RelayCommand(_ => ExecuteSupprimer(), _ => HasCoursSelectionne);
            ModifierTitreCommand = new RelayCommand(_ => ExecuteModifierTitre(), _ => HasCoursSelectionne);
            RafraichirCommand = new RelayCommand(_ => ChargerCours());
            OuvrirQuestionsCommand = new RelayCommand(_ => ExecuteOuvrirQuestions(), _ => HasCoursSelectionne);

            // Charger la liste au démarrage
            ChargerCours();
        }

        // ══════════════════════════════════════════════════════════
        //  CHARGEMENT
        // ══════════════════════════════════════════════════════════

        private void ChargerCours()
        {
            try
            {
                var liste = _coursService.GetAllSansQuestions();
                Cours = new ObservableCollection<CoursLocal>(liste);
                MessageStatut = $"{Cours.Count} cours chargé(s)";
                HasError = false;
            }
            catch (Exception ex)
            {
                HasError = true;
                MessageStatut = $"Erreur chargement : {ex.Message}";
            }
        }

        // ══════════════════════════════════════════════════════════
        //  IMPORT PDF
        // ══════════════════════════════════════════════════════════

        private void ExecuteImporterPdf()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Importer un cours PDF",
                Filter = "Fichiers PDF|*.pdf",
            };

            if (dialog.ShowDialog() != true) return;

            ExecuteImport(dialog.FileName);
        }

        // ══════════════════════════════════════════════════════════
        //  IMPORT WORD
        // ══════════════════════════════════════════════════════════

        private void ExecuteImporterWord()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Importer un cours Word",
                Filter = "Fichiers Word|*.docx",
            };

            if (dialog.ShowDialog() != true) return;

            ExecuteImport(dialog.FileName);
        }

        // ══════════════════════════════════════════════════════════
        //  IMPORT TOUS FORMATS
        // ══════════════════════════════════════════════════════════

        private void ExecuteImporterFichier()
        {
            var dialog = new OpenFileDialog
            {
                Title = "Importer un cours",
                Filter = ImportService.FiltreOpenFileDialog,
            };

            if (dialog.ShowDialog() != true) return;

            ExecuteImport(dialog.FileName);
        }

        // ══════════════════════════════════════════════════════════
        //  LOGIQUE COMMUNE D'IMPORT
        // ══════════════════════════════════════════════════════════

        private void ExecuteImport(string cheminFichier)
        {
            try
            {
                IsLoading = true;
                HasError = false;
                MessageStatut = "Import en cours...";

                // Demander un titre personnalisé
                string nomFichier = System.IO.Path.GetFileNameWithoutExtension(cheminFichier);
                string titre = Microsoft.VisualBasic.Interaction.InputBox(
                    "Titre du cours (laisser vide pour utiliser le nom du fichier) :",
                    "Titre du cours",
                    nomFichier
                );

                // Import
                var cours = _importService.ImporterFichier(cheminFichier, titre);

                // Rafraîchir la liste
                ChargerCours();

                // Sélectionner le nouveau cours
                CoursSelectionne = Cours.FirstOrDefault(c => c.Id == cours.Id);

                var (mots, _, _) = ImportService.Statistiques(cours.Contenu);
                MessageStatut = $"✅ Cours importé : '{cours.Titre}' ({mots} mots)";
            }
            catch (Exception ex)
            {
                HasError = true;
                MessageStatut = $"❌ {ex.Message}";
                MessageBox.Show(ex.Message, "Erreur d'import",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  MODIFIER TITRE
        // ══════════════════════════════════════════════════════════

        private void ExecuteModifierTitre()
        {
            if (CoursSelectionne == null) return;

            string nouveauTitre = Microsoft.VisualBasic.Interaction.InputBox(
                "Nouveau titre du cours :",
                "Modifier le titre",
                CoursSelectionne.Titre
            );

            if (string.IsNullOrWhiteSpace(nouveauTitre)) return;
            if (nouveauTitre == CoursSelectionne.Titre) return;

            try
            {
                _coursService.Modifier(CoursSelectionne.Id, nouveauTitre);
                ChargerCours();
                MessageStatut = $"✅ Titre modifié : '{nouveauTitre}'";
            }
            catch (Exception ex)
            {
                HasError = true;
                MessageStatut = $"❌ {ex.Message}";
            }
        }

        // ══════════════════════════════════════════════════════════
        //  SUPPRIMER
        // ══════════════════════════════════════════════════════════

        private void ExecuteSupprimer()
        {
            if (CoursSelectionne == null) return;

            int nbQuestions = _coursService.NombreQuestions(CoursSelectionne.Id);

            // Message d'avertissement si des questions sont liées
            string message = nbQuestions > 0
                ? $"Supprimer '{CoursSelectionne.Titre}' ?\n\n" +
                  $"⚠️ {nbQuestions} question(s) associée(s) seront également supprimées."
                : $"Supprimer '{CoursSelectionne.Titre}' ?";

            var result = MessageBox.Show(
                message,
                "Confirmer la suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result != MessageBoxResult.Yes) return;

            try
            {
                int id = CoursSelectionne.Id;
                string titre = CoursSelectionne.Titre;
                CoursSelectionne = null;

                _coursService.Supprimer(id);
                ChargerCours();
                MessageStatut = $"✅ Cours supprimé : '{titre}'";
            }
            catch (Exception ex)
            {
                HasError = true;
                MessageStatut = $"❌ {ex.Message}";
            }
        }

        // ══════════════════════════════════════════════════════════
        //  OUVRIR QUESTIONS
        // ══════════════════════════════════════════════════════════

        private void ExecuteOuvrirQuestions()
        {
            if (CoursSelectionne == null) return;

            // Sera implémenté dans EPIC 3
            // Ouvre QuestionWindow avec ce cours présélectionné
            MessageBox.Show(
                $"Génération de questions pour :\n'{CoursSelectionne.Titre}'\n\n(Disponible dans EPIC 3)",
                "Questions",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        // ══════════════════════════════════════════════════════════
        //  RECHERCHE
        // ══════════════════════════════════════════════════════════

        private void ExecuteRecherche()
        {
            try
            {
                var resultats = _coursService.Rechercher(Recherche);
                Cours = new ObservableCollection<CoursLocal>(resultats);
                MessageStatut = string.IsNullOrWhiteSpace(Recherche)
                    ? $"{Cours.Count} cours chargé(s)"
                    : $"{Cours.Count} résultat(s) pour '{Recherche}'";
            }
            catch (Exception ex)
            {
                HasError = true;
                MessageStatut = $"❌ {ex.Message}";
            }
        }
    }
}