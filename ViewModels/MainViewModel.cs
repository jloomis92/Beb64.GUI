using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Beb64.GUI.Services;

namespace Beb64.GUI.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IBase64Service _base64 = new Base64Service();
        private readonly IThemeService _theme = new ThemeService();

        private bool _updatingTheme;   // re-entrancy guard

        // ---------- State ----------
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EncodeCommand))]
        [NotifyCanExecuteChangedFor(nameof(DecodeCommand))]
        private string? inputText;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
        private string? resultText;

        [ObservableProperty]
        private string? statusText;

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
        [RelayCommand(CanExecute = nameof(CanEncodeDecode))]
        private void Encode()
        {
            try
            {
                IsBusy = true;
                ResultText = _base64.Encode(InputText ?? string.Empty);
                StatusText = "Encoded.";
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
                StatusText = "Decoded.";
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
            StatusText = "Cleared.";
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
        }

        public MainViewModel()
        {
            // Apply persisted theme and sync flags
            var current = _theme.GetSavedOrDefault();
            _theme.ApplyTheme(current);

            _updatingTheme = true;
            IsLightTheme = current == AppTheme.Light;
            IsDarkTheme = current == AppTheme.Dark;
            _updatingTheme = false;
        }
    }
}
