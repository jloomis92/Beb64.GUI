using System;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Navigation;

namespace Beb64.GUI.Views
{
    public partial class AboutWindow : Window
    {
        public string Tagline { get; }
        public string VersionLine { get; }
        public string Publisher { get; }
        public string BuildDate { get; }
        public string ProjectUrl { get; }
        public string IssuesUrl { get; }
        public string DiagnosticsText { get; }

        public AboutWindow()
        {
            InitializeComponent();

            Tagline = "Fast, simple Base64 encoding & decoding for Windows.";

            var asm = typeof(App).Assembly;

            // Prefer InformationalVersion (we fixed it to avoid +hash)
            var infoVer = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var fileVer = FileVersionInfo.GetVersionInfo(asm.Location).ProductVersion;
            var cleanVersion = infoVer ?? fileVer ?? asm.GetName().Version?.ToString() ?? "?.?.?";

            VersionLine = $"Version {cleanVersion} (CLR {Environment.Version})";
            Publisher = "Jack Loomis";
            BuildDate = GetBuildDate(asm).ToString("yyyy-MM-dd HH:mm:ss");

            // TODO: set to your repo
            ProjectUrl = "https://github.com/youraccount/yourrepo";
            IssuesUrl = ProjectUrl + "/issues";

            DiagnosticsText = BuildDiagnostics(cleanVersion);
        }

        private static DateTime GetBuildDate(Assembly asm)
        {
            try
            {
                var path = asm.Location;
                return System.IO.File.GetLastWriteTime(path);
            }
            catch
            {
                return DateTime.Now;
            }
        }

        private string BuildDiagnostics(string ver)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"App:      BeB64 GUI");
            sb.AppendLine($"Version:  {ver}");
            sb.AppendLine($"Assembly: {typeof(App).Assembly.FullName}");
            sb.AppendLine($"OS:       {Environment.OSVersion}");
            sb.AppendLine($".NET:     {Environment.Version}");
            sb.AppendLine($"64-bit:   {Environment.Is64BitProcess}");
            sb.AppendLine($"User:     {Environment.UserName}");
            sb.AppendLine($"Machine:  {Environment.MachineName}");
            return sb.ToString();
        }

        private void CopyDiagnostics_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(DiagnosticsText);
                MessageBox.Show(this, "Diagnostics copied to clipboard.", "BeB64",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to copy: " + ex.Message, "BeB64",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                e.Handled = true;
            }
            catch { /* ignore */ }
        }
    }
}
