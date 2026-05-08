using Microsoft.Win32;
using smartest_desktop.Helpers;
using System.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace smartest_desktop.ViewModels
{
    public class ExamenResultViewModel : BaseViewModel
    {
        private readonly int _dureeMinutes;

        private readonly string _empreinteInitiale;

        private readonly Func<Task>? _supprimerExamenPersisteAsync;

        public string TitreExamen
        {
            get => _titreExamen;
            set => SetProperty(ref _titreExamen, value);
        }

        private string _titreExamen = string.Empty;

        public int Duree => _dureeMinutes;

        public string Difficulte { get; }

        public string CoursSourceLabel { get; }

        public ObservableCollection<QuestionExamen> Questions { get; } = new();

        public int NombreQuestions => Questions.Count;

        private QuestionExamen? _questionSelectionnee;

        public QuestionExamen? QuestionSelectionnee
        {
            get => _questionSelectionnee;
            set
            {
                SetProperty(ref _questionSelectionnee, value);
                OnPropertyChanged(nameof(HasQuestion));
                OnPropertyChanged(nameof(NoQuestion));
            }
        }

        public bool HasQuestion => QuestionSelectionnee != null;
        public bool NoQuestion => QuestionSelectionnee == null;

        public int? ExamenIdExistant { get; }

        public bool IsEditionExistant => ExamenIdExistant.HasValue;

        public string TitreFenetre =>
            IsEditionExistant ? "SmarTest — Modifier l'examen" : "SmarTest — Révision de l'examen";

        public string LibelleBoutonValider =>
            IsEditionExistant ? "Enregistrer les modifications" : "Valider et sauvegarder";

        public string SousTitreEtape =>
            IsEditionExistant
                ? "Modifiez puis enregistrez dans la base locale"
                : "Vérifiez et ajustez avant validation";

        public string SousTitreCompteur =>
            IsEditionExistant
                ? $"{NombreQuestions} questions · {Duree} min · {Difficulte} · {CoursSourceLabel}"
                : $"{NombreQuestions} questions générées · {Duree} min · {Difficulte} · {CoursSourceLabel}";

        public IReadOnlyList<double> ValeursBareme { get; } =
            Enumerable.Range(0, 81).Select(i => i * ExamenBaremeHelper.Pas).ToList();

        public double TotalBareme => Math.Round(Questions.Sum(q => q.BaremePoints), 2);
        public double EcartBareme => Math.Round(ExamenBaremeHelper.TotalCible - TotalBareme, 2);
        public bool BaremeValide => Math.Abs(EcartBareme) < 1e-9 && Questions.All(q => ExamenBaremeHelper.EstAuPas(q.BaremePoints));
        public string ResumeBareme => $"Barème : {ExamenBaremeHelper.FormatPoints(TotalBareme)} / {ExamenBaremeHelper.FormatPoints(ExamenBaremeHelper.TotalCible)}";

        public ICommand SelectionnerCommand { get; }
        public ICommand SupprimerCommand { get; }
        /// <summary>Paramètre : type de question — QCM, VF, CHECKBOX ou REDACTION.</summary>
        public ICommand AjouterQuestionCommand { get; }
        public ICommand AttacherImageCommand { get; }
        public ICommand SupprimerImageCommand { get; }
        public ICommand ValiderExamenCommand { get; }
        public ICommand RegenerarCommand { get; }
        public ICommand RetourCommand { get; }

        public ICommand SetReponseCorrecteCommand { get; }
        private readonly RelayCommand _validerExamenCommand;

        public event Action<List<QuestionExamen>, string, int, string, string>? ExamenValide;

        public event Action? NavigationRegenerarRequested;
        public event Action? NavigationRetourRequested;

        public ExamenResultViewModel(
            List<QuestionExamen> questions,
            string titre,
            int duree,
            string difficulte,
            string coursTitre,
            int? examenIdExistant = null,
            Func<Task>? supprimerExamenPersisteAsync = null)
        {
            ExamenIdExistant = examenIdExistant;
            _supprimerExamenPersisteAsync = supprimerExamenPersisteAsync;

            _dureeMinutes = duree;
            TitreExamen = titre;
            Difficulte = difficulte;
            CoursSourceLabel = coursTitre;

            int n = 1;
            foreach (var q in questions)
            {
                q.Numero = n++;
                Questions.Add(q);
            }

            if (Questions.Count > 0)
            {
                Questions[0].IsSelected = true;
                QuestionSelectionnee = Questions[0];
            }

            if (Questions.Sum(q => q.BaremePoints) <= 0.001)
                ExamenBaremeHelper.AppliquerBaremeParDefaut(Questions.ToList());
            RafraichirQualitePedagogique();
            _empreinteInitiale = CalculerEmpreinte();

            SelectionnerCommand = new RelayCommand(p =>
            {
                if (p is not QuestionExamen q) return;

                foreach (var item in Questions)
                    item.IsSelected = false;

                q.IsSelected = true;
                QuestionSelectionnee = q;
            });

            SupprimerCommand = new RelayCommand(p =>
            {
                if (p is not QuestionExamen q) return;

                int idx = Questions.IndexOf(q);
                if (idx < 0) return;

                Questions.Remove(q);
                Renuméroter();
                OnPropertyChanged(nameof(NombreQuestions));
                OnPropertyChanged(nameof(SousTitreCompteur));
                ActualiserEtatBareme();
                _validerExamenCommand.RaiseCanExecuteChanged();

                if (Questions.Count > 0)
                    QuestionSelectionnee = Questions[Math.Min(idx, Questions.Count - 1)];
                else
                    QuestionSelectionnee = null;

                foreach (var item in Questions)
                    item.IsSelected = item == QuestionSelectionnee;
                if (QuestionSelectionnee != null)
                    QuestionSelectionnee.IsSelected = true;

                if (Questions.Count == 0 && _supprimerExamenPersisteAsync != null)
                    _ = SupprimerExamenVideEtFermerAsync();
            });

            AjouterQuestionCommand = new RelayCommand(p =>
            {
                string type = p as string ?? "QCM";
                if (type != "QCM" && type != "VF" && type != "CHECKBOX" && type != "REDACTION")
                    type = "QCM";

                var q = new QuestionExamen
                {
                    Type = type,
                    Difficulte = Difficulte,
                    Enonce = "Nouvelle question",
                    BaremePoints = ExamenBaremeHelper.Pas
                };

                if (type == "QCM")
                {
                    q.ReponseCorrecte = string.Empty;
                }
                else if (type == "VF")
                {
                    q.OptionA = "Vrai";
                    q.OptionB = "Faux";
                    q.OptionC = string.Empty;
                    q.OptionD = string.Empty;
                    q.ReponseCorrecte = "A";
                }
                else if (type == "CHECKBOX")
                {
                    q.OptionACorrecte = false;
                    q.OptionBCorrecte = false;
                    q.OptionCCorrecte = false;
                    q.OptionDCorrecte = false;
                }
                else
                {
                    q.ReponseModele = string.Empty;
                }

                int idx = CalculerIndexInsertionParType(Questions, type);
                Questions.Insert(idx, q);
                Renuméroter();
                OnPropertyChanged(nameof(NombreQuestions));
                OnPropertyChanged(nameof(SousTitreCompteur));
                ActualiserEtatBareme();
                _validerExamenCommand.RaiseCanExecuteChanged();

                foreach (var item in Questions)
                    item.IsSelected = false;
                q.IsSelected = true;
                QuestionSelectionnee = q;
            });

            SetReponseCorrecteCommand = new RelayCommand(p =>
            {
                if (p is not string lettre) return;
                if (QuestionSelectionnee == null) return;
                if (!QuestionSelectionnee.IsQCM && !QuestionSelectionnee.IsVF) return;

                QuestionSelectionnee.ReponseCorrecte = lettre.ToUpperInvariant();
                OnPropertyChanged(nameof(QuestionSelectionnee));
            });

            AttacherImageCommand = new RelayCommand(_ =>
            {
                if (QuestionSelectionnee == null) return;

                var dlg = new OpenFileDialog
                {
                    Title = "Sélectionner une image",
                    Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp"
                };

                if (dlg.ShowDialog() != true) return;

                try
                {
                    byte[] bytes = File.ReadAllBytes(dlg.FileName);
                    string base64 = Convert.ToBase64String(bytes);
                    string ext = Path.GetExtension(dlg.FileName).TrimStart('.').ToLowerInvariant();

                    QuestionSelectionnee.ImageBase64 = base64;
                    QuestionSelectionnee.ImageType = ext;
                    QuestionSelectionnee.ImageNom = Path.GetFileName(dlg.FileName);

                    OnPropertyChanged(nameof(QuestionSelectionnee));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(UserErrorMessage.FromException(ex, "Impossible de lire cette image."),
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            SupprimerImageCommand = new RelayCommand(_ =>
            {
                if (QuestionSelectionnee == null) return;
                QuestionSelectionnee.ImageBase64 = string.Empty;
                QuestionSelectionnee.ImageType = string.Empty;
                QuestionSelectionnee.ImageNom = string.Empty;
                OnPropertyChanged(nameof(QuestionSelectionnee));
            });

            _validerExamenCommand = new RelayCommand(
                _ =>
                {
                    if (Questions.Count == 0)
                    {
                        MessageBox.Show(
                            "L'examen ne contient aucune question.",
                            "Examen vide",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    if (!BaremeValide)
                    {
                        var msg = Questions.Any(q => !ExamenBaremeHelper.EstAuPas(q.BaremePoints))
                            ? "Les points doivent etre saisis par pas de 0,25."
                            : $"Le barème total doit être de 20 points (actuel: {ExamenBaremeHelper.FormatPoints(TotalBareme)}).";
                        MessageBox.Show(
                            msg,
                            "Barème  invalide",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }

                    MessageBoxResult res;
                    if (IsEditionExistant)
                    {
                        res = MessageBox.Show(
                            $"Enregistrer les modifications de l'examen « {TitreExamen} » ?\n\n" +
                            $"• {Questions.Count} questions\n" +
                            $"• Difficulté : {Difficulte}\n" +
                            $"• Durée : {Duree} min\n\n" +
                            "Les données en base seront mises à jour.",
                            "Enregistrer les modifications",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                    }
                    else
                    {
                        res = MessageBox.Show(
                            $"Valider et sauvegarder l'examen « {TitreExamen} » ?\n\n" +
                            $"• {Questions.Count} questions\n" +
                            $"• Difficulté : {Difficulte}\n" +
                            $"• Durée : {Duree} min\n\n" +
                            "L'examen sera enregistré localement.",
                            "Valider et sauvegarder",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);
                    }

                    if (res != MessageBoxResult.Yes) return;

                    ExamenValide?.Invoke(
                        Questions.ToList(),
                        TitreExamen,
                        Duree,
                        Difficulte,
                        CoursSourceLabel);
                },
                _ => Questions.Count > 0);
            ValiderExamenCommand = _validerExamenCommand;

            Questions.CollectionChanged += (_, __) =>
            {
                RebrancherEvenementsQuestions();
                ActualiserEtatBareme();
                RafraichirQualitePedagogique();
                _validerExamenCommand.RaiseCanExecuteChanged();
            };
            RebrancherEvenementsQuestions();
            ActualiserEtatBareme();

            RegenerarCommand = new RelayCommand(_ =>
            {
                var res = MessageBox.Show(
                    "Regénérer l'examen ?\nLes questions actuelles seront perdues.",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                    NavigationRegenerarRequested?.Invoke();
            });

            RetourCommand = new RelayCommand(_ =>
            {
                // Pas encore enregistré en base : toujours confirmer (même sans modification d'empreinte)
                if (!IsEditionExistant)
                {
                    var resNouveau = MessageBox.Show(
                        "Quitter sans sauvegarder ?\n" +
                        "L'examen n'est pas enregistré dans la base locale. Vous perdrez le contenu généré.",
                        "Quitter sans sauvegarder",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if (resNouveau == MessageBoxResult.Yes)
                        NavigationRetourRequested?.Invoke();
                    return;
                }

                if (!ADesModificationsDepuisOuverture())
                {
                    NavigationRetourRequested?.Invoke();
                    return;
                }

                var res = MessageBox.Show(
                    "Quitter sans enregistrer les modifications ?\nLes changements seront perdus.",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (res == MessageBoxResult.Yes)
                    NavigationRetourRequested?.Invoke();
            });
        }

        private string CalculerEmpreinte()
        {
            var qs = Questions.Select(q => new
            {
                q.Numero,
                q.Type,
                q.Enonce,
                q.Difficulte,
                q.OptionA,
                q.OptionB,
                q.OptionC,
                q.OptionD,
                q.ReponseCorrecte,
                q.OptionACorrecte,
                q.OptionBCorrecte,
                q.OptionCCorrecte,
                q.OptionDCorrecte,
                q.ReponseModele,
                q.BaremePoints,
                q.Explication,
                ImgLen = q.ImageBase64?.Length ?? 0,
                q.ImageNom,
                q.ImageType
            }).ToList();

            return JsonSerializer.Serialize(new { TitreExamen, Duree = _dureeMinutes, questions = qs });
        }

        private bool ADesModificationsDepuisOuverture() =>
            CalculerEmpreinte() != _empreinteInitiale;

        private void Renuméroter()
        {
            int n = 1;
            foreach (var q in Questions)
                q.Numero = n++;
        }

        private void RebrancherEvenementsQuestions()
        {
            foreach (var q in Questions)
            {
                q.PropertyChanged -= Question_PropertyChanged;
                q.PropertyChanged += Question_PropertyChanged;
            }
        }

        private void Question_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(QuestionExamen.BaremePoints))
            {
                ActualiserEtatBareme();
                _validerExamenCommand.RaiseCanExecuteChanged();
            }
            if (e.PropertyName is nameof(QuestionExamen.Enonce)
                or nameof(QuestionExamen.OptionA)
                or nameof(QuestionExamen.OptionB)
                or nameof(QuestionExamen.OptionC)
                or nameof(QuestionExamen.OptionD)
                or nameof(QuestionExamen.ReponseCorrecte)
                or nameof(QuestionExamen.ReponseModele)
                or nameof(QuestionExamen.Explication))
            {
                RafraichirQualitePedagogique();
            }
        }

        private void ActualiserEtatBareme()
        {
            OnPropertyChanged(nameof(TotalBareme));
            OnPropertyChanged(nameof(EcartBareme));
            OnPropertyChanged(nameof(BaremeValide));
            OnPropertyChanged(nameof(ResumeBareme));
        }

        private void RafraichirQualitePedagogique()
        {
            foreach (var q in Questions)
            {
                _ = PedagogicalQualityEvaluator.EvaluateExamQuestion(q);
            }
        }

        /// <summary>
        /// Ordre d'affichage aligné sur la génération : QCM, puis CHECKBOX, puis RÉDACTION, puis IMAGE.
        /// La nouvelle question est insérée dans le bloc de son type (après les questions du même type déjà présentes).
        /// </summary>
        private static int TypeOrder(string t) => t switch
        {
            "QCM" => 0,
            "VF" => 1,
            "CHECKBOX" => 2,
            "REDACTION" => 3,
            "IMAGE" => 4,
            _ => 0
        };

        private static int CalculerIndexInsertionParType(ObservableCollection<QuestionExamen> questions, string type)
        {
            int want = TypeOrder(type);
            int insertAt = 0;
            for (int i = 0; i < questions.Count; i++)
            {
                int o = TypeOrder(questions[i].Type);
                if (o > want)
                    return i;
                insertAt = i + 1;
            }

            return insertAt;
        }

        private async Task SupprimerExamenVideEtFermerAsync()
        {
            try
            {
                if (_supprimerExamenPersisteAsync != null)
                    await _supprimerExamenPersisteAsync();
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    NavigationRetourRequested?.Invoke());
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                    MessageBox.Show(
                        UserErrorMessage.FromException(ex, "Impossible de supprimer cet examen pour le moment."),
                        "Erreur",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error));
            }
        }
    }
}
