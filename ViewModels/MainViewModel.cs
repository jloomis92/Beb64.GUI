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
            _statusText = DEFAULT_STATUS; // Always initialize

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
            if (_suppressInputStatus)
                return;

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
                StatusText = "Plain text input detected.";
            }
        }

        // Add this helper method to your ViewModel:
        private bool IsValidBase64String(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return false;

            // Remove all whitespace and line breaks
            string clean = string.Concat(base64.Where(c => !char.IsWhiteSpace(c)));

            if (clean.Length == 0 || clean.Length % 4 != 0)
                return false;

            if (!System.Text.RegularExpressions.Regex.IsMatch(
                    clean,
                    @"^[A-Za-z0-9+/]*={0,2}$",
                    System.Text.RegularExpressions.RegexOptions.None))
                return false;

            try
            {
                Convert.FromBase64String(clean);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool GetIsValidBase64(string value) => IsValidBase64String(value);

        [RelayCommand]
        public async Task EncodeAsync()
        {
            IsProgressVisible = true;
            ProgressValue = 0;
            ProgressMaximum = 100;

            var progress = new Progress<double>(value => ProgressValue = value);

            try
            {
                // Add a minimum delay for UI visibility (for testing)
                await Task.Delay(500);

                ResultText = await _base64.EncodeAsync(InputText ?? string.Empty, progress);
                StatusText = "Encoded successfully.";
            }
            catch (Exception ex)
            {
                StatusText = $"Encode failed: {ex.Message}";
            }
            finally
            {
                IsProgressVisible = false;
            }
        }

        private bool _isProgressVisible;
        public bool IsProgressVisible
        {
            get => _isProgressVisible;
            set => SetProperty(ref _isProgressVisible, value);
        }

        private double _progressValue;
        public double ProgressValue
        {
            get => _progressValue;
            set => SetProperty(ref _progressValue, value);
        }

        private double _progressMaximum = 100;
        public double ProgressMaximum
        {
            get => _progressMaximum;
            set => SetProperty(ref _progressMaximum, value);
        }

        // Add these properties to your MainViewModel
        private bool _isFileProgressVisible;
        public bool IsFileProgressVisible
        {
            get => _isFileProgressVisible;
            set => SetProperty(ref _isFileProgressVisible, value);
        }

        private double _fileProgressValue;
        public double FileProgressValue
        {
            get => _fileProgressValue;
            set => SetProperty(ref _fileProgressValue, value);
        }

        // Command to encode a file to Base64 and save output
        [RelayCommand]
        public async Task EncodeFileAsync(string inputFile)
        {
            IsFileProgressVisible = true;
            FileProgressValue = 0;
            var progress = new Progress<double>(v => FileProgressValue = v);

            try
            {
                // Suggest output filename with "-encoded.txt" appended
                string inputName = Path.GetFileNameWithoutExtension(inputFile);
                string outputName = inputName + "-encoded.txt";

                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    FileName = outputName
                };
                if (dlg.ShowDialog() == true)
                {
                    await _base64.EncodeFileToBase64Async(inputFile, dlg.FileName, progress);
                    StatusText = $"File encoded to Base64 and saved to {dlg.FileName}.";
                    ResultText = null;
                    _suppressInputStatus = true;
                    InputText = string.Empty;
                    _suppressInputStatus = false;
                    _ = ResetStatusAfterDelayAsync();
                }
                else
                {
                    StatusText = "Save canceled.";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"File encode failed: {ex.Message}";
            }
            finally
            {
                IsFileProgressVisible = false;
                FileProgressValue = 0;
            }
        }

        private async Task<bool> IsFileBase64Async(string filePath)
        {
            try
            {
                string sample;
                using (var reader = new StreamReader(filePath, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                {
                    char[] buffer = new char[8192];
                    int read = await reader.ReadAsync(buffer, 0, buffer.Length);
                    sample = new string(buffer, 0, read);
                }

                // Remove BOM if present
                if (sample.Length > 0 && sample[0] == '\uFEFF')
                    sample = sample.Substring(1);

                sample = string.Concat(sample.Where(c => !char.IsWhiteSpace(c)));

                if (sample.Length == 0 || sample.Length % 4 != 0)
                    return false;
                if (!System.Text.RegularExpressions.Regex.IsMatch(
                        sample,
                        @"^[A-Za-z0-9+/]*={0,2}$",
                        System.Text.RegularExpressions.RegexOptions.None))
                    return false;
                Convert.FromBase64String(sample);
                return true;
            }
            catch
            {
                return false;
            }
        }

        [RelayCommand]
        public async Task ProcessFileAsync(string inputFile)
        {
            IsFileProgressVisible = true;
            FileProgressValue = 0;
            var progress = new Progress<double>(v => FileProgressValue = v);

            try
            {
                bool isBase64 = await IsFileBase64Async(inputFile);

                string inputName = Path.GetFileNameWithoutExtension(inputFile);
                string inputExt = Path.GetExtension(inputFile);
                string baseName = inputName;
                while (baseName.EndsWith("-encoded", StringComparison.OrdinalIgnoreCase) ||
                       baseName.EndsWith("-decoded", StringComparison.OrdinalIgnoreCase))
                {
                    if (baseName.EndsWith("-encoded", StringComparison.OrdinalIgnoreCase))
                        baseName = baseName.Substring(0, baseName.Length - 8);
                    else if (baseName.EndsWith("-decoded", StringComparison.OrdinalIgnoreCase))
                        baseName = baseName.Substring(0, baseName.Length - 8);
                }

                string outputName;
                string filter;

                if (isBase64)
                {
                    outputName = baseName + "-decoded" + inputExt;
                    filter = "All files (*.*)|*.*";
                }
                else
                {
                    outputName = baseName + "-encoded.txt";
                    filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                }

                var dlg = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = filter,
                    FileName = outputName,
                };
                if (dlg.ShowDialog() == true)
                {
                    if (isBase64)
                    {
                        if (isBase64)
                        {
                            // Use streaming decode for large files, but pre-clean the file for whitespace and BOM
                            try
                            {
                                // Create a temp file with cleaned Base64 (no whitespace, no BOM)
                                string tempBase64File = Path.GetTempFileName();
                                using (var reader = new StreamReader(inputFile, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
                                using (var writer = new StreamWriter(tempBase64File, false, System.Text.Encoding.UTF8))
                                {
                                    bool firstLine = true;
                                    while (!reader.EndOfStream)
                                    {
                                        string line = await reader.ReadLineAsync() ?? "";
                                        if (firstLine && line.Length > 0 && line[0] == '\uFEFF')
                                            line = line.Substring(1);
                                        firstLine = false;
                                        foreach (char c in line)
                                        {
                                            // Only write valid Base64 characters
                                            if ((c >= 'A' && c <= 'Z') ||
                                                (c >= 'a' && c <= 'z') ||
                                                (c >= '0' && c <= '9') ||
                                                c == '+' || c == '/' || c == '=')
                                            {
                                                writer.Write(c);
                                            }
                                        }
                                    }
                                }

                                await _base64.DecodeBase64FileAsync(tempBase64File, dlg.FileName, progress);

                                // Clean up temp file
                                File.Delete(tempBase64File);

                                StatusText = $"File decoded from Base64 and saved to {dlg.FileName}.";
                                ResultText = null;
                                _suppressInputStatus = true;
                                InputText = string.Empty;
                                _suppressInputStatus = false;
                                _ = ResetStatusAfterDelayAsync();
                            }
                            catch (Exception ex)
                            {
                                StatusText = $"File decode failed: {ex.Message}";
                            }
                        }
                    }
                    else
                    {
                        await _base64.EncodeFileToBase64Async(inputFile, dlg.FileName, progress);
                        StatusText = $"File encoded to Base64 and saved to {dlg.FileName}.";
                        ResultText = null;
                        InputText = string.Empty;
                        _ = ResetStatusAfterDelayAsync();
                    }
                }
                else
                {
                    StatusText = "Save canceled.";
                }
            }
            catch (Exception ex)
            {
                StatusText = $"File process failed: {ex.Message}";
            }
            finally
            {
                IsFileProgressVisible = false;
                FileProgressValue = 0;
            }
        }

        // Helper method for delayed status reset
        private async Task ResetStatusAfterDelayAsync(int milliseconds = 5000)
        {
            var currentStatus = StatusText;
            await Task.Delay(milliseconds);
            // Only reset if the status hasn't changed in the meantime
            if (StatusText == currentStatus)
                StatusText = DEFAULT_STATUS;
        }

        private bool _suppressInputStatus;
    }
}
