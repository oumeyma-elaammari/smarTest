using Microsoft.Win32;
using smartest_desktop.Data;
using smartest_desktop.Data.LocalEntities;
using smartest_desktop.Helpers;
using smartest_desktop.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace smartest_desktop.ViewModels
{
    public class ExamenResultViewModel : BaseViewModel
    {
        private readonly LocalDbContext     _db;
        private readonly ExamenLocalService _service;

        // ── Données de l'examen ───────────────────────────────────────────────

        private string _titreExamen = string.Empty;
        public string TitreExamen
        {
            get => _titreExamen;
            set => SetProperty(ref _titreExamen, value);
        }

        public int    Duree     { get; }
        public string Difficulte { get; }

        public string CoursSourceLabel { get; }

        // ── Liste de questions ────────────────────────────────────────────────

        public ObservableCollection<QuestionExamen> Questions { get; } = new();

        public int NombreQuestions => Questions.Count;

        // ── Question sélectionnée ─────────────────────────────────────────────

        private QuestionExamen? _questionSelectionnee;
        public QuestionExamen? QuestionSelectionnee
        {
            get => _questionSelectionnee;
            set
            {
                if (_questionSelectionnee?.IsEditing == true)
                    AnnulerEditionInterne();

                SetProperty(ref _questionSelectionnee, value);
                OnPropertyChanged(nameof(HasQuestion));
                OnPropertyChanged(nameof(NoQuestion));
                RefreshEditionCommands();
            }
        }

        public bool HasQuestion => QuestionSelectionnee != null;
        public bool NoQuestion  => QuestionSelectionnee == null;

        // ── Messages ──────────────────────────────────────────────────────────

        private string _successMessage = string.Empty;
        public string SuccessMessage
        {
            get => _successMessage;
            set { SetProperty(ref _successMessage, value); OnPropertyChanged(nameof(HasSuccess)); }
        }
        public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);

        // ── Sauvegarde pour annulation d'édition ──────────────────────────────

        private QuestionExamen? _backup;

        // ── Commandes ─────────────────────────────────────────────────────────

        public ICommand SelectionnerCommand      { get; }
        public ICommand EditerCommand            { get; }
        public ICommand SauvegarderEditionCommand { get; }
        public ICommand AnnulerEditionCommand    { get; }
        public ICommand SupprimerCommand         { get; }
        public ICommand AttacherImageCommand     { get; }
        public ICommand SupprimerImageCommand    { get; }
        public ICommand ValiderExamenCommand     { get; }
        public ICommand RegenerarCommand         { get; }
        public ICommand RetourCommand            { get; }

        // ── Événements ────────────────────────────────────────────────────────

        public event Action<List<QuestionExamen>, string, int, string, string>? ExamenValide;
        public event Action? NavigationRegenerarRequested;
        public event Action? NavigationRetourRequested;

        // ── Constructeur ──────────────────────────────────────────────────────

        public ExamenResultViewModel(
            List<QuestionExamen> questions,
            string titre,
            int    duree,
            string difficulte,
            string coursTitre)
        {
            _db      = App.LocalDb;
            _service = new ExamenLocalService(_db);

            TitreExamen    = titre;
            Duree          = duree;
            Difficulte     = difficulte;
            CoursSourceLabel = coursTitre;

            int n = 1;
            foreach (var q in questions)
            {
                q.Numero = n++;
                Questions.Add(q);
            }

            // ── Commandes ────────────────────────────────────────────────────

            SelectionnerCommand = new RelayCommand(p =>
            {
                if (p is not QuestionExamen q) return;

                foreach (var item in Questions)
                    item.IsSelected = false;

                q.IsSelected = true;
                QuestionSelectionnee = q;
            });

            EditerCommand = new RelayCommand(
                _ =>
                {
                    if (QuestionSelectionnee == null) return;
                    _backup = Clone(QuestionSelectionnee);
                    QuestionSelectionnee.IsEditing = true;
                    RefreshEditionCommands();
                },
                _ => QuestionSelectionnee != null && QuestionSelectionnee.IsNotEditing);

            SauvegarderEditionCommand = new RelayCommand(
                _ =>
                {
                    if (QuestionSelectionnee == null) return;
                    QuestionSelectionnee.IsEditing = false;
                    _backup = null;
                    RefreshEditionCommands();
                    ShowSuccess("✅ Question modifiée avec succès");
                },
                _ => QuestionSelectionnee?.IsEditing == true);

            AnnulerEditionCommand = new RelayCommand(
                _ => AnnulerEditionInterne(),
                _ => QuestionSelectionnee?.IsEditing == true);

            SupprimerCommand = new RelayCommand(p =>
            {
                if (p is not QuestionExamen q) return;

                var res = MessageBox.Show(
                    $"Supprimer la question {q.Numero} ?\n\n\"{Tronquer(q.Enonce, 80)}\"",
                    "Supprimer",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (res != MessageBoxResult.Yes) return;

                if (QuestionSelectionnee == q)
                    QuestionSelectionnee = null;

                Questions.Remove(q);
                Renuméroter();
                OnPropertyChanged(nameof(NombreQuestions));
                ShowSuccess("🗑 Question supprimée");
            });

            AttacherImageCommand = new RelayCommand(_ =>
            {
                if (QuestionSelectionnee == null) return;

                var dlg = new OpenFileDialog
                {
                    Title  = "Sélectionner une image",
                    Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp"
                };

                if (dlg.ShowDialog() != true) return;

                try
                {
                    byte[] bytes  = File.ReadAllBytes(dlg.FileName);
                    string base64 = Convert.ToBase64String(bytes);
                    string ext    = Path.GetExtension(dlg.FileName).TrimStart('.').ToLower();

                    QuestionSelectionnee.ImageBase64 = base64;
                    QuestionSelectionnee.ImageType   = ext;
                    QuestionSelectionnee.ImageNom    = Path.GetFileName(dlg.FileName);

                    ShowSuccess("🖼 Image attachée avec succès");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la lecture de l'image :\n{ex.Message}",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            SupprimerImageCommand = new RelayCommand(_ =>
            {
                if (QuestionSelectionnee == null) return;
                QuestionSelectionnee.ImageBase64 = string.Empty;
                QuestionSelectionnee.ImageType   = string.Empty;
                QuestionSelectionnee.ImageNom    = string.Empty;
                ShowSuccess("🗑 Image supprimée");
            });

            ValiderExamenCommand = new RelayCommand(
                async _ => await ValiderExamen(),
                _ => Questions.Count > 0);

            RegenerarCommand = new RelayCommand(_ =>
            {
                var res = MessageBox.Show(
                    "Regénérer l'examen ?\nLes questions actuelles seront perdues.",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                    NavigationRegenerarRequested?.Invoke();
            });

            RetourCommand = new RelayCommand(_ =>
            {
                var res = MessageBox.Show(
                    "Quitter sans valider ?\nL'examen généré sera perdu.",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                    NavigationRetourRequested?.Invoke();
            });
        }

        // ── Sauvegarde ────────────────────────────────────────────────────────

        private async Task ValiderExamen()
        {
            if (Questions.Count == 0) return;

            if (QuestionSelectionnee?.IsEditing == true)
                AnnulerEditionInterne();

            var res = MessageBox.Show(
                $"Valider l'examen \"{TitreExamen}\" ?\n\n" +
                $"• {Questions.Count} questions\n" +
                $"• Difficulté : {Difficulte}\n" +
                $"• Durée : {Duree} min\n\n" +
                $"L'examen sera sauvegardé localement.",
                "Valider l'examen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res != MessageBoxResult.Yes) return;

            try
            {
                var examen = new ExamenLocal
                {
                    Titre       = TitreExamen,
                    Duree       = Duree,
                    Statut      = "BROUILLON",
                    DateCreation = DateTime.Now
                };

                await _service.SauvegarderAsync(examen, Questions.ToList(), CoursSourceLabel);

                ExamenValide?.Invoke(
                    Questions.ToList(), TitreExamen, Duree, Difficulte, CoursSourceLabel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la sauvegarde :\n{ex.Message}",
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void AnnulerEditionInterne()
        {
            if (QuestionSelectionnee == null || _backup == null) return;
            RestoreFrom(_backup, QuestionSelectionnee);
            QuestionSelectionnee.IsEditing = false;
            _backup = null;
            RefreshEditionCommands();
        }

        private void RefreshEditionCommands()
        {
            ((RelayCommand)EditerCommand).RaiseCanExecuteChanged();
            ((RelayCommand)SauvegarderEditionCommand).RaiseCanExecuteChanged();
            ((RelayCommand)AnnulerEditionCommand).RaiseCanExecuteChanged();
        }

        private void Renuméroter()
        {
            int n = 1;
            foreach (var q in Questions) q.Numero = n++;
        }

        private void ShowSuccess(string msg)
        {
            SuccessMessage = msg;
            Task.Delay(2500).ContinueWith(_ =>
                Application.Current.Dispatcher.Invoke(() => SuccessMessage = string.Empty));
        }

        private static string Tronquer(string t, int max) =>
            t.Length <= max ? t : t[..max] + "…";

        private static QuestionExamen Clone(QuestionExamen src) => new()
        {
            Numero            = src.Numero,
            Type              = src.Type,
            Enonce            = src.Enonce,
            Difficulte        = src.Difficulte,
            Explication       = src.Explication,
            OptionA           = src.OptionA,
            OptionB           = src.OptionB,
            OptionC           = src.OptionC,
            OptionD           = src.OptionD,
            ReponseCorrecte   = src.ReponseCorrecte,
            OptionACorrecte   = src.OptionACorrecte,
            OptionBCorrecte   = src.OptionBCorrecte,
            OptionCCorrecte   = src.OptionCCorrecte,
            OptionDCorrecte   = src.OptionDCorrecte,
            ReponseModele     = src.ReponseModele,
            ImageBase64       = src.ImageBase64,
            ImageType         = src.ImageType,
            ImageNom          = src.ImageNom,
        };

        private static void RestoreFrom(QuestionExamen src, QuestionExamen dst)
        {
            dst.Enonce          = src.Enonce;
            dst.Explication     = src.Explication;
            dst.OptionA         = src.OptionA;
            dst.OptionB         = src.OptionB;
            dst.OptionC         = src.OptionC;
            dst.OptionD         = src.OptionD;
            dst.ReponseCorrecte = src.ReponseCorrecte;
            dst.OptionACorrecte = src.OptionACorrecte;
            dst.OptionBCorrecte = src.OptionBCorrecte;
            dst.OptionCCorrecte = src.OptionCCorrecte;
            dst.OptionDCorrecte = src.OptionDCorrecte;
            dst.ReponseModele   = src.ReponseModele;
        }
    }
}
