using System;
using System.Linq;
using System.Windows;
using Beb64.GUI.Theming;

namespace Beb64.GUI.Services
{
    public class ThemeService : IThemeService
    {
        public AppTheme Current { get; private set; }

        public void ApplyTheme(AppTheme theme)
        {
            var app = Application.Current;
            if (app == null) return;

            // Remove existing theme dictionaries that live under /Themes/
            var toRemove = app.Resources.MergedDictionaries
                .Where(d => d.Source != null &&
                            d.Source.OriginalString.StartsWith("Themes/", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var d in toRemove)
                app.Resources.MergedDictionaries.Remove(d);

            // Add the new one
            var uri = new Uri($"Themes/{(theme == AppTheme.Light ? "Light" : "Dark")}.xaml", UriKind.Relative);
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = uri });

            Properties.Settings.Default.DefaultTheme = theme.ToString();
            Properties.Settings.Default.Save();
            Current = theme;
        }


        public AppTheme GetSavedOrDefault()
        {
            var saved = Properties.Settings.Default.DefaultTheme;
            return Enum.TryParse(saved, out AppTheme t) ? t : AppTheme.Light;
        }
    }
}
