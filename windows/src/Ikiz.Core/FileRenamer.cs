namespace Ikiz.Core;

public sealed record RenamePlanItem(FileItem File, string NewFullPath);

public sealed record RenameResult(int Id, string OldPath, string NewPath, bool Ok, string? Error);

public sealed record RenameOutcome(IReadOnlyList<RenameResult> Results)
{
    public int SuccessCount => Results.Count(r => r.Ok);
    public int FailedCount => Results.Count(r => !r.Ok);
}

/// <summary>
/// Toplu yeniden adlandırma için ad doğrulama ve plan üretimi.
/// Uzantı korunur; aynı klasörde çakışma olursa " (2)", " (3)" eklenir.
/// </summary>
public static class FileRenamer
{
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    private const int MaxNameLength = 180;

    public static string? ValidateBaseName(string? input)
        => ValidateCore((input ?? "").Trim());

    public static string? ValidateFullName(string? input)
    {
        var value = (input ?? "").Trim();
        if (value.Length == 0) return "Ad boş olamaz.";

        var extension = Path.GetExtension(value).TrimStart('.');
        if (string.IsNullOrEmpty(extension)) return "Dosya uzantısı gerekli.";
        if (!FileCategories.IsUserContent(extension))
            return "Yalnızca belge ve medya uzantıları kullanılabilir.";

        return ValidateCore(Path.GetFileNameWithoutExtension(value));
    }

    private static string? ValidateCore(string value)
    {
        if (value.Length == 0) return "Ad boş olamaz.";
        if (value is "." or "..") return "Geçersiz ad.";

        if (value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            return "Ad şu karakterleri içeremez: \\ / : * ? \" < > |";

        if (value.EndsWith(' ')) return "Ad boşluk ile bitemez.";
        if (value.EndsWith('.')) return "Ad nokta ile bitemez.";
        if (value.Length > MaxNameLength) return "Ad çok uzun.";

        var plain = value;
        var dot = plain.IndexOf('.');
        if (dot >= 0) plain = plain[..dot];
        if (ReservedNames.Contains(plain)) return "Bu ad Windows tarafından ayrılmış.";

        return null;
    }

    public static List<RenamePlanItem> BuildPlan(IEnumerable<FileItem> files, string newName, bool keepExtension)
    {
        var plan = new List<RenamePlanItem>();
        var items = files
            .Where(f => f is not null)
            .DistinctBy(f => f.Id)
            .ToList();
        var value = (newName ?? "").Trim();
        if (items.Count == 0 || value.Length == 0) return plan;

        var sources = new HashSet<string>(items.Select(f => f.FullPath), StringComparer.OrdinalIgnoreCase);
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in items)
        {
            var target = BuildTarget(file, value, keepExtension, sources, taken);
            taken.Add(target);
            plan.Add(new RenamePlanItem(file, target));
        }

        return plan
            .Where(p => !string.Equals(p.File.FullPath, p.NewFullPath, StringComparison.Ordinal))
            .ToList();
    }

    private static string BuildTarget(
        FileItem file,
        string value,
        bool keepExtension,
        HashSet<string> sources,
        HashSet<string> taken)
    {
        var ext = keepExtension && !string.IsNullOrEmpty(file.Extension) ? "." + file.Extension : "";
        var baseName = value;
        var candidate = Path.Combine(file.Directory, baseName + ext);

        var n = 2;
        while (taken.Contains(candidate) ||
               (File.Exists(candidate) && !sources.Contains(candidate)))
        {
            candidate = Path.Combine(file.Directory, $"{baseName} ({n}){ext}");
            n++;
        }

        return candidate;
    }

    public static RenameOutcome Apply(IReadOnlyList<RenamePlanItem> plan)
    {
        var results = new List<RenameResult>(plan.Count);
        foreach (var item in plan)
        {
            var oldPath = item.File.FullPath;
            var newPath = item.NewFullPath;

            try
            {
                if (!File.Exists(oldPath))
                {
                    results.Add(new RenameResult(item.File.Id, oldPath, newPath, false, "Dosya bulunamadı"));
                    continue;
                }

                if (string.Equals(oldPath, newPath, StringComparison.Ordinal))
                {
                    results.Add(new RenameResult(item.File.Id, oldPath, newPath, true, null));
                    continue;
                }

                if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
                {
                    // Yalnızca büyük/küçük harf değişiyor: ara ad üzerinden taşı.
                    var temp = newPath + ".ikiz-" + Guid.NewGuid().ToString("N") + ".tmp";
                    File.Move(oldPath, temp);
                    File.Move(temp, newPath);
                }
                else
                {
                    File.Move(oldPath, newPath);
                }

                results.Add(new RenameResult(item.File.Id, oldPath, newPath, true, null));
            }
            catch (Exception ex)
            {
                results.Add(new RenameResult(item.File.Id, oldPath, newPath, false, ex.Message));
            }
        }

        return new RenameOutcome(results);
    }
}
