using System.Windows;
using smartest_desktop.ViewModels;

namespace smartest_desktop.Views
{
    public partial class QuizWindow : Window
    {
        public QuizWindow()
        {
            InitializeComponent();
            DataContext = new QuizViewModel();
        }
    }
}