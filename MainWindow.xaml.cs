using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using System.IO;
using Beb64.GUI.Services;
using Microsoft.Win32;
using System.Windows.Input;
using System.Windows.Controls;
using Beb64.GUI.Theming;

using AppSettings = Beb64.GUI.Properties.Settings;

namespace Beb64.GUI
{

    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };

        public MainWindow()
        {
            InitializeComponent();
            _statusTimer.Tick += (_, __) => StatusText.Text = "";

            LightThemeMenuItem.IsChecked = ThemeManager.CurrentTheme == AppTheme.Light;
            DarkThemeMenuItem.IsChecked = ThemeManager.CurrentTheme == AppTheme.Dark;
        }

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (SaveResultToFile())
                SetStatus("Output saved.");
        }

        private void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !string.IsNullOrWhiteSpace(ResultBox?.Text);
        }

        private bool SaveResultToFile()
        {
            if (string.IsNullOrWhiteSpace(ResultBox.Text))
                return false;

            var dlg = new SaveFileDialog
            {
                Title = "Save output",
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                FileName = "output.txt"
            };

            if (dlg.ShowDialog(this) == true)
            {
                File.WriteAllText(dlg.FileName, ResultBox.Text);
                return true;
            }
            return false;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void SetStatus(string message, bool isError = false)
        {
            StatusText.Text = message;
            StatusText.Foreground = isError ? Brushes.IndianRed : Brushes.Gray;
            _statusTimer.Stop();
            _statusTimer.Start();
        }

        private void ThemeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem mi || mi.Tag is not string tag) return;

            var theme = tag == "Dark" ? AppTheme.Dark : AppTheme.Light;
            ThemeManager.ApplyTheme(theme);

            LightThemeMenuItem.IsChecked = theme == AppTheme.Light;
            DarkThemeMenuItem.IsChecked = theme == AppTheme.Dark;

            AppSettings.Default.DefaultTheme = theme.ToString();
            AppSettings.Default.Save();
        }

        private static void UncheckSiblings(MenuItem clicked)
        {
            if (clicked.Parent is not MenuItem parent) return;
            foreach (var item in parent.Items)
            {
                if (item is MenuItem mi && mi != clicked && mi.IsCheckable)
                    mi.IsChecked = false;
            }
        }

        private void Encode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var input = InputBox.Text;
                ResultBox.Text = Encoder.EncodeToBase64(input);
            }
            catch (ArgumentException ex)
            {
                MessageBox.Show(ex.Message, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unexpected error while encoding:\n{ex}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Decode_Click(object sender, RoutedEventArgs e)
        {
            var input = InputBox.Text;

            try
            {
                ResultBox.Text = Encoder.DecodeFromBase64StrictUtf8(input);
            }
            catch (FormatException)
            {
                SetStatus("Input is not valid Base64.", isError: true);
                ResultBox.Clear();
            }
            catch (InvalidDataException ex)
            {
                // It's valid Base64, but not valid UTF-8 text
                ResultBox.Clear();
                SetStatus(ex.Message, isError: true);
            }
            catch (Exception ex)
            {
                SetStatus($"Decode error: {ex.Message}", isError: true);
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(ResultBox.Text))
            {
                Clipboard.SetText(ResultBox.Text);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    ResultBox.Focus();
                    ResultBox.SelectAll();
                }));

                SetStatus("Result copied to clipboard.");
            }
            else
            {
                SetStatus("Nothing to copy.", isError: true);
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            InputBox.Clear();
            ResultBox.Clear();
        }
    }
}
