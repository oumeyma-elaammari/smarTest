using smartest_desktop.Helpers;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows.Input;

namespace smartest_desktop.ViewModels
{
    public class QuizViewModel : BaseViewModel
    {
        private string _cours;
        public string Cours
        {
            get => _cours;
            set => SetProperty(ref _cours, value);
        }

        private int _nbQuestions = 5;
        public int NbQuestions
        {
            get => _nbQuestions;
            set => SetProperty(ref _nbQuestions, value);
        }

        private string _difficulte = "Moyen";
        public string Difficulte
        {
            get => _difficulte;
            set => SetProperty(ref _difficulte, value);
        }

        public ObservableCollection<Question> Questions { get; set; } = new();

        public ICommand GenererCommand { get; }

        public QuizViewModel(string contenuCours)
        {
            Cours = contenuCours;
            GenererCommand = new RelayCommand(_ => GenererQuiz());
        }
        public QuizViewModel() { }
        private void GenererQuiz()
        {
            Questions.Clear();

            for (int i = 1; i <= NbQuestions; i++)
            {
                Questions.Add(new Question
                {
                    Texte = $"Question {i}",
                    Choix = new List<string> { "A", "B", "C" },
                    Reponse = "A"
                });
            }
        }
    }

    public class Question
    {
        public string Texte { get; set; }
        public List<string> Choix { get; set; }
        public string Reponse { get; set; }
    }
}