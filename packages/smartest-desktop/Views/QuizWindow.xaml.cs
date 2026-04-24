using System.Windows;
using smartest_desktop.ViewModels;

namespace smartest_desktop.Views
{
    public partial class QuizWindow : Window
    {
        public QuizWindow(string contenuCours)
        {
            InitializeComponent();
            DataContext = new QuizViewModel(contenuCours);
        }

        public QuizWindow() : this(string.Empty) { }

    }
}