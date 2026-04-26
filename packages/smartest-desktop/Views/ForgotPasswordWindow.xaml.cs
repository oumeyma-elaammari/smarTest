using System.Windows;
using smartest_desktop.ViewModels;

namespace smartest_desktop.Views
{
    public partial class ForgotPasswordWindow : Window
    {
        public ForgotPasswordWindow()
        {
            InitializeComponent();
            DataContext = new ForgotPasswordViewModel();
        }
    }
}