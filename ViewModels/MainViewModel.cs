using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Beb64.GUI.Services;
using Beb64.GUI.Theming;

namespace Beb64.GUI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private const string DEFAULT_STATUS = "Welcome to BeB64! Ready to encode or decode.";

        private readonly IBase64Service _base64 = new Base64Service();
        private readonly IThemeService _theme = new ThemeService();

        private bool _updatingTheme;   // prevents recursion when toggling themes

        // ---------- State ----------
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EncodeCommand))]
        [NotifyCanExecuteChangedFor(nameof(DecodeCommand))]
        private string? inputText;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string? resultText;

        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EncodeCommand))]
        [NotifyCanExecuteChangedFor(nameof(DecodeCommand))]
        private bool isBusy;

        // Two-way theme flags
        [ObservableProperty]
        private bool isLightTheme;

        partial void OnIsLightThemeChanged(bool value)
        {
            if (_updatingTheme) return;
            if (value)
            {
                _updatingTheme = true;
                SetTheme(AppTheme.Light, updateFlags: false);
                IsDarkTheme = false;
                _updatingTheme = false;
            }
        }

        [ObservableProperty]
        private bool isDarkTheme;

        partial void OnIsDarkThemeChanged(bool value)
        {
            if (_updatingTheme) return;
            if (value)
            {
                _updatingTheme = true;
                SetTheme(AppTheme.Dark, updateFlags: false);
                IsLightTheme = false;
                _updatingTheme = false;
            }
        }

        // ---------- Commands ----------
        public MainViewModel()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
                return;

                // Apply persisted theme & sync flags
                var current = _theme.GetSavedOrDefault();
                _theme.ApplyTheme(current);

                _updatingTheme = true;
                IsLightTheme = current == AppTheme.Light;
                IsDarkTheme = current == AppTheme.Dark;
                _updatingTheme = false;
        }

        [RelayCommand(CanExecute = nameof(CanEncodeDecode))]
        private void Encode()
        {
            try
            {
                IsBusy = true;
                ResultText = _base64.Encode(InputText ?? string.Empty);
                StatusText = "Encoded successfully.";
            }
            catch (Exception ex)
            {
                StatusText = $"Encode failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        [RelayCommand(CanExecute = nameof(CanEncodeDecode))]
        private void Decode()
        {
            try
            {
                IsBusy = true;
                if (!_base64.TryDecode(InputText ?? string.Empty, out var result, out var error))
                {
                    StatusText = $"Invalid Base64: {error}";
                    ResultText = string.Empty;
                    return;
                }
                ResultText = result;
                StatusText = "Decoded successfully.";
            }
            catch (Exception ex)
            {
                StatusText = $"Decode failed: {ex.Message}";
            }
            finally { IsBusy = false; }
        }

        private bool CanEncodeDecode() =>
            !IsBusy && !string.IsNullOrWhiteSpace(InputText);

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void Save()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = "output.txt"
            };
            if (dlg.ShowDialog() == true)
            {
                File.WriteAllText(dlg.FileName, ResultText ?? string.Empty);
                StatusText = $"Saved to {dlg.FileName}";
            }
        }

        private bool CanSave() => !string.IsNullOrWhiteSpace(ResultText);

        [RelayCommand]
        private void Clear()
        {
            InputText = string.Empty;
            ResultText = string.Empty;
            StatusText = DEFAULT_STATUS;   // <- reset to default
        }

        [RelayCommand] private void Exit() => Application.Current.Shutdown();

        [RelayCommand] private void SetLightTheme() => SetTheme(AppTheme.Light);
        [RelayCommand] private void SetDarkTheme() => SetTheme(AppTheme.Dark);

        private void SetTheme(AppTheme theme, bool updateFlags = true)
        {
            _theme.ApplyTheme(theme);

            if (updateFlags)
            {
                _updatingTheme = true;
                IsLightTheme = theme == AppTheme.Light;
                IsDarkTheme = theme == AppTheme.Dark;
                _updatingTheme = false;
            }

            // Only overwrite the default welcome message—don’t spam status if user already did actions
            if (StatusText == DEFAULT_STATUS)
                StatusText = $"Theme set to {theme}.";
        }

        partial void OnInputTextChanged(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                StatusText = "Input is empty.";
            }
            else if (IsValidBase64String(value))
            {
                StatusText = "Valid Base64 input.";
            }
            else
            {
                StatusText = "Invalid Base64 input.";
            }
        }

        // Add this helper method to your ViewModel:
        private bool IsValidBase64String(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return false;

            base64 = base64.Trim().Replace("\r", "").Replace("\n", "");
            if (base64.Length % 4 != 0)
                return false;

            return System.Text.RegularExpressions.Regex.IsMatch(
                base64,
                @"^[A-Za-z0-9+/]*={0,2}$",
                System.Text.RegularExpressions.RegexOptions.None
            );
        }
    }
}
