using System.IO;

namespace Ikiz.App;

/// <summary>Uygulamanın kullanıcıya ait veri ve günlük dosyalarının konumu.</summary>
public static class AppPaths
{
    public static string DataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Ikiz");

    public static string LogFile => Path.Combine(DataDirectory, "ikiz-error.log");
}
