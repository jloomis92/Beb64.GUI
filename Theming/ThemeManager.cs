using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Beb64.GUI.Theming
{

    public static class ThemeManager
    {
        public static AppTheme CurrentTheme { get; private set; } = AppTheme.Light;

        public static void ApplyTheme(AppTheme theme)
        {
            var app = Application.Current;
            if (app is null) return;

            var toRemove = app.Resources.MergedDictionaries
                .Where(d => d.Source != null && d.Source.OriginalString.StartsWith("Themes/", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var d in toRemove)
                app.Resources.MergedDictionaries.Remove(d);

            var source = new Uri($"Themes/{theme}.xaml", UriKind.Relative);
            app.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = source });

            CurrentTheme = theme;
        }

        private static void RecolorSystemMenuBrushes(Application app)
        {
            // Pull your brushes from the just-merged theme dictionary
            Brush menuBg = (Brush)app.Resources["MenuBackgroundBrush"];
            Brush menuFg = (Brush)app.Resources["MenuForegroundBrush"];
            Brush borderBrush = (Brush)app.Resources["ControlBorderBrush"];
            Brush hoverBrush = (Brush)app.Resources["ControlHoverBrush"];
            Brush selBg = (Brush)app.Resources["SelectionBackgroundBrush"];
            Brush selFg = (Brush)app.Resources["SelectionForegroundBrush"];

            // These keys are what the default MenuItem popup template uses
            app.Resources[SystemColors.MenuBrushKey] = menuBg;
            app.Resources[SystemColors.MenuTextBrushKey] = menuFg;
            app.Resources[SystemColors.ControlDarkBrushKey] = borderBrush;   // popup border & some separators
            app.Resources[SystemColors.ControlLightBrushKey] = borderBrush;
            app.Resources[SystemColors.GrayTextBrushKey] = menuFg;
            app.Resources[SystemColors.HighlightBrushKey] = selBg;
            app.Resources[SystemColors.HighlightTextBrushKey] = selFg;
        }
    }
}
