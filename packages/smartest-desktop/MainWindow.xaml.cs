using smartest_desktop.ViewModels;
using System.Windows;

namespace smartest_desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}