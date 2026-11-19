using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace Ikiz.App;

public enum ThemeMode
{
    Dark,
    Light,
}

/// <summary>
/// Açık / koyu tema sözlüklerini değiştirir ve seçimi kalıcılaştırır.
/// </summary>
public static class ThemeManager
{
    private static readonly string SettingsPath = Path.Combine(AppPaths.DataDirectory, "theme.txt");

    public static ThemeMode Mode { get; private set; } = ThemeMode.Dark;
    public static bool IsLight => Mode == ThemeMode.Light;

    public static event Action? Changed;

    public static void Initialize()
    {
        Mode = LoadPreference() ?? SystemPreference();
        SwapDictionary(Mode);
    }

    public static void Apply(ThemeMode mode)
    {
        if (Mode == mode) return;
        Mode = mode;
        SwapDictionary(mode);
        SavePreference(mode);
        Changed?.Invoke();
    }

    public static void Toggle() => Apply(IsLight ? ThemeMode.Dark : ThemeMode.Light);

    private static void SwapDictionary(ThemeMode mode)
    {
        var app = Application.Current;
        if (app is null) return;

        var source = mode == ThemeMode.Light ? "Theme/Light.xaml" : "Theme/Dark.xaml";
        var dictionary = new ResourceDictionary { Source = new Uri(source, UriKind.Relative) };
        var merged = app.Resources.MergedDictionaries;

        for (var i = 0; i < merged.Count; i++)
        {
            var original = merged[i].Source?.OriginalString ?? "";
            if (original.Contains("theme/dark.xaml", StringComparison.OrdinalIgnoreCase) ||
                original.Contains("theme/light.xaml", StringComparison.OrdinalIgnoreCase))
            {
                merged[i] = dictionary;
                return;
            }
        }

        merged.Insert(0, dictionary);
    }

    private static ThemeMode? LoadPreference()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return null;
            var text = File.ReadAllText(SettingsPath).Trim();
            if (text.Equals("light", StringComparison.OrdinalIgnoreCase)) return ThemeMode.Light;
            if (text.Equals("dark", StringComparison.OrdinalIgnoreCase)) return ThemeMode.Dark;
        }
        catch
        {
            // okuma hatası tema seçimini engellemez
        }

        return null;
    }

    private static void SavePreference(ThemeMode mode)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, mode == ThemeMode.Light ? "light" : "dark");
        }
        catch
        {
            // yazma hatası sessizce yok sayılır
        }
    }

    private static ThemeMode SystemPreference()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int value)
                return value == 0 ? ThemeMode.Dark : ThemeMode.Light;
        }
        catch
        {
            // kayıt defteri okunamazsa koyu tema
        }

        return ThemeMode.Dark;
    }
}
