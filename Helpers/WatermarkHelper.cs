using System.Windows;
using System.Windows.Controls;

namespace Beb64.GUI.Helpers
{
    public static class WatermarkHelper
    {
        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.RegisterAttached(
                "Watermark",
                typeof(string),
                typeof(WatermarkHelper),
                new FrameworkPropertyMetadata(string.Empty));

        public static string GetWatermark(DependencyObject obj) =>
            (string)obj.GetValue(WatermarkProperty);

        public static void SetWatermark(DependencyObject obj, string value) =>
            obj.SetValue(WatermarkProperty, value);
    }
}