using Beb64.GUI.Theming;

namespace Beb64.GUI.Services
{
    public interface IThemeService
    {
        AppTheme Current { get; }
        void ApplyTheme(AppTheme theme);
        AppTheme GetSavedOrDefault();
    }
}
