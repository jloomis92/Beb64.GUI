using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Beb64.GUI.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        [ObservableProperty] private string tagline = "Fast, simple Base64 encoding & decoding for Windows.";
        [ObservableProperty] private string versionLine = "";
        [ObservableProperty] private string publisher = "Jack Loomis";
        [ObservableProperty] private string buildDate = "";
        [ObservableProperty] private string projectUrl = "https://github.com/jloomis92/Beb64.GUI";
        [ObservableProperty] private string issuesUrl;
        [ObservableProperty] private string diagnosticsText = "";

        public AboutViewModel()
        {
            issuesUrl = ProjectUrl + "/issues";

            var asm = typeof(App).Assembly;

            var infoVer = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var fileVer = FileVersionInfo.GetVersionInfo(asm.Location).ProductVersion;
            var cleanVersion = infoVer ?? fileVer ?? asm.GetName().Version?.ToString() ?? "?.?.?";

            VersionLine = $"Version {cleanVersion} (CLR {Environment.Version})";
            BuildDate = GetBuildDate(asm).ToString("yyyy-MM-dd HH:mm:ss");
            DiagnosticsText = BuildDiagnostics(cleanVersion);
        }

        [RelayCommand]
        private void CopyDiagnostics()
        {
            try
            {
                Clipboard.SetText(DiagnosticsText);
                // Keep it silent or surface through a toast/status bar if you prefer
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy diagnostics: {ex.Message}", "BeB64",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void OpenUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open link: {ex.Message}", "BeB64",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static DateTime GetBuildDate(Assembly asm)
        {
            try
            {
                return System.IO.File.GetLastWriteTime(asm.Location);
            }
            catch
            {
                return DateTime.Now;
            }
        }

        private string BuildDiagnostics(string ver)
        {
            var sb = new StringBuilder();
            sb.AppendLine("App:      BeB64 GUI");
            sb.AppendLine($"Version:  {ver}");
            sb.AppendLine($"Assembly: {typeof(App).Assembly.FullName}");
            sb.AppendLine($"OS:       {Environment.OSVersion}");
            sb.AppendLine($".NET:     {Environment.Version}");
            sb.AppendLine($"64-bit:   {Environment.Is64BitProcess}");
            sb.AppendLine($"User:     {Environment.UserName}");
            sb.AppendLine($"Machine:  {Environment.MachineName}");
            return sb.ToString();
        }
    }
}
