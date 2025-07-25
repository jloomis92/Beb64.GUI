using System.Reflection;
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

            IThemeService themeService = new ThemeService();
            themeService.ApplyTheme(themeService.GetSavedOrDefault());

            var ver = typeof(App).Assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "?.?.?";

            MainWindow window = new MainWindow { Title = $"Beb64 {ver}" };
            window.Show();
        }
    }
}
