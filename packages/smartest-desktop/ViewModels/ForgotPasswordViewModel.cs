using smartest_desktop.Helpers;
using smartest_desktop.Services;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfApp = System.Windows.Application;

namespace smartest_desktop.ViewModels
{
    public class ForgotPasswordViewModel : BaseViewModel
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

        public ICommand SendCommand { get; }
        public ICommand ResetPasswordCommand { get; }
        public ICommand BackToLoginCommand { get; }

        public ForgotPasswordViewModel()
        {
            SendCommand = new RelayCommand(
                async _ => await ExecuteSend(),
                _ => IsNotLoading);

            // ✅ Bouton pour ouvrir la fenêtre ResetPassword
            ResetPasswordCommand = new RelayCommand(
                _ => OpenResetPassword());

            BackToLoginCommand = new RelayCommand(
                _ => CloseWindow());
        }

        private async Task ExecuteSend()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                ErrorMessage = "Veuillez saisir votre email";
                SuccessMessage = null;
                return;
            }

            if (!IsValidEmail(Email.Trim()))
            {
                ErrorMessage = "Format d'email invalide";
                SuccessMessage = null;
                return;
            }
            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;

            var error = await _authService.ForgotPasswordAsync(Email.Trim().ToLower());

            IsLoading = false;

            if (error != null)
            {
                ErrorMessage = error;
            }
            else
            {
                SuccessMessage = "Si cet email existe, un lien de réinitialisation a été envoyé.";
            }
        }


        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void OpenResetPassword()
        {
            var resetWindow = new Views.ResetPasswordWindow();
            resetWindow.Show();

            foreach (System.Windows.Window w in WpfApp.Current.Windows)
            {
                if (w is Views.ForgotPasswordWindow)
                {
                    w.Close();
                    break;
                }
            }
        }

        private void CloseWindow()
        {
            foreach (System.Windows.Window w in WpfApp.Current.Windows)
            {
                if (w is Views.ForgotPasswordWindow)
                {
                    w.Close();
                    break;
                }
            }
        }
    }
}