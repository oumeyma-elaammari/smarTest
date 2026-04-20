using smartest_desktop.Helpers;
using smartest_desktop.Services;
using System.Windows.Input;

namespace smartest_desktop.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public ICommand GoToLoginCommand { get; }
        public ICommand GoToRegisterCommand { get; }

        public MainViewModel()
        {
            GoToLoginCommand = new RelayCommand(_ =>
                NavigationService.NavigateTo<Views.LoginWindow, MainWindow>());

            GoToRegisterCommand = new RelayCommand(_ =>
                NavigationService.NavigateTo<Views.RegisterWindow, MainWindow>());
        }
    }
}