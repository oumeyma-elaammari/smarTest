using System.Collections.Generic;

namespace smartest_desktop.ViewModels
{
    /// <summary>
    /// Modèle UI temporaire d'une question d'examen générée par Ollama.
    /// Utilisé entre la génération et la sauvegarde en base locale.
    /// Types : QCM | CHECKBOX | REDACTION | IMAGE
    /// </summary>
    public class QuestionExamen : BaseViewModel
    {
        // ── Identité ─────────────────────────────────────────────────────────

        private int _numero;
        public int Numero
        {
            get => _numero;
            set => SetProperty(ref _numero, value);
        }

        private string _type = "QCM";
        public string Type
        {
            get => _type;
            set
            {
                SetProperty(ref _type, value);
                OnPropertyChanged(nameof(IsQCM));
                OnPropertyChanged(nameof(IsCheckbox));
                OnPropertyChanged(nameof(IsRedaction));
                OnPropertyChanged(nameof(IsImage));
                OnPropertyChanged(nameof(HasOptions));
                OnPropertyChanged(nameof(TypeLabel));
                OnPropertyChanged(nameof(TypeColor));
                OnPropertyChanged(nameof(TypeBackground));
            }
        }

        private string _difficulte = "Moyen";
        public string Difficulte
        {
            get => _difficulte;
            set => SetProperty(ref _difficulte, value);
        }

        // ── Énoncé & explication ─────────────────────────────────────────────

        private string _enonce = string.Empty;
        public string Enonce
        {
            get => _enonce;
            set => SetProperty(ref _enonce, value);
        }

        private string _explication = string.Empty;
        public string Explication
        {
            get => _explication;
            set => SetProperty(ref _explication, value);
        }

        // ── Options (QCM / CHECKBOX / IMAGE) ─────────────────────────────────

        private string _optionA = string.Empty;
        public string OptionA { get => _optionA; set => SetProperty(ref _optionA, value); }

        private string _optionB = string.Empty;
        public string OptionB { get => _optionB; set => SetProperty(ref _optionB, value); }

        private string _optionC = string.Empty;
        public string OptionC { get => _optionC; set => SetProperty(ref _optionC, value); }

        private string _optionD = string.Empty;
        public string OptionD { get => _optionD; set => SetProperty(ref _optionD, value); }

        // ── QCM / IMAGE — une seule bonne réponse ────────────────────────────

        private string _reponseCorrecte = string.Empty;
        public string ReponseCorrecte
        {
            get => _reponseCorrecte;
            set => SetProperty(ref _reponseCorrecte, value);
        }

        // ── CHECKBOX — plusieurs bonnes réponses ─────────────────────────────

        private bool _optionACorrecte;
        public bool OptionACorrecte { get => _optionACorrecte; set => SetProperty(ref _optionACorrecte, value); }

        private bool _optionBCorrecte;
        public bool OptionBCorrecte { get => _optionBCorrecte; set => SetProperty(ref _optionBCorrecte, value); }

        private bool _optionCCorrecte;
        public bool OptionCCorrecte { get => _optionCCorrecte; set => SetProperty(ref _optionCCorrecte, value); }

        private bool _optionDCorrecte;
        public bool OptionDCorrecte { get => _optionDCorrecte; set => SetProperty(ref _optionDCorrecte, value); }

        public List<string> ReponsesCorrectes
        {
            get
            {
                var list = new List<string>();
                if (OptionACorrecte) list.Add("A");
                if (OptionBCorrecte) list.Add("B");
                if (OptionCCorrecte) list.Add("C");
                if (OptionDCorrecte) list.Add("D");
                return list;
            }
        }

        // ── REDACTION — réponse libre ─────────────────────────────────────────

        private string _reponseModele = string.Empty;
        public string ReponseModele
        {
            get => _reponseModele;
            set => SetProperty(ref _reponseModele, value);
        }

        // ── IMAGE ─────────────────────────────────────────────────────────────

        private string _imageBase64 = string.Empty;
        public string ImageBase64
        {
            get => _imageBase64;
            set { SetProperty(ref _imageBase64, value); OnPropertyChanged(nameof(HasImage)); }
        }

        private string _imageType = string.Empty;
        public string ImageType { get => _imageType; set => SetProperty(ref _imageType, value); }

        private string _imageNom = string.Empty;
        public string ImageNom { get => _imageNom; set => SetProperty(ref _imageNom, value); }

        public bool HasImage => !string.IsNullOrEmpty(ImageBase64);

        // ── État UI ──────────────────────────────────────────────────────────

        private bool _isEditing;
        public bool IsEditing
        {
            get => _isEditing;
            set { SetProperty(ref _isEditing, value); OnPropertyChanged(nameof(IsNotEditing)); }
        }
        public bool IsNotEditing => !_isEditing;

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        // ── Computed booleans ────────────────────────────────────────────────

        public bool IsQCM       => Type == "QCM";
        public bool IsCheckbox  => Type == "CHECKBOX";
        public bool IsRedaction => Type == "REDACTION";
        public bool IsImage     => Type == "IMAGE";
        public bool HasOptions  => Type is "QCM" or "CHECKBOX" or "IMAGE";

        public string TypeLabel => Type switch
        {
            "QCM"       => "Choix multiple",
            "CHECKBOX"  => "Cases à cocher",
            "REDACTION" => "Rédaction",
            "IMAGE"     => "Avec image",
            _           => Type
        };

        public string TypeColor => Type switch
        {
            "QCM"       => "#0369A1",
            "CHECKBOX"  => "#7C3AED",
            "REDACTION" => "#B45309",
            "IMAGE"     => "#0F766E",
            _           => "#6B6B6B"
        };

        public string TypeBackground => Type switch
        {
            "QCM"       => "#F0F9FF",
            "CHECKBOX"  => "#F5F3FF",
            "REDACTION" => "#FFFBEB",
            "IMAGE"     => "#F0FDFA",
            _           => "#F5F4F0"
        };
    }
}
