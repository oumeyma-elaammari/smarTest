using System.Windows;
using smartest_desktop.ViewModels;

namespace smartest_desktop.Views
{
    public partial class ResetPasswordWindow : Window
    {
        private ResetPasswordViewModel ViewModel =>
            (ResetPasswordViewModel)DataContext;

        public ResetPasswordWindow(string token = "")
        {
            InitializeComponent();
            DataContext = new ResetPasswordViewModel(token);

            // Pré-remplir le token si fourni
            if (!string.IsNullOrEmpty(token))
                TokenBox_Prefill(token);
        }

        private void TokenBox_Prefill(string token)
        {
            // Le token est déjà dans le ViewModel
        }

        private void PasswordBox_Changed(object sender, RoutedEventArgs e)
        {
            ViewModel.NewPassword = PasswordBox.Password;
        }

        private void ConfirmPasswordBox_Changed(object sender, RoutedEventArgs e)
        {
            ViewModel.ConfirmPassword = ConfirmPasswordBox.Password;
        }
    }
}