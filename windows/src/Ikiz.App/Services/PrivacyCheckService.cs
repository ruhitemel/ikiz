using System.IO;
using System.Text.RegularExpressions;

namespace Ikiz.App.Services;

public sealed record PrivacyFinding(string Title, string Detail, bool IsOk, bool IsWarning = false);

/// <summary>
/// Paylaşım (ekran görüntüsü / kayıt) öncesi uygulamanın elinde tuttuğu verileri denetler.
/// Çıktıda gerçek kullanıcı adı, yerel yol veya dosya adı asla görünmez.
/// </summary>
public static partial class PrivacyCheckService
{
    public static IReadOnlyList<PrivacyFinding> Check(
        IReadOnlyList<string> roots,
        IReadOnlyList<string> fileNames,
        string logPath,
        int indexedCount)
    {
        var corpus = new List<string>(fileNames);
        corpus.AddRange(roots);
        corpus.Add(ReadLogTail(logPath));
        var joined = string.Join("\n", corpus);

        var emails = EmailRegex().Matches(joined).Count;
        var ips = IPv4Regex().Matches(joined).Cast<Match>().Select(m => m.Value).Distinct(StringComparer.Ordinal).Count();
        var userName = Environment.UserName;
        var namedFiles = string.IsNullOrWhiteSpace(userName)
            ? 0
            : fileNames.Count(n => n.Contains(userName, StringComparison.OrdinalIgnoreCase));

        var list = new List<PrivacyFinding>
        {
            new("E-posta adresi",
                emails == 0
                    ? $"{Humanize(fileNames.Count)} dosya adı, {roots.Count} kök ve günlük tarandı; e-posta deseni yok."
                    : $"{emails} e-posta benzeri desen bulundu. Paylaşmadan önce kaynağı kontrol et.",
                emails == 0, emails > 0),

            new("IP adresi",
                ips == 0
                    ? "Ağ adresi deseni bulunamadı. Uygulama IP adresi toplamaz."
                    : $"{ips} IP adresi benzeri desen bulundu. Paylaşmadan önce kaynağı kontrol et.",
                ips == 0, ips > 0),

            new("Kişisel yol",
                roots.Count == 0
                    ? "Tanımlı tarama kökü yok."
                    : $"{roots.Count} kök yalnızca bu cihazda tutuluyor; gösterimde kullanıcı adı maskelenir. " +
                      MaskPath(roots[0]),
                true),

            new("Kullanıcı adı",
                namedFiles == 0
                    ? "Dosya adlarında oturum açan kullanıcının adı geçmiyor."
                    : $"{namedFiles} dosya adında kullanıcı adı geçebiliyor; ekran görüntüsünde bu adlar görünmesin.",
                namedFiles == 0, namedFiles > 0),

            new("Günlük dosyası",
                File.Exists(logPath)
                    ? "Hata kaydı yerel veri dizininde. Paylaşım paketine dahil edilmez."
                    : "Hata kaydı oluşmadı.",
                true),

            new("Gizli anahtar / erişim belirteci",
                "Uygulamada saklanan anahtar yok. Yayın iş akışında yalnızca GitHub'ın geçici belirteci kullanılır.",
                true),

            new("Dosya verisi aktarımı",
                "Dosya adı, yol ve içerik hiçbir sunucuya gönderilmez. Güncelleme denetimi yalnızca herkese açık sürüm bilgisini okur.",
                true),

            new("Geliştirici notu",
                "Ad ve kullanıcı adı yalnızca Hakkında bölümünde görünür; e-posta adresi uygulamada yer almaz.",
                true),

            new("İndeks kapsamı",
                $"Şu an {Humanize(indexedCount)} dosya indeksli. Hiçbir dosya adı bu denetim ekranında listelenmez.",
                true),
        };

        return list;
    }

    private static string ReadLogTail(string path)
    {
        try
        {
            if (!File.Exists(path)) return "";
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var length = Math.Min(stream.Length, 200_000);
            stream.Seek(-length, SeekOrigin.End);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch
        {
            return "";
        }
    }

    private static string MaskPath(string path)
    {
        var masked = path;
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(profile))
            masked = masked.Replace(profile, "<kullanıcı>", StringComparison.OrdinalIgnoreCase);

        var user = Environment.UserName;
        if (!string.IsNullOrWhiteSpace(user))
            masked = masked.Replace(user, "<kullanıcı>", StringComparison.OrdinalIgnoreCase);

        return masked.Length > 96 ? masked[..96] + "…" : masked;
    }

    private static string Humanize(int count) => count.ToString("N0", new System.Globalization.CultureInfo("tr-TR"));

    [GeneratedRegex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}")]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"\b(?:25[0-5]|2[0-4]\d|1?\d?\d)(?:\.(?:25[0-5]|2[0-4]\d|1?\d?\d)){3}\b")]
    private static partial Regex IPv4Regex();
}
