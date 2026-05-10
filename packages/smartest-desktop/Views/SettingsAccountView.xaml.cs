using System.Windows.Controls;
using System.ComponentModel;
using smartest_desktop.ViewModels;
using System.Windows;

namespace smartest_desktop.Views
{
    public partial class SettingsAccountView : UserControl
    {
        private SettingsAccountViewModel? _vm;
        private bool _isSyncingUi;

        public SettingsAccountView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += (_, __) => SyncPasswordBoxesFromViewModel();
        }

        private void OnDataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null)
                _vm.PropertyChanged -= OnViewModelPropertyChanged;

            _vm = DataContext as SettingsAccountViewModel;
            if (_vm != null)
                _vm.PropertyChanged += OnViewModelPropertyChanged;

            SyncPasswordBoxesFromViewModel();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsAccountViewModel.AncienMotDePasse) ||
                e.PropertyName == nameof(SettingsAccountViewModel.NouveauMotDePasse) ||
                e.PropertyName == nameof(SettingsAccountViewModel.ConfirmationMotDePasse))
            {
                SyncPasswordBoxesFromViewModel();
            }
        }

        private void SyncPasswordBoxesFromViewModel()
        {
            if (_vm == null)
                return;

            var ancien = _vm.AncienMotDePasse ?? string.Empty;
            var nouveau = _vm.NouveauMotDePasse ?? string.Empty;
            var conf = _vm.ConfirmationMotDePasse ?? string.Empty;

            _isSyncingUi = true;
            try
            {
                if (OldPasswordBox.Password != ancien)
                    OldPasswordBox.Password = ancien;
                if (OldPasswordTextBox.Text != ancien)
                    OldPasswordTextBox.Text = ancien;

                if (NewPasswordBox.Password != nouveau)
                    NewPasswordBox.Password = nouveau;
                if (NewPasswordTextBox.Text != nouveau)
                    NewPasswordTextBox.Text = nouveau;

                if (ConfirmPasswordBox.Password != conf)
                    ConfirmPasswordBox.Password = conf;
                if (ConfirmPasswordTextBox.Text != conf)
                    ConfirmPasswordTextBox.Text = conf;
            }
            finally
            {
                _isSyncingUi = false;
            }
        }

        private void OldPasswordBox_OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_isSyncingUi || _vm == null)
                return;
            _vm.AncienMotDePasse = OldPasswordBox.Password;
        }

        private void NewPasswordBox_OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_isSyncingUi || _vm == null)
                return;
            _vm.NouveauMotDePasse = NewPasswordBox.Password;
        }

        private void ConfirmPasswordBox_OnPasswordChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_isSyncingUi || _vm == null)
                return;
            _vm.ConfirmationMotDePasse = ConfirmPasswordBox.Password;
        }

        private void OldPasswordTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingUi || _vm == null)
                return;
            _vm.AncienMotDePasse = OldPasswordTextBox.Text;
        }

        private void NewPasswordTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingUi || _vm == null)
                return;
            _vm.NouveauMotDePasse = NewPasswordTextBox.Text;
        }

        private void ConfirmPasswordTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSyncingUi || _vm == null)
                return;
            _vm.ConfirmationMotDePasse = ConfirmPasswordTextBox.Text;
        }

        private void ToggleOldPassword_Click(object sender, RoutedEventArgs e)
        {
            if (OldPasswordMaskedBorder.Visibility == Visibility.Visible)
            {
                OldPasswordTextBox.Text = OldPasswordBox.Password;
                OldPasswordMaskedBorder.Visibility = Visibility.Collapsed;
                OldPasswordPlainBorder.Visibility = Visibility.Visible;
                EyeIconOld.Text = "🔒";
            }
            else
            {
                OldPasswordBox.Password = OldPasswordTextBox.Text;
                OldPasswordPlainBorder.Visibility = Visibility.Collapsed;
                OldPasswordMaskedBorder.Visibility = Visibility.Visible;
                EyeIconOld.Text = "🔓";
            }
        }

        private void ToggleNewPassword_Click(object sender, RoutedEventArgs e)
        {
            if (NewPasswordMaskedBorder.Visibility == Visibility.Visible)
            {
                NewPasswordTextBox.Text = NewPasswordBox.Password;
                NewPasswordMaskedBorder.Visibility = Visibility.Collapsed;
                NewPasswordPlainBorder.Visibility = Visibility.Visible;
                EyeIconNew.Text = "🔒";
            }
            else
            {
                NewPasswordBox.Password = NewPasswordTextBox.Text;
                NewPasswordPlainBorder.Visibility = Visibility.Collapsed;
                NewPasswordMaskedBorder.Visibility = Visibility.Visible;
                EyeIconNew.Text = "🔓";
            }
        }

        private void ToggleConfirmPassword_Click(object sender, RoutedEventArgs e)
        {
            if (ConfirmPasswordMaskedBorder.Visibility == Visibility.Visible)
            {
                ConfirmPasswordTextBox.Text = ConfirmPasswordBox.Password;
                ConfirmPasswordMaskedBorder.Visibility = Visibility.Collapsed;
                ConfirmPasswordPlainBorder.Visibility = Visibility.Visible;
                EyeIconConfirm.Text = "🔒";
            }
            else
            {
                ConfirmPasswordBox.Password = ConfirmPasswordTextBox.Text;
                ConfirmPasswordPlainBorder.Visibility = Visibility.Collapsed;
                ConfirmPasswordMaskedBorder.Visibility = Visibility.Visible;
                EyeIconConfirm.Text = "🔓";
            }
        }
    }
}
