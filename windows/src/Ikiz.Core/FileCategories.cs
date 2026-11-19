namespace Ikiz.Core;

/// <summary>
/// Yalnızca kullanıcı içeriği (belge, görsel, video, ses) indekslenir.
/// Sistem, uygulama, yapılandırma ve çalıştırılabilir dosyalar listeye girmez.
/// </summary>
public static class FileCategories
{
    private static readonly HashSet<string> Documents = new(StringComparer.OrdinalIgnoreCase)
    {
        "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx",
        "txt", "md", "rtf", "odt", "ods", "odp", "csv",
        "epub", "mobi",
    };

    private static readonly HashSet<string> Images = new(StringComparer.OrdinalIgnoreCase)
    {
        "jpg", "jpeg", "png", "gif", "webp", "bmp", "tif", "tiff",
        "heic", "heif", "svg", "raw", "cr2", "nef", "arw", "dng",
    };

    private static readonly HashSet<string> Video = new(StringComparer.OrdinalIgnoreCase)
    {
        "mp4", "mkv", "mov", "avi", "wmv", "webm", "m4v",
        "mpg", "mpeg", "flv", "3gp",
    };

    private static readonly HashSet<string> Audio = new(StringComparer.OrdinalIgnoreCase)
    {
        "mp3", "wav", "flac", "m4a", "aac", "ogg", "wma", "opus", "aiff",
    };

    public static bool IsUserContent(string? extension)
    {
        if (string.IsNullOrEmpty(extension)) return false;
        return Documents.Contains(extension)
               || Images.Contains(extension)
               || Video.Contains(extension)
               || Audio.Contains(extension);
    }

    public static string? CategoryOf(string? extension)
    {
        if (string.IsNullOrEmpty(extension)) return null;
        if (Documents.Contains(extension)) return "Belge";
        if (Images.Contains(extension)) return "Görsel";
        if (Video.Contains(extension)) return "Video";
        if (Audio.Contains(extension)) return "Ses";
        return null;
    }

    public static IReadOnlyCollection<string> DocumentExtensions => Documents;
    public static IReadOnlyCollection<string> ImageExtensions => Images;
    public static IReadOnlyCollection<string> VideoExtensions => Video;
    public static IReadOnlyCollection<string> AudioExtensions => Audio;
}
