using System.IO;

namespace Ikiz.App;

public static class AppLog
{
    private const long MaxLogBytes = 512 * 1024;

    public static void Write(string context, Exception ex)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.DataDirectory);
            var logFile = AppPaths.LogFile;
            if (File.Exists(logFile) && new FileInfo(logFile).Length > MaxLogBytes)
            {
                File.WriteAllText(logFile, "");
            }

            var message = Sanitize($"{ex.GetType().Name}: {ex.Message}");
            File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {context}: {message}\n");
        }
        catch
        {
            // logging must never break the app
        }
    }

    public static string Sanitize(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        var result = text;
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(profile))
            result = result.Replace(profile, "<kullanıcı-dizini>", StringComparison.OrdinalIgnoreCase);

        var user = Environment.UserName;
        if (!string.IsNullOrWhiteSpace(user) && user.Length >= 2)
            result = result.Replace(user, "<kullanıcı>", StringComparison.OrdinalIgnoreCase);

        return result;
    }
}
