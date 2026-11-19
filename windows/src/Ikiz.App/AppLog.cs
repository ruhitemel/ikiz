using System.IO;

namespace Ikiz.App;

public static class AppLog
{
    public static void Write(string context, Exception ex)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.DataDirectory);
            File.AppendAllText(AppPaths.LogFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}: {ex}\n\n");
        }
        catch
        {
            // logging must never break the app
        }
    }
}
