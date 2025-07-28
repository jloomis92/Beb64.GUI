using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using System.Text;
using Beb64.GUI.ViewModels;

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

        private async void InputTextBox_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    var file = files[0];
                    var fileInfo = new FileInfo(file);

                    // Use the smart process method instead of always encoding
                    await (DataContext as MainViewModel)?.ProcessFileAsync(file);
                    e.Handled = true;
                    return;
                }
            }
            // Existing logic for text drop...
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
