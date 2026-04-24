using smartest_desktop.Helpers;
using System.Windows.Input;

namespace smartest_desktop.ViewModels
{
    /// <summary>
    /// ViewModel de la page hub "Quiz et Examens"
    /// Affiche deux cartes : Générer un Quiz / Générer un Examen
    /// </summary>
    public class QuizExamenViewModel : BaseViewModel
    {
        public ICommand GenererQuizCommand { get; }
        public ICommand GenererExamenCommand { get; }
        public ICommand RetourDashboardCommand { get; }

        public event Action? NavigateToQuizGeneration;
        public event Action? NavigateToDashboard;

        public QuizExamenViewModel()
        {
            GenererQuizCommand = new RelayCommand(_ => NavigateToQuizGeneration?.Invoke());
            GenererExamenCommand = new RelayCommand(_ => { /* à implémenter plus tard */ });
            RetourDashboardCommand = new RelayCommand(_ => NavigateToDashboard?.Invoke());
        }
    }
}
