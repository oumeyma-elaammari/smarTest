using smartest_desktop.Helpers;
using smartest_desktop.Services;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfApp = System.Windows.Application;

namespace smartest_desktop.ViewModels
{
    public class EmailVerificationViewModel : BaseViewModel
    {
        private readonly AuthService _authService = new AuthService();

        private string _email;
        private string _errorMessage;
        private string _successMessage;
        private bool _isLoading;

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
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

        public ICommand GoToLoginCommand { get; }
        public ICommand ResendEmailCommand { get; }

        public EmailVerificationViewModel(string email)
        {
            Email = email;

            GoToLoginCommand = new RelayCommand(
                _ => OpenLogin());

            ResendEmailCommand = new RelayCommand(
                async _ => await ResendEmail(),
                _ => IsNotLoading);
        }

        private async Task ResendEmail()
        {
            if (string.IsNullOrWhiteSpace(Email)) return;

            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;

            // Ré-inscription pour renvoyer l'email — appel forget password
            // ou on peut appeler un endpoint dédié si disponible
            var error = await _authService.ForgotPasswordAsync(Email);

            IsLoading = false;

            if (error != null)
            {
                ErrorMessage = "Impossible de renvoyer l'email. Réessayez.";
            }
            else
            {
                SuccessMessage = "Email renvoyé ! Vérifiez votre boîte mail.";
            }
        }

        private void OpenLogin()
        {
            var login = new Views.LoginWindow();
            login.Show();

            foreach (System.Windows.Window w in WpfApp.Current.Windows)
            {
                if (w is Views.EmailVerificationWindow)
                {
                    w.Close();
                    break;
                }
            }
        }
    }
}