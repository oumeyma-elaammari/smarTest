using smartest_desktop.Helpers;
using smartest_desktop.Services;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfApp = System.Windows.Application;

namespace smartest_desktop.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly AuthService _authService = new AuthService();

        private string _email;
        private string _password;
        private string _errorMessage;
        private bool _isLoading;

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

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                SetProperty(ref _errorMessage, value);
                OnPropertyChanged(nameof(HasError));
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
        public bool IsNotLoading => !IsLoading;

        public ICommand LoginCommand { get; }
        public ICommand ForgotPasswordCommand { get; }
        public ICommand RegisterCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(
                async _ => await ExecuteLogin(),
                _ => IsNotLoading);

            ForgotPasswordCommand = new RelayCommand(_ =>
                NavigationService.NavigateTo<Views.ForgotPasswordWindow, Views.LoginWindow>());

            RegisterCommand = new RelayCommand(_ =>
                NavigationService.NavigateTo<Views.RegisterWindow, Views.LoginWindow>());
        }

        private async Task ExecuteLogin()
        {
            if (string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Veuillez remplir tous les champs";
                return;
            }

            IsLoading = true;
            ErrorMessage = null;

            var (auth, error) = await _authService
                .LoginAsync(Email.Trim().ToLower(), Password);

            IsLoading = false;

            if (error != null)
            {
                ErrorMessage = error;
                return;
            }

            WpfApp.Current.Properties["Token"] = auth.Token;
            WpfApp.Current.Properties["Nom"] = auth.Nom;
            WpfApp.Current.Properties["Email"] = auth.Email;

            NavigationService.NavigateTo<Views.DashboardWindow, Views.LoginWindow>();
        }
    }
}