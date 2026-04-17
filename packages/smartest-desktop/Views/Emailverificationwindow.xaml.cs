using System.Windows;
using smartest_desktop.ViewModels;

namespace smartest_desktop.Views
{
    public partial class EmailVerificationWindow : Window
    {
        public EmailVerificationWindow(string email)
        {
            InitializeComponent();
            DataContext = new EmailVerificationViewModel(email);
        }
    }
}