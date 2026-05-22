using smartest_desktop.Helpers;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using WpfApp = System.Windows.Application;

namespace smartest_desktop.ViewModels
{
    public sealed class SettingsAccountViewModel : BaseViewModel
    {
        private string _nom = string.Empty;
        public string Nom
        {
            get => _nom;
            private set
            {
                if (!SetProperty(ref _nom, value))
                    return;
                OnPropertyChanged(nameof(HasCompteChanges));
                _enregistrerCommand?.RaiseCanExecuteChanged();
            }
        }

        private string _email = string.Empty;
        public string Email
        {
            get => _email;
            private set
            {
                if (!SetProperty(ref _email, value))
                    return;
                OnPropertyChanged(nameof(CompteEmail));
                OnPropertyChanged(nameof(HasCompteChanges));
                _enregistrerCommand?.RaiseCanExecuteChanged();
            }
        }

        private string _compteNom = string.Empty;
        public string CompteNom
        {
            get => _compteNom;
            set
            {
                if (!SetProperty(ref _compteNom, value))
                    return;
                OnPropertyChanged(nameof(HasCompteChanges));
                _enregistrerCommand?.RaiseCanExecuteChanged();
            }
        }

        public string CompteEmail => Email;

        private string _messageCompte = string.Empty;
        public string MessageCompte
        {
            get => _messageCompte;
            private set => SetProperty(ref _messageCompte, value);
        }

        private Brush _messageCompteBrush = Brushes.Transparent;
        public Brush MessageCompteBrush
        {
            get => _messageCompteBrush;
            private set => SetProperty(ref _messageCompteBrush, value);
        }

        private string _ancienMotDePasse = string.Empty;
        public string AncienMotDePasse
        {
            get => _ancienMotDePasse;
            set
            {
                if (!SetProperty(ref _ancienMotDePasse, value))
                    return;
                OnPropertyChanged(nameof(HasPasswordChanges));
                _enregistrerMotDePasseCommand?.RaiseCanExecuteChanged();
            }
        }

        private string _nouveauMotDePasse = string.Empty;
        public string NouveauMotDePasse
        {
            get => _nouveauMotDePasse;
            set
            {
                if (!SetProperty(ref _nouveauMotDePasse, value))
                    return;
                OnPropertyChanged(nameof(HasPasswordChanges));
                RefreshNewPasswordRulesUI();
                _enregistrerMotDePasseCommand?.RaiseCanExecuteChanged();
            }
        }

        private string _confirmationMotDePasse = string.Empty;
        public string ConfirmationMotDePasse
        {
            get => _confirmationMotDePasse;
            set
            {
                if (!SetProperty(ref _confirmationMotDePasse, value))
                    return;
                OnPropertyChanged(nameof(HasPasswordChanges));
                RefreshNewPasswordRulesUI();
                _enregistrerMotDePasseCommand?.RaiseCanExecuteChanged();
            }
        }

        private string _messageSecurite = string.Empty;
        public string MessageSecurite
        {
            get => _messageSecurite;
            private set => SetProperty(ref _messageSecurite, value);
        }

        private Brush _messageSecuriteBrush = Brushes.Transparent;
        public Brush MessageSecuriteBrush
        {
            get => _messageSecuriteBrush;
            private set => SetProperty(ref _messageSecuriteBrush, value);
        }

        public bool HasMessageCompte => !string.IsNullOrWhiteSpace(MessageCompte);
        public bool HasMessageSecurite => !string.IsNullOrWhiteSpace(MessageSecurite);
        public bool IsSaving
        {
            get => _isSaving;
            private set
            {
                if (!SetProperty(ref _isSaving, value))
                    return;
                _enregistrerCommand?.RaiseCanExecuteChanged();
                _supprimerCompteCommand?.RaiseCanExecuteChanged();
            }
        }
        public bool IsSavingPassword
        {
            get => _isSavingPassword;
            private set
            {
                if (!SetProperty(ref _isSavingPassword, value))
                    return;
                _enregistrerMotDePasseCommand?.RaiseCanExecuteChanged();
                _supprimerCompteCommand?.RaiseCanExecuteChanged();
            }
        }

        public bool IsDeletingAccount
        {
            get => _isDeletingAccount;
            private set
            {
                if (!SetProperty(ref _isDeletingAccount, value))
                    return;
                _supprimerCompteCommand?.RaiseCanExecuteChanged();
            }
        }
        public bool HasCompteChanges =>
            !string.Equals((CompteNom ?? string.Empty).Trim(), (Nom ?? string.Empty).Trim(), StringComparison.Ordinal);
        public bool HasPasswordChanges =>
            !string.IsNullOrWhiteSpace(AncienMotDePasse) ||
            !string.IsNullOrWhiteSpace(NouveauMotDePasse) ||
            !string.IsNullOrWhiteSpace(ConfirmationMotDePasse);
        public bool AfficherFormulaireMotDePasse
        {
            get => _afficherFormulaireMotDePasse;
            private set => SetProperty(ref _afficherFormulaireMotDePasse, value);
        }

        public RelayCommand EnregistrerCommand => _enregistrerCommand;
        public RelayCommand EnregistrerMotDePasseCommand => _enregistrerMotDePasseCommand;
        public RelayCommand ToggleMotDePasseFormCommand => _toggleMotDePasseFormCommand;
        public RelayCommand AnnulerMotDePasseFormCommand => _annulerMotDePasseFormCommand;
        public RelayCommand SupprimerCompteCommand => _supprimerCompteCommand;
        public RelayCommand TesterCleGroqCommand => _testerCleGroqCommand;
        public RelayCommand EnregistrerCleGroqCommand => _enregistrerCleGroqCommand;
        public RelayCommand ModifierCleGroqCommand => _modifierCleGroqCommand;
        public RelayCommand AnnulerCleGroqCommand => _annulerCleGroqCommand;
        public RelayCommand AppliquerThemeClairCommand => _appliquerThemeClairCommand;
        public RelayCommand AppliquerThemeSombreCommand => _appliquerThemeSombreCommand;
        private readonly RelayCommand _enregistrerCommand;
        private readonly RelayCommand _enregistrerMotDePasseCommand;
        private readonly RelayCommand _toggleMotDePasseFormCommand;
        private readonly RelayCommand _annulerMotDePasseFormCommand;
        private readonly RelayCommand _supprimerCompteCommand;
        private readonly RelayCommand _testerCleGroqCommand;
        private readonly RelayCommand _enregistrerCleGroqCommand;
        private readonly RelayCommand _modifierCleGroqCommand;
        private readonly RelayCommand _annulerCleGroqCommand;
        private readonly RelayCommand _appliquerThemeClairCommand;
        private readonly RelayCommand _appliquerThemeSombreCommand;
        private readonly Services.AuthService _authService;
        private bool _isSaving;
        private bool _isSavingPassword;
        private bool _isDeletingAccount;
        private bool _afficherFormulaireMotDePasse;

        // ── Clé API Groq ──────────────────────────────────────────────────────
        private string _cleGroq = string.Empty;
        private string _cleGroqSnapshot = string.Empty;

        public string CleGroq
        {
            get => _cleGroq;
            set
            {
                if (!SetProperty(ref _cleGroq, value)) return;
                _testerCleGroqCommand?.RaiseCanExecuteChanged();
                _enregistrerCleGroqCommand?.RaiseCanExecuteChanged();
            }
        }

        private bool _isEditingGroqKey;
        public bool IsEditingGroqKey
        {
            get => _isEditingGroqKey;
            private set
            {
                if (!SetProperty(ref _isEditingGroqKey, value)) return;
                _modifierCleGroqCommand?.RaiseCanExecuteChanged();
                _annulerCleGroqCommand?.RaiseCanExecuteChanged();
                _enregistrerCleGroqCommand?.RaiseCanExecuteChanged();
            }
        }

        private string _statutGroq = string.Empty;
        public string StatutGroq
        {
            get => _statutGroq;
            private set => SetProperty(ref _statutGroq, value);
        }

        private Brush _statutGroqBrush = Brushes.Transparent;
        public Brush StatutGroqBrush
        {
            get => _statutGroqBrush;
            private set => SetProperty(ref _statutGroqBrush, value);
        }

        public bool HasStatutGroq => !string.IsNullOrWhiteSpace(StatutGroq);

        private bool _isTestingGroq;
        public bool IsTestingGroq
        {
            get => _isTestingGroq;
            private set
            {
                if (!SetProperty(ref _isTestingGroq, value)) return;
                _testerCleGroqCommand?.RaiseCanExecuteChanged();
                _enregistrerCleGroqCommand?.RaiseCanExecuteChanged();
                _modifierCleGroqCommand?.RaiseCanExecuteChanged();
                _annulerCleGroqCommand?.RaiseCanExecuteChanged();
            }
        }

        private bool _isSavingGroq;
        public bool IsSavingGroq
        {
            get => _isSavingGroq;
            private set
            {
                if (!SetProperty(ref _isSavingGroq, value)) return;
                _enregistrerCleGroqCommand?.RaiseCanExecuteChanged();
                _testerCleGroqCommand?.RaiseCanExecuteChanged();
                _modifierCleGroqCommand?.RaiseCanExecuteChanged();
                _annulerCleGroqCommand?.RaiseCanExecuteChanged();
            }
        }

        // ── Thème ─────────────────────────────────────────────────────────────
        private bool _themeSombre = App.EstThemeSombre;
        public bool ThemeSombre
        {
            get => _themeSombre;
            private set
            {
                if (_themeSombre == value) return;
                _themeSombre = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ThemeClair));
                App.AppliquerTheme(value);
            }
        }
        public bool ThemeClair => !_themeSombre;

        private static readonly Brush RuleOkBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16a34a"));
        private static readonly Brush RulePendingBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94a3b8"));
        private static readonly Brush RuleFailBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc2626"));

        private bool _pwdLenOk;
        private bool _pwdUpperOk;
        private bool _pwdDigitOk;
        private bool _pwdSpecialOk;
        private bool _pwdConfirmMatchOk;

        public bool PasswordRuleLengthOk => _pwdLenOk;
        public bool PasswordRuleUpperOk => _pwdUpperOk;
        public bool PasswordRuleDigitOk => _pwdDigitOk;
        public bool PasswordRuleSpecialOk => _pwdSpecialOk;

        public string PasswordRuleLengthGlyph => _pwdLenOk ? "\u2713" : "\u25CB";
        public string PasswordRuleUpperGlyph => _pwdUpperOk ? "\u2713" : "\u25CB";
        public string PasswordRuleDigitGlyph => _pwdDigitOk ? "\u2713" : "\u25CB";
        public string PasswordRuleSpecialGlyph => _pwdSpecialOk ? "\u2713" : "\u25CB";

        public Brush PasswordRuleLengthBrush => _pwdLenOk ? RuleOkBrush : RulePendingBrush;
        public Brush PasswordRuleUpperBrush => _pwdUpperOk ? RuleOkBrush : RulePendingBrush;
        public Brush PasswordRuleDigitBrush => _pwdDigitOk ? RuleOkBrush : RulePendingBrush;
        public Brush PasswordRuleSpecialBrush => _pwdSpecialOk ? RuleOkBrush : RulePendingBrush;

        public bool ShowPasswordConfirmRule => !string.IsNullOrEmpty(ConfirmationMotDePasse);
        public bool PasswordConfirmMatchesRule => _pwdConfirmMatchOk;

        public string PasswordConfirmRuleGlyph => !_pwdConfirmMatchOk ? "\u2715" : "\u2713";

        public Brush PasswordConfirmRuleBrush
        {
            get
            {
                if (!ShowPasswordConfirmRule)
                    return RulePendingBrush;
                return _pwdConfirmMatchOk ? RuleOkBrush : RuleFailBrush;
            }
        }

        public SettingsAccountViewModel()
        {
            _authService = new Services.AuthService();
            _enregistrerCommand = new RelayCommand(async _ => await EnregistrerAsync(), _ => HasCompteChanges && !IsSaving);
            _enregistrerMotDePasseCommand = new RelayCommand(async _ => await EnregistrerMotDePasseAsync(), _ => HasPasswordChanges && !IsSavingPassword);
            _toggleMotDePasseFormCommand = new RelayCommand(_ => OuvrirFormMotDePasse());
            _annulerMotDePasseFormCommand = new RelayCommand(_ => AnnulerFormMotDePasse());
            _supprimerCompteCommand = new RelayCommand(
                _ => { _ = SupprimerCompteAsync(); },
                _ => !IsDeletingAccount && !IsSaving && !IsSavingPassword);

            _testerCleGroqCommand = new RelayCommand(
                async _ => await TesterCleGroqAsync(),
                _ => !string.IsNullOrWhiteSpace(CleGroq) && !IsTestingGroq && !IsSavingGroq);

            _enregistrerCleGroqCommand = new RelayCommand(
                _ => EnregistrerCleGroq(),
                _ => IsEditingGroqKey && !string.IsNullOrWhiteSpace(CleGroq) && !IsSavingGroq && !IsTestingGroq);

            _modifierCleGroqCommand = new RelayCommand(
                _ => ModifierCleGroq(),
                _ => !IsEditingGroqKey && !IsTestingGroq && !IsSavingGroq);

            _annulerCleGroqCommand = new RelayCommand(
                _ => AnnulerModificationCleGroq(),
                _ => IsEditingGroqKey && !IsTestingGroq && !IsSavingGroq);

            _appliquerThemeClairCommand = new RelayCommand(_ => ThemeSombre = false);
            _appliquerThemeSombreCommand = new RelayCommand(_ => ThemeSombre = true);

            ChargerProfilLocal();
            RefreshNewPasswordRulesUI();
            ChargerCleGroq();
        }

        public void ResetDraft()
        {
            CompteNom = Nom;
            MessageCompte = string.Empty;
            MessageCompteBrush = Brushes.Transparent;
            MessageSecurite = string.Empty;
            MessageSecuriteBrush = Brushes.Transparent;
            ViderChampsMotDePasse();
            ChargerCleGroq();
            StatutGroq = string.Empty;
            IsEditingGroqKey = false;
            OnPropertyChanged(nameof(HasStatutGroq));
            _themeSombre = App.EstThemeSombre;
            OnPropertyChanged(nameof(ThemeSombre));
            OnPropertyChanged(nameof(ThemeClair));
            OnPropertyChanged(nameof(HasMessageCompte));
            OnPropertyChanged(nameof(HasMessageSecurite));
            OnPropertyChanged(nameof(HasCompteChanges));
            OnPropertyChanged(nameof(HasPasswordChanges));
            _enregistrerCommand.RaiseCanExecuteChanged();
            _enregistrerMotDePasseCommand.RaiseCanExecuteChanged();
            _supprimerCompteCommand.RaiseCanExecuteChanged();
            _testerCleGroqCommand.RaiseCanExecuteChanged();
            _enregistrerCleGroqCommand.RaiseCanExecuteChanged();
            _modifierCleGroqCommand.RaiseCanExecuteChanged();
            _annulerCleGroqCommand.RaiseCanExecuteChanged();
        }

        private void OuvrirFormMotDePasse()
        {
            AfficherFormulaireMotDePasse = true;
        }

        private void AnnulerFormMotDePasse()
        {
            AfficherFormulaireMotDePasse = false;
            ViderChampsMotDePasse();
            MessageSecurite = string.Empty;
            MessageSecuriteBrush = Brushes.Transparent;
            OnPropertyChanged(nameof(HasMessageSecurite));
            OnPropertyChanged(nameof(HasPasswordChanges));
            _enregistrerMotDePasseCommand.RaiseCanExecuteChanged();
        }

        private void ChargerProfilLocal()
        {
            var nomDefaut = WpfApp.Current.Properties["Nom"]?.ToString() ?? "Professeur";
            var emailDefaut = WpfApp.Current.Properties["Email"]?.ToString() ?? string.Empty;
            var (nom, email) = App.ChargerPreferencesProfilLocal(nomDefaut, emailDefaut);

            Nom = nom;
            Email = email;
            CompteNom = nom;

            App.AppliquerProfilCourant(nom, email);
        }

        private async Task EnregistrerAsync()
        {
            var nom = (CompteNom ?? string.Empty).Trim();

            if (!NomValide(nom))
            {
                SetMessage("Le nom doit contenir uniquement des lettres (2 à 50 caractères).", false);
                return;
            }

            var token = WpfApp.Current.Properties["Token"]?.ToString();
            if (string.IsNullOrWhiteSpace(token))
            {
                SetMessage("Session invalide. Veuillez vous reconnecter.", false);
                return;
            }

            IsSaving = true;
            SetMessage("Enregistrement en cours...", true);
            try
            {
                var (nomServeur, erreurServeur) = await _authService
                    .UpdateProfesseurProfilNomAsync(token, nom);

                if (!string.IsNullOrWhiteSpace(erreurServeur))
                {
                    SetMessage($"Mise à jour serveur refusée: {erreurServeur}", false);
                    return;
                }

                nom = (nomServeur ?? nom).Trim();
                var email = (Email ?? string.Empty).Trim();

                if (!App.EnregistrerPreferencesProfilLocal(nom, email, out var erreurLocal))
                {
                    SetMessage($"Profil serveur mis à jour, mais sauvegarde locale impossible: {erreurLocal}", false);
                    return;
                }

                Nom = nom;
                Email = email;
                App.AppliquerProfilCourant(nom, email);

                SetMessage("Modifications sauvegardées avec succès.", true);
                _ = MasquerMessageSuccesApresDelaiAsync();
                OnPropertyChanged(nameof(HasCompteChanges));
                _enregistrerCommand.RaiseCanExecuteChanged();
            }
            finally
            {
                IsSaving = false;
            }
        }

        private static bool NomValide(string nom)
        {
            if (string.IsNullOrWhiteSpace(nom))
                return false;
            return Regex.IsMatch(nom.Trim(), @"^[a-zA-ZÀ-ÿ\s\-]{2,50}$");
        }

        private static bool NouveauMotDePasseValide(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;
            return Regex.IsMatch(password, @"^(?=.*[A-Z])(?=.*[0-9])(?=.*[!@#$%^&*()_+\-=]).{8,}$");
        }

        private async Task EnregistrerMotDePasseAsync()
        {
            var oldPassword = AncienMotDePasse ?? string.Empty;
            var newPassword = NouveauMotDePasse ?? string.Empty;
            var confirmPassword = ConfirmationMotDePasse ?? string.Empty;

            if (string.IsNullOrWhiteSpace(oldPassword))
            {
                SetMessageSecurite("L'ancien mot de passe est obligatoire.", false);
                return;
            }

            if (!NouveauMotDePasseValide(newPassword))
            {
                SetMessageSecurite("Nouveau mot de passe: min 8 caractères, 1 majuscule, 1 chiffre et 1 caractère spécial.", false);
                return;
            }

            if (!string.Equals(newPassword, confirmPassword, StringComparison.Ordinal))
            {
                SetMessageSecurite("La confirmation du mot de passe ne correspond pas.", false);
                return;
            }

            var token = WpfApp.Current.Properties["Token"]?.ToString();
            if (string.IsNullOrWhiteSpace(token))
            {
                SetMessageSecurite("Session invalide. Veuillez vous reconnecter.", false);
                return;
            }

            IsSavingPassword = true;
            SetMessageSecurite("Modification en cours...", true);
            try
            {
                var error = await _authService.ChangeProfesseurPasswordAsync(
                    token,
                    oldPassword,
                    newPassword,
                    confirmPassword);

                if (!string.IsNullOrWhiteSpace(error))
                {
                    SetMessageSecurite(error, false);
                    return;
                }

                ViderChampsMotDePasse();
                AfficherFormulaireMotDePasse = false;
                SetMessageSecurite("Mot de passe modifié avec succès.", true);
                _ = MasquerMessageSecuriteSuccesApresDelaiAsync();
                OnPropertyChanged(nameof(HasPasswordChanges));
                _enregistrerMotDePasseCommand.RaiseCanExecuteChanged();
            }
            finally
            {
                IsSavingPassword = false;
            }
        }

        private void SetMessage(string message, bool success)
        {
            MessageCompte = message;
            MessageCompteBrush = success
                ? (Brush)new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16a34a"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc2626"));
            OnPropertyChanged(nameof(HasMessageCompte));
        }

        private void SetMessageSecurite(string message, bool success)
        {
            MessageSecurite = message;
            MessageSecuriteBrush = success
                ? (Brush)new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16a34a"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc2626"));
            OnPropertyChanged(nameof(HasMessageSecurite));
        }

        private void ViderChampsMotDePasse()
        {
            AncienMotDePasse = string.Empty;
            NouveauMotDePasse = string.Empty;
            ConfirmationMotDePasse = string.Empty;
            RefreshNewPasswordRulesUI();
        }

        private async Task MasquerMessageSuccesApresDelaiAsync()
        {
            await Task.Delay(2200);
            if (string.Equals(MessageCompte, "Modifications sauvegardées avec succès.", StringComparison.Ordinal))
            {
                MessageCompte = string.Empty;
                OnPropertyChanged(nameof(HasMessageCompte));
            }
        }

        private async Task MasquerMessageSecuriteSuccesApresDelaiAsync()
        {
            await Task.Delay(2200);
            if (string.Equals(MessageSecurite, "Mot de passe modifié avec succès.", StringComparison.Ordinal))
            {
                MessageSecurite = string.Empty;
                OnPropertyChanged(nameof(HasMessageSecurite));
            }
        }

        private void RefreshNewPasswordRulesUI()
        {
            var p = NouveauMotDePasse ?? string.Empty;
            var c = ConfirmationMotDePasse ?? string.Empty;
            _pwdLenOk = p.Length >= 8;
            _pwdUpperOk = Regex.IsMatch(p, "[A-Z]");
            _pwdDigitOk = Regex.IsMatch(p, "[0-9]");
            _pwdSpecialOk = Regex.IsMatch(p, "[!@#$%^&*()_+\\-=]");
            _pwdConfirmMatchOk = !string.IsNullOrEmpty(c) && string.Equals(p, c, StringComparison.Ordinal);

            OnPropertyChanged(nameof(PasswordRuleLengthOk));
            OnPropertyChanged(nameof(PasswordRuleUpperOk));
            OnPropertyChanged(nameof(PasswordRuleDigitOk));
            OnPropertyChanged(nameof(PasswordRuleSpecialOk));
            OnPropertyChanged(nameof(PasswordRuleLengthGlyph));
            OnPropertyChanged(nameof(PasswordRuleUpperGlyph));
            OnPropertyChanged(nameof(PasswordRuleDigitGlyph));
            OnPropertyChanged(nameof(PasswordRuleSpecialGlyph));
            OnPropertyChanged(nameof(PasswordRuleLengthBrush));
            OnPropertyChanged(nameof(PasswordRuleUpperBrush));
            OnPropertyChanged(nameof(PasswordRuleDigitBrush));
            OnPropertyChanged(nameof(PasswordRuleSpecialBrush));
            OnPropertyChanged(nameof(ShowPasswordConfirmRule));
            OnPropertyChanged(nameof(PasswordConfirmMatchesRule));
            OnPropertyChanged(nameof(PasswordConfirmRuleGlyph));
            OnPropertyChanged(nameof(PasswordConfirmRuleBrush));
        }

        private void ChargerCleGroq()
        {
            try
            {
                var cle = Services.GroqKeyService.LireCle(App.LocalDb);
                _cleGroq = cle ?? string.Empty;
                _cleGroqSnapshot = _cleGroq;
                IsEditingGroqKey = false;
                OnPropertyChanged(nameof(CleGroq));
                _testerCleGroqCommand?.RaiseCanExecuteChanged();
                _enregistrerCleGroqCommand?.RaiseCanExecuteChanged();
                _modifierCleGroqCommand?.RaiseCanExecuteChanged();
                _annulerCleGroqCommand?.RaiseCanExecuteChanged();
            }
            catch { }
        }

        private void ModifierCleGroq()
        {
            _cleGroqSnapshot = CleGroq;
            StatutGroq = string.Empty;
            OnPropertyChanged(nameof(HasStatutGroq));
            IsEditingGroqKey = true;
        }

        private void AnnulerModificationCleGroq()
        {
            CleGroq = _cleGroqSnapshot;
            StatutGroq = string.Empty;
            OnPropertyChanged(nameof(HasStatutGroq));
            IsEditingGroqKey = false;
        }

        private async Task TesterCleGroqAsync()
        {
            IsTestingGroq = true;
            SetStatutGroq("Test de la connexion en cours...", null);
            try
            {
                var (valide, message) = await Services.GroqKeyService.TesterCleAsync(CleGroq.Trim());
                SetStatutGroq(valide ? $"\u2705 Clé valide — {message}" : $"\u274C {message}", valide);
            }
            catch (Exception ex)
            {
                SetStatutGroq($"\u274C Erreur : {ex.Message}", false);
            }
            finally
            {
                IsTestingGroq = false;
            }
        }

        private void EnregistrerCleGroq()
        {
            var cle = CleGroq?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(cle))
            {
                SetStatutGroq("\u274C La clé ne peut pas être vide.", false);
                return;
            }

            if (!Services.GroqKeyService.CleEstValide(cle))
            {
                SetStatutGroq("\u274C Format invalide (doit commencer par gsk_ et faire au moins 20 caractères).", false);
                return;
            }

            IsSavingGroq = true;
            try
            {
                Services.GroqKeyService.SauvegarderCle(App.LocalDb, cle);
                _cleGroqSnapshot = cle;
                IsEditingGroqKey = false;
                SetStatutGroq("\u2705 Clé enregistrée avec succès.", true);
                _ = MasquerStatutGroqApresDelaiAsync();
            }
            catch (Exception ex)
            {
                SetStatutGroq($"\u274C Erreur : {ex.Message}", false);
            }
            finally
            {
                IsSavingGroq = false;
            }
        }

        private void SetStatutGroq(string message, bool? success)
        {
            StatutGroq = message;
            if (success == null)
                StatutGroqBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6b7a99"));
            else if (success.Value)
                StatutGroqBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#16a34a"));
            else
                StatutGroqBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#dc2626"));
            OnPropertyChanged(nameof(HasStatutGroq));
        }

        private async Task MasquerStatutGroqApresDelaiAsync()
        {
            await Task.Delay(3000);
            if (StatutGroq.StartsWith("\u2705 Clé enregistrée", StringComparison.Ordinal))
            {
                StatutGroq = string.Empty;
                OnPropertyChanged(nameof(HasStatutGroq));
            }
        }

        private async Task SupprimerCompteAsync()
        {
            if (!Views.SuppressionCompteDialog.DemanderConfirmation(
                    WpfApp.Current.MainWindow))
                return;

            var token = WpfApp.Current.Properties["Token"]?.ToString();
            if (string.IsNullOrWhiteSpace(token))
            {
                MessageBox.Show("Session invalide. Reconnectez-vous puis réessayez.",
                    "Suppression impossible", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsDeletingAccount = true;
            try
            {
                var emailCompte = WpfApp.Current.Properties["Email"]?.ToString();

                var erreurApi = await _authService.DeleteProfesseurCompteAsync(token);
                if (!string.IsNullOrWhiteSpace(erreurApi))
                {
                    MessageBox.Show(erreurApi, "Suppression impossible",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                if (!string.IsNullOrWhiteSpace(emailCompte))
                    smartest_desktop.App.SupprimerDonneesLocalesPourEmail(emailCompte);

                smartest_desktop.App.Deconnecter();
            }
            finally
            {
                IsDeletingAccount = false;
            }
        }
    }
}
