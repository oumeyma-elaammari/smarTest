using System;

namespace smartest_desktop.ViewModels
{
    /// <summary>Ligne email dans la liste publication web (sélection pour suppression).</summary>
    public sealed class PublicationWebEmailRowViewModel : BaseViewModel
    {
        private readonly Action? _onSelectionChanged;
    private readonly Action? _onEmailChanged;

    private string _email;
    public string Email
    {
        get => _email;
        set
        {
            if (!SetProperty(ref _email, value ?? string.Empty))
                return;
            _onEmailChanged?.Invoke();
        }
    }

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (!SetProperty(ref _isSelected, value))
                    return;
                _onSelectionChanged?.Invoke();
            }
        }

    public PublicationWebEmailRowViewModel(
        string email,
        Action? onSelectionChanged = null,
        Action? onEmailChanged = null)
        {
        _email = email;
            _onSelectionChanged = onSelectionChanged;
        _onEmailChanged = onEmailChanged;
        }
    }
}
