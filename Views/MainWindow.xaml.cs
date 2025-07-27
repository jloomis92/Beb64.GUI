using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using System.Text;

namespace Beb64.GUI.Views
{
    public partial class MainWindow : Window
    {
        private string? _fullBase64;
        private string? _lastFileExtension;

        private static readonly string[] TextExtensions = { ".txt", ".csv", ".json", ".xml" };

        public MainWindow()
        {
            InitializeComponent();
        }

        private void InputTextBox_PreviewDragEnter(object sender, DragEventArgs e)
        {
            if (IsAnyFileDrag(e))
            {
                e.Effects = DragDropEffects.Copy;
                ((TextBox)sender).Tag = "DropReady";
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void InputTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (IsAnyFileDrag(e))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void InputTextBox_PreviewDragLeave(object sender, DragEventArgs e)
        {
            ((TextBox)sender).Tag = null;
            e.Handled = true;
        }

        private void InputTextBox_PreviewDrop(object sender, DragEventArgs e)
        {
            ((TextBox)sender).Tag = null;
            try
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (files != null && files.Length > 0 && File.Exists(files[0]))
                    {
                        if (IsTextFile(files[0]))
                        {
                            string text = File.ReadAllText(files[0]);
                            ((TextBox)sender).Text = text;
                            _fullBase64 = null;
                        }
                        else
                        {
                            byte[] fileBytes = File.ReadAllBytes(files[0]);
                            string base64 = Convert.ToBase64String(fileBytes);
                            _fullBase64 = base64;
                            _lastFileExtension = Path.GetExtension(files[0]);

                            int previewLength = 200;
                            ((TextBox)sender).Text = base64.Length > previewLength
                                ? base64.Substring(0, previewLength) + "\n...\n(Preview only. Will use full data for decode.)"
                                : base64;
                        }

                        if (DataContext is Beb64.GUI.ViewModels.MainViewModel vm)
                            vm.StatusText = $"Loaded: {Path.GetFileName(files[0])} ({new FileInfo(files[0]).Length} bytes)";
                    }
                }
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"File error: {ioEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (DataContext is Beb64.GUI.ViewModels.MainViewModel vm)
                    vm.StatusText = $"File error: {ioEx.Message}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (DataContext is Beb64.GUI.ViewModels.MainViewModel vm)
                    vm.StatusText = $"Unexpected error: {ex.Message}";
            }
            e.Handled = true;
        }

        private bool IsAnyFileDrag(DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                return files != null && files.Length > 0 && File.Exists(files[0]);
            }
            return false;
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            new Views.AboutWindow { Owner = this }.ShowDialog();
        }

        private void DecodeToFile_Click(object sender, RoutedEventArgs e)
        {
            string base64 = _fullBase64 ?? InputTextBox.Text.Trim();
            if (!IsValidBase64String(base64))
            {
                MessageBox.Show("Input is not a valid Base64 string.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                if (DataContext is Beb64.GUI.ViewModels.MainViewModel vm)
                    vm.StatusText = "Input is not a valid Base64 string.";
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save Decoded File",
                Filter = "All Files|*.*",
                DefaultExt = _lastFileExtension ?? ""
            };
            if (_lastFileExtension != null)
                dialog.FileName = $"decoded{_lastFileExtension}";

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    byte[] fileBytes = Convert.FromBase64String(base64.Replace("\n", "").Replace("\r", ""));
                    if (IsTextFile(dialog.FileName))
                    {
                        string text = Encoding.UTF8.GetString(fileBytes);
                        File.WriteAllText(dialog.FileName, text);
                    }
                    else
                    {
                        File.WriteAllBytes(dialog.FileName, fileBytes);
                    }

                    string fileType = GetFriendlyFileType(_lastFileExtension ?? string.Empty);
                    if (DataContext is Beb64.GUI.ViewModels.MainViewModel vm)
                        vm.StatusText = $"File saved as {Path.GetFileName(dialog.FileName)} ({fileType})";

                    MessageBox.Show($"File saved successfully as {Path.GetFileName(dialog.FileName)} ({fileType})", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (FormatException)
                {
                    MessageBox.Show("Input is not a valid Base64 string.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (DataContext is Beb64.GUI.ViewModels.MainViewModel vm)
                        vm.StatusText = "Input is not a valid Base64 string.";
                }
                catch (IOException ioEx)
                {
                    MessageBox.Show($"File error: {ioEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (DataContext is Beb64.GUI.ViewModels.MainViewModel vm)
                        vm.StatusText = $"File error: {ioEx.Message}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (DataContext is Beb64.GUI.ViewModels.MainViewModel vm)
                        vm.StatusText = $"Unexpected error: {ex.Message}";
                }
            }
        }

        private void SaveEncodedOutput(string base64)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Save Encoded Output",
                Filter = "Base64 files (*.b64)|*.b64|Text files (*.txt)|*.txt|All Files|*.*",
                DefaultExt = ".b64",
                FileName = _lastFileExtension != null ? $"encoded{_lastFileExtension}.b64" : "encoded.b64"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(dialog.FileName, base64);
                    if (DataContext is Beb64.GUI.ViewModels.MainViewModel vm)
                        vm.StatusText = $"Base64 saved as {Path.GetFileName(dialog.FileName)}";
                    MessageBox.Show($"Base64 saved successfully as {Path.GetFileName(dialog.FileName)}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (IOException ioEx)
                {
                    MessageBox.Show($"File error: {ioEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unexpected error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private bool IsValidBase64String(string base64)
        {
            if (string.IsNullOrWhiteSpace(base64))
                return false;

            // Remove whitespace and line breaks
            base64 = base64.Trim().Replace("\r", "").Replace("\n", "");

            // Base64 length must be a multiple of 4
            if (base64.Length % 4 != 0)
                return false;

            // Regex for valid Base64 characters and padding
            return System.Text.RegularExpressions.Regex.IsMatch(
                base64,
                @"^[A-Za-z0-9+/]*={0,2}$",
                System.Text.RegularExpressions.RegexOptions.None
            );
        }

        private void InputTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = (TextBox)sender;

            // Update border color for feedback using ViewModel's logic
            if (DataContext is Beb64.GUI.ViewModels.MainViewModel vm)
            {
                bool isValid = vm.GetIsValidBase64(textBox.Text); // Call ViewModel's method
                textBox.BorderBrush = isValid
                    ? System.Windows.Media.Brushes.Green
                    : System.Windows.Media.Brushes.Red;
                // Do NOT set StatusText here; let ViewModel handle it via OnInputTextChanged
            }
        }

        private string FormatBase64String(string base64, int lineLength = 76)
        {
            if (string.IsNullOrEmpty(base64) || lineLength <= 0)
                return base64;

            return string.Join(Environment.NewLine,
                Enumerable.Range(0, base64.Length / lineLength + (base64.Length % lineLength == 0 ? 0 : 1))
                          .Select(i => base64.Substring(i * lineLength, Math.Min(lineLength, base64.Length - i * lineLength))));
        }

        private bool IsTextFile(string path)
        {
            string ext = Path.GetExtension(path);
            return TextExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
        }

        private string GetFriendlyFileType(string extension)
        {
            return extension?.ToLower() switch
            {
                ".jpg" or ".jpeg" => "JPEG Image",
                ".png" => "PNG Image",
                ".txt" => "Text File",
                ".csv" => "CSV File",
                ".json" => "JSON File",
                ".xml" => "XML File",
                ".pdf" => "PDF Document",
                _ => extension?.ToUpper() + " File"
            };
        }
    }
}
