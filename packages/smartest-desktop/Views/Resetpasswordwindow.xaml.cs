using System.Windows;
using System.Windows.Controls;
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

        private void TogglePassword_Click(object sender, RoutedEventArgs e)
        {
            if (PasswordBox.Visibility == Visibility.Visible)
            {
                PasswordTextBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordTextBox.Visibility = Visibility.Visible;
                EyeIcon1.Text = "🔒";
            }
            else
            {
                PasswordBox.Password = PasswordTextBox.Text;
                PasswordTextBox.Visibility = Visibility.Collapsed;
                PasswordBox.Visibility = Visibility.Visible;
                EyeIcon1.Text = "🔓";
            }
        }

        private void ToggleConfirmPassword_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmPasswordBox.Visibility == Visibility.Visible)
            {
                ConfirmPasswordTextBox.Text = ConfirmPasswordBox.Password;
                ConfirmPasswordBox.Visibility = Visibility.Collapsed;
                ConfirmPasswordTextBox.Visibility = Visibility.Visible;
                EyeIcon2.Text = "🔒";
            }
            else
            {
                ConfirmPasswordBox.Password = ConfirmPasswordTextBox.Text;
                ConfirmPasswordTextBox.Visibility = Visibility.Collapsed;
                ConfirmPasswordBox.Visibility = Visibility.Visible;
                EyeIcon2.Text = "🔓";
            }
        }
    }
}