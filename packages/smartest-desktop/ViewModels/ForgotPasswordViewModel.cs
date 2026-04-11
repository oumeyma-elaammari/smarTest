using System.Threading.Tasks;
using System.Windows.Input;
using smartest_desktop.Helpers;
using smartest_desktop.Services;

namespace smartest_desktop.ViewModels
{
    public class ForgotPasswordViewModel : BaseViewModel
    {
        private readonly AuthService _authService = new AuthService();

        private string? _email;
        private string? _errorMessage;
        private string? _successMessage;
        private bool _isLoading;

        public string? Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string? ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                OnPropertyChanged(nameof(HasError));
            }
        }

        public string? SuccessMessage
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
            set => SetProperty(ref _isLoading, value);
        }

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
        public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);

        public ICommand SendCommand { get; }

        public ForgotPasswordViewModel()
        {
            SendCommand = new RelayCommand(
                async _ => await ExecuteSend());
        }

        private async Task ExecuteSend()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Veuillez saisir votre email";
                return;
            }

            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;

            var error = await _authService
                .ForgotPasswordAsync(Email.Trim().ToLower());

            IsLoading = false;

            if (error != null)
            {
                ErrorMessage = error;
                return;
            }

            SuccessMessage =
                "Si cet email existe, un lien a été envoyé.\n" +
                "Vérifiez votre boîte mail.";
        }
    }
}