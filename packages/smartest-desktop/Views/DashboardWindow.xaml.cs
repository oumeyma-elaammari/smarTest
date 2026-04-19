using System.Windows;
using smartest_desktop.ViewModels;
namespace smartest_desktop.Views
{
    public partial class DashboardWindow : Window
    {
        public DashboardWindow()
        {
            InitializeComponent();
            DataContext = new DashboardViewModel(); 

        }
    }
}