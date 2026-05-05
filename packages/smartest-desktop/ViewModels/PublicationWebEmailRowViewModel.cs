using System;

namespace smartest_desktop.ViewModels
{
    /// <summary>Ligne email dans la liste publication web (sélection pour suppression).</summary>
    public sealed class PublicationWebEmailRowViewModel : BaseViewModel
    {
        private readonly Action? _onSelectionChanged;

        public string Email { get; }

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

        public PublicationWebEmailRowViewModel(string email, Action? onSelectionChanged = null)
        {
            Email = email;
            _onSelectionChanged = onSelectionChanged;
        }
    }
}
