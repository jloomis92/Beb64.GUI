using System.Configuration;
using System.Data;
using System.Windows;
using Beb64.GUI.Theming;
using BeB64GUI;


namespace Beb64.GUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ThemeManager.ApplyTheme(AppTheme.Light); // Set default theme
            new MainWindow().Show();
        }
    }
}
