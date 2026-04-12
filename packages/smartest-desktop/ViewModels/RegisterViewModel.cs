using System.Threading.Tasks;
using System.Windows.Input;
using smartest_desktop.Helpers;
using smartest_desktop.Services;

namespace smartest_desktop.ViewModels
{
    public class RegisterViewModel : BaseViewModel
    {
        private readonly AuthService _authService = new AuthService();

        private string _nom;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private string _errorMessage;
        private string _successMessage;
        private bool _isLoading;

        public string Nom
        {
            get => _nom;
            set => SetProperty(ref _nom, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                OnPropertyChanged(nameof(HasError));
            }
        }

        public string SuccessMessage
        {
            get => _successMessage;
            set
            {
                SetProperty(ref _successMessage, value);
                OnPropertyChanged(nameof(HasSuccess));
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                SetProperty(ref _isLoading, value);
                OnPropertyChanged(nameof(IsNotLoading));
            }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);
        public bool IsNotLoading => !IsLoading;

        public ICommand RegisterCommand { get; }

        public RegisterViewModel()
        {
            RegisterCommand = new RelayCommand(
                async _ => await ExecuteRegister(),
                _ => IsNotLoading);
        }

        private async Task ExecuteRegister()
        {
            // ── Validation ─────────────────────────────
            if (string.IsNullOrWhiteSpace(Nom) ||
                string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password) ||
                string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ErrorMessage = "Veuillez remplir tous les champs";
                return;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Les mots de passe ne correspondent pas";
                return;
            }

            if (Password.Length < 8)
            {
                ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères";
                return;
            }

            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;

            var error = await _authService.RegisterAsync(
                Nom.Trim(),
                Email.Trim().ToLower(),
                Password,
                ConfirmPassword);

            IsLoading = false;

            if (error != null)
            {
                ErrorMessage = error;
                return;
            }

            // ✅ Succès
            SuccessMessage =
                "Inscription réussie !\n" +
                "Vérifiez votre email pour confirmer votre compte.";
        }
    }
}