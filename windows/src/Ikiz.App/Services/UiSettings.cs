using System.IO;

namespace Ikiz.App.Services;

/// <summary>Arayüz tercihlerini yerel veri dizininde saklar.</summary>
public static class UiSettings
{
    private static readonly string HideStagedPath = Path.Combine(AppPaths.DataDirectory, "hide-staged.txt");

    public static bool HideStaged
    {
        get
        {
            try
            {
                if (!File.Exists(HideStagedPath)) return true;
                return File.ReadAllText(HideStagedPath).Trim() != "0";
            }
            catch
            {
                return true;
            }
        }
        set
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(HideStagedPath)!);
                File.WriteAllText(HideStagedPath, value ? "1" : "0");
            }
            catch
            {
                // tercih kaydedilemezse oturum içinde çalışmaya devam et
            }
        }
    }
}
