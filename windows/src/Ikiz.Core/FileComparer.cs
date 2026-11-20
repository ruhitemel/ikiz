using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Ikiz.Core;

public sealed record ComparedFileEntry(
    FileItem File,
    bool Exists,
    long ExactBytes,
    DateTime ModifiedUtc,
    string Sha256Hex,
    string ShortHash,
    string ContentGroupLabel,
    string PreviewText,
    bool IsOldest,
    bool IsNewest);

public sealed record FileComparisonReport(
    IReadOnlyList<ComparedFileEntry> Entries,
    bool AllIdenticalContent,
    int DistinctHashCount,
    string VerdictTitle,
    string VerdictDetail);

/// <summary>
/// Seçilen dosyaları bayt boyutu, değiştirilme tarihi, SHA-256 içerik parmak izi ve
/// kısa önizleme metniyle karşılaştırır.
/// </summary>
public static class FileComparer
{
    private static readonly HashSet<string> TextPreviewExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        "txt", "md", "csv", "rtf",
    };

    public static FileComparisonReport Compare(IReadOnlyList<FileItem> files, CancellationToken token = default)
    {
        var distinctFiles = files
            .Where(f => f is not null)
            .DistinctBy(f => f.Id)
            .ToList();

        if (distinctFiles.Count == 0)
        {
            return new FileComparisonReport(
                Array.Empty<ComparedFileEntry>(),
                AllIdenticalContent: false,
                DistinctHashCount: 0,
                VerdictTitle: "Karşılaştırılacak dosya yok",
                VerdictDetail: "En az iki dosya seçerek içeriklerini karşılaştırabilirsin.");
        }

        var raw = new List<(FileItem File, bool Exists, long Bytes, DateTime ModifiedUtc, string Hash, string ShortHash, string Preview)>(distinctFiles.Count);

        foreach (var item in distinctFiles)
        {
            token.ThrowIfCancellationRequested();

            if (item.IsDirectory)
            {
                var dirExists = Directory.Exists(item.FullPath);
                var dirTime = dirExists ? Directory.GetLastWriteTimeUtc(item.FullPath) : item.ModifiedUtc;
                raw.Add((item, dirExists, 0, dirTime, "DIR-EMPTY", "BOŞ KLASÖR", "İçeriksiz boş klasör"));
                continue;
            }

            if (!File.Exists(item.FullPath))
            {
                raw.Add((item, false, item.Size, item.ModifiedUtc, "MISSING", "BULUNAMADI", "Dosya diskte bulunamadı"));
                continue;
            }

            try
            {
                var info = new FileInfo(item.FullPath);
                var bytes = info.Length;
                var mod = info.LastWriteTimeUtc;
                var hashHex = ComputeSha256Hex(item.FullPath);
                var shortHash = bytes == 0
                    ? "0 KB · BOŞ DOSYA"
                    : $"{hashHex[..8]} · {hashHex[8..16]}";
                var preview = ReadPreview(item.FullPath, item.Extension, bytes);
                raw.Add((item, true, bytes, mod, hashHex, shortHash, preview));
            }
            catch (Exception ex)
            {
                raw.Add((item, false, item.Size, item.ModifiedUtc, "ERROR", "OKUNAMADI", ex.Message));
            }
        }

        var alive = raw.Where(r => r.Exists).ToList();
        var oldestUtc = alive.Count > 0 ? alive.Min(r => r.ModifiedUtc) : DateTime.MinValue;
        var newestUtc = alive.Count > 0 ? alive.Max(r => r.ModifiedUtc) : DateTime.MaxValue;
        var hasTimeSpread = alive.Count > 1 && oldestUtc != newestUtc;

        var hashGroups = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var r in alive)
        {
            var key = $"{r.Bytes}:{r.Hash}";
            if (!hashGroups.ContainsKey(key))
                hashGroups[key] = hashGroups.Count + 1;
        }

        var distinctHashes = hashGroups.Count;
        var allIdentical = alive.Count >= 2 && distinctHashes == 1 && alive.Count == raw.Count;

        var entries = new List<ComparedFileEntry>(raw.Count);
        var oldestAssigned = false;
        var newestAssigned = false;

        foreach (var r in raw)
        {
            var isOldest = false;
            var isNewest = false;
            if (r.Exists)
            {
                if (!oldestAssigned && r.ModifiedUtc == oldestUtc)
                {
                    isOldest = true;
                    oldestAssigned = true;
                }
                if (hasTimeSpread && !newestAssigned && r.ModifiedUtc == newestUtc && !isOldest)
                {
                    isNewest = true;
                    newestAssigned = true;
                }
            }

            var groupLabel = !r.Exists
                ? "Okunamadı"
                : allIdentical
                    ? "Birebir aynı içerik"
                    : $"İçerik #{hashGroups[$"{r.Bytes}:{r.Hash}"]}";

            entries.Add(new ComparedFileEntry(
                File: r.File,
                Exists: r.Exists,
                ExactBytes: r.Bytes,
                ModifiedUtc: r.ModifiedUtc,
                Sha256Hex: r.Hash,
                ShortHash: r.ShortHash,
                ContentGroupLabel: groupLabel,
                PreviewText: r.Preview,
                IsOldest: isOldest,
                IsNewest: isNewest));
        }

        string verdictTitle;
        string verdictDetail;
        if (raw.Count == 1)
        {
            verdictTitle = "Tek dosya inceleniyor";
            verdictDetail = "Karşılaştırma için bir çift grubunu veya en az iki dosyayı seçebilirsin.";
        }
        else if (allIdentical)
        {
            verdictTitle = "%100 Birebir Aynı İçerik (SHA-256 Eşleşti)";
            verdictDetail = $"Karşılaştırılan {raw.Count} öğenin bayt boyutu ve SHA-256 içerik parmak izi birebir aynı. Fazla kopyaları güvenle temizleyebilirsin.";
        }
        else
        {
            verdictTitle = $"Dikkat: İçerikler Farklı ({Math.Max(1, distinctHashes)} farklı içerik)";
            verdictDetail = "Bu dosyaların tümü birebir aynı içeriğe sahip değil. Silmeden önce SHA-256 parmak izlerini, boyutlarını ve tarihlerini kontrol et.";
        }

        return new FileComparisonReport(
            Entries: entries,
            AllIdenticalContent: allIdentical,
            DistinctHashCount: distinctHashes,
            VerdictTitle: verdictTitle,
            VerdictDetail: verdictDetail);
    }

    private static string ComputeSha256Hex(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(stream, hash);
        return Convert.ToHexString(hash);
    }

    private static string ReadPreview(string path, string extension, long bytes)
    {
        var category = FileCategories.CategoryOf(extension) ?? "Dosya";
        var formattedBytes = bytes.ToString("N0", CultureInfo.GetCultureInfo("tr-TR")) + " bayt";
        if (bytes == 0)
            return $"0 bayt · İçeriksiz boş {category.ToLowerInvariant()} dosyası";

        if (!TextPreviewExtensions.Contains(extension))
            return $"{category} · {formattedBytes}";

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var buffer = new char[140];
            var read = reader.Read(buffer, 0, buffer.Length);
            if (read <= 0) return $"{category} · {formattedBytes}";

            var text = new string(buffer, 0, read)
                .Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal)
                .Trim();

            if (text.Any(c => char.IsControl(c) && c != '\t'))
                return $"{category} · {formattedBytes}";

            return text.Length > 110 ? $"\"{text[..110]}…\"" : $"\"{text}\"";
        }
        catch
        {
            return $"{category} · {formattedBytes}";
        }
    }
}
