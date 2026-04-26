using System.Windows;
using System.Windows.Controls;
using smartest_desktop.Services;
using smartest_desktop.ViewModels;

namespace smartest_desktop.Views
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
            DataContext = new RegisterViewModel();
        }

        private void PasswordBox_Changed(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegisterViewModel vm)
                vm.Password = ((PasswordBox)sender).Password;
        }

        private void ConfirmPasswordBox_Changed(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegisterViewModel vm)
                vm.ConfirmPassword = ((PasswordBox)sender).Password;
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

        private void BackToWelcome_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.NavigateTo<MainWindow, RegisterWindow>();
        }
    }
}