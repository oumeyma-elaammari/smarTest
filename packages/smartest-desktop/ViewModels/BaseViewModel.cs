using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace smartest_desktop.ViewModels
{
    public class BaseViewModel : INotifyPropertyChanged
    {
        // L'interface INotifyPropertyChanged attend un event nullable
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
