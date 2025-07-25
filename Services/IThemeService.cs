namespace Beb64.GUI.Services
{
    public enum AppTheme
    {
        Light,
        Dark
    }

    public interface IThemeService
    {
        AppTheme Current { get; }
        void ApplyTheme(AppTheme theme);
        AppTheme GetSavedOrDefault();
    }
}
