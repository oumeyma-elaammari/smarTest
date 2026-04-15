using smartest_desktop.Helpers;
using smartest_desktop.Services;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfApp = System.Windows.Application;

namespace smartest_desktop.ViewModels
{
    public class ResetPasswordViewModel : BaseViewModel
    {
        private readonly AuthService _authService = new AuthService();

        private string _token;
        private string _newPassword;
        private string _confirmPassword;
        private string _errorMessage;
        private string _successMessage;
        private bool _isLoading;

        public string Token
        {
            get => _token;
            set => SetProperty(ref _token, value);
        }

        public string NewPassword
        {
            get => _newPassword;
            set => SetProperty(ref _newPassword, value);
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

        public ICommand ResetCommand { get; }
        public ICommand BackToLoginCommand { get; }

        public ResetPasswordViewModel(string token = "")
        {
            Token = token;

            ResetCommand = new RelayCommand(
                async _ => await ExecuteReset(),
                _ => IsNotLoading);

            BackToLoginCommand = new RelayCommand(
                _ => OpenLogin());
        }

        private async Task ExecuteReset()
        {
            // Validation
            if (string.IsNullOrWhiteSpace(Token))
            {
                ErrorMessage = "Veuillez saisir le code de réinitialisation";
                SuccessMessage = null;
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPassword))
            {
                ErrorMessage = "Veuillez saisir un nouveau mot de passe";
                SuccessMessage = null;
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                ErrorMessage = "Les mots de passe ne correspondent pas";
                SuccessMessage = null;
                return;
            }

            if (NewPassword.Length < 8)
            {
                ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères";
                SuccessMessage = null;
                return;
            }

            IsLoading = true;
            ErrorMessage = null;
            SuccessMessage = null;

            var error = await _authService.ResetPasswordAsync(
                Token.Trim(),
                NewPassword,
                ConfirmPassword);

            IsLoading = false;

            if (error != null)
            {
                ErrorMessage = error;
                return;
            }

            // ✅ Succès — afficher message et rediriger vers login
            SuccessMessage = "Mot de passe réinitialisé avec succès ! Vous pouvez maintenant vous connecter.";

            // Attendre 2 secondes puis ouvrir login
            await Task.Delay(2000);
            OpenLogin();
        }

        private void OpenLogin()
        {
            var login = new Views.LoginWindow();
            login.Show();

            // Fermer la fenêtre actuelle
            foreach (System.Windows.Window w in WpfApp.Current.Windows)
            {
                if (w is Views.ResetPasswordWindow)
                {
                    w.Close();
                    break;
                }
            }
        }
    }
}