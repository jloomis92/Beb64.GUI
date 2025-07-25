using System.Windows;
using Beb64.GUI.Services;
using Beb64.GUI.Views;

namespace Beb64.GUI
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Let ThemeService swap the default Light dictionary with the saved one.
            IThemeService themeService = new ThemeService();
            themeService.ApplyTheme(themeService.GetSavedOrDefault());

            new MainWindow().Show();
        }
    }
}
