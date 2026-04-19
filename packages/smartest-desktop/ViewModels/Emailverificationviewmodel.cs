using smartest_desktop.Helpers;
using smartest_desktop.Services;
using System.Threading.Tasks;
using System.Windows.Input;

namespace smartest_desktop.ViewModels
{
    public class EmailVerificationViewModel : BaseViewModel
    {
        private readonly AuthService _authService = new AuthService();

        private string _email;
        private string _code;
        private string _errorMessage;
        private string _successMessage;
        private bool _isLoading;

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Code
        {
            get => _code;
            set => SetProperty(ref _code, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set { SetProperty(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); }
        }

        public string SuccessMessage
        {
            get => _successMessage;
            set { SetProperty(ref _successMessage, value); OnPropertyChanged(nameof(HasSuccess)); }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set { SetProperty(ref _isLoading, value); OnPropertyChanged(nameof(IsNotLoading)); }
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);
        public bool IsNotLoading => !IsLoading;

        public ICommand VerifyCommand { get; }
        public ICommand ResendEmailCommand { get; }
        public ICommand GoToLoginCommand { get; }

        public EmailVerificationViewModel(string email)
        {
            Email = email;

            VerifyCommand = new RelayCommand(
                async _ => await VerifyCode(),
                _ => IsNotLoading);

            ResendEmailCommand = new RelayCommand(
                async _ => await ResendCode(),
                _ => IsNotLoading);

            GoToLoginCommand = new RelayCommand(_ =>
                NavigationService.NavigateTo<Views.LoginWindow, Views.EmailVerificationWindow>());
        }

        private async Task VerifyCode()
        {
            if (string.IsNullOrWhiteSpace(Code) || Code.Trim().Length != 6)
            {
                ErrorMessage = "Veuillez saisir le code à 6 chiffres reçu par email.";
                SuccessMessage = null;
                return;
            }

            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;

            var error = await _authService.VerifyEmailByCodeAsync(Email, Code.Trim());

            IsLoading = false;

            if (error != null)
            {
                ErrorMessage = error;
                return;
            }

            SuccessMessage = "Email vérifié ! Redirection vers la connexion...";
            await Task.Delay(1500);
            NavigationService.NavigateTo<Views.LoginWindow, Views.EmailVerificationWindow>();
        }

        private async Task ResendCode()
        {
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;

            var error = await _authService.ResendVerificationCodeAsync(Email);

            IsLoading = false;

            if (error != null)
                ErrorMessage = "Impossible de renvoyer le code. Réessayez.";
            else
                SuccessMessage = "Nouveau code envoyé ! Vérifiez votre boîte mail.";
        }
    }
}