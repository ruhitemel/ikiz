namespace Ikiz.App.ViewModels;

public sealed partial class MainViewModel
{
    public bool IsLightTheme
    {
        get => ThemeManager.IsLight;
        set
        {
            if (value) ThemeManager.Apply(ThemeMode.Light);
        }
    }

    public bool IsDarkTheme
    {
        get => !ThemeManager.IsLight;
        set
        {
            if (value) ThemeManager.Apply(ThemeMode.Dark);
        }
    }

    public void OnThemeChanged()
    {
        Raise(nameof(IsLightTheme));
        Raise(nameof(IsDarkTheme));
        Refresh();
    }

    public void Refresh() => TriggerSearch(immediate: true);
}
