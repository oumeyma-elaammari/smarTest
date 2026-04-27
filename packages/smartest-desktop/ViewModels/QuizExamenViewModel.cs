using smartest_desktop.Helpers;
using System.Windows.Input;

namespace smartest_desktop.ViewModels
{
    public class QuizExamenViewModel : BaseViewModel
    {
        public ICommand GenererQuizCommand { get; }
        public ICommand GenererExamenCommand { get; }
        public ICommand RetourDashboardCommand { get; }

        public event Action? NavigateToQuizGeneration;
        public event Action? NavigateToExamenGeneration;
        public event Action? NavigateToDashboard;

        public QuizExamenViewModel()
        {
            GenererQuizCommand    = new RelayCommand(_ => NavigateToQuizGeneration?.Invoke());
            GenererExamenCommand  = new RelayCommand(_ => NavigateToExamenGeneration?.Invoke());
            RetourDashboardCommand = new RelayCommand(_ => NavigateToDashboard?.Invoke());
        }
    }
}
