using System.Collections.Concurrent;
using System.Globalization;

namespace Ikiz.Core;

public sealed class SearchRequest
{
    public string Text { get; init; } = "";
    public bool SearchPath { get; init; }
    public string Type { get; init; } = "all";
    public string Sort { get; init; } = "waste";
    public bool OnlyDuplicates { get; init; } = true;
    public bool GroupMode { get; init; } = true;
    public int MaxGroups { get; init; } = 400;
    public int MaxFiles { get; init; } = 800;
}

public sealed record DuplicateGroup(string Key, string Name, string Extension, long Size, IReadOnlyList<FileItem> Files)
{
    public int Count => Files.Count;
    public long Waste => Size * (Files.Count - 1);
}

public sealed class SearchResult
{
    public required IReadOnlyList<DuplicateGroup> Groups { get; init; }
    public required IReadOnlyList<FileItem> Files { get; init; }
    public required IReadOnlyDictionary<string, int> TypeCounts { get; init; }
    public required int MatchedCount { get; init; }
    public required int DuplicateGroupCount { get; init; }
    public required int DuplicateFileCount { get; init; }
    public required long TotalWaste { get; init; }
    public required bool Truncated { get; init; }
}

public sealed class FileIndex
{
    private static readonly StringComparer TrComparer =
        StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), true);

    private FileItem[] _snapshot = Array.Empty<FileItem>();
    private Dictionary<string, List<FileItem>> _byKey = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<int, byte> _removed = new();
    private int _nextId;

    public int TotalCount => _snapshot.Length;
    public int AliveCount => _snapshot.Length - _removed.Count;

    public void ReplaceAll(IReadOnlyList<FileCandidate> candidates)
    {
        var items = new FileItem[candidates.Count];
        var byKey = new Dictionary<string, List<FileItem>>(StringComparer.Ordinal);
        for (var i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            var item = new FileItem
            {
                Id = ++_nextId,
                FullPath = c.FullPath,
                Name = c.Name,
                Directory = c.Directory,
                Extension = c.Extension,
                Size = c.Size,
                ModifiedUtc = c.ModifiedUtc,
            };
            items[i] = item;
            if (!byKey.TryGetValue(item.DuplicateKey, out var list))
                byKey[item.DuplicateKey] = list = new List<FileItem>(2);
            list.Add(item);
        }

        _byKey = byKey;
        _removed.Clear();
        _snapshot = items;
    }

    public IReadOnlyList<FileItem> GetByKey(string key)
    {
        if (!_byKey.TryGetValue(key, out var list)) return Array.Empty<FileItem>();
        return list
            .Where(f => !_removed.ContainsKey(f.Id))
            .OrderBy(f => f.Directory, StringComparer.OrdinalIgnoreCase)
            .ThenBy(f => f.FullPath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public int AliveCountForKey(string key)
    {
        if (!_byKey.TryGetValue(key, out var list)) return 0;
        return list.Count(f => !_removed.ContainsKey(f.Id));
    }

    public void Remove(int id) => _removed[id] = 1;

    /// <summary>
    /// Yeniden adlandırılan dosyaları indekste günceller ve yeni öğeleri döndürür.
    /// </summary>
    public IReadOnlyList<FileItem> ApplyRenames(IReadOnlyDictionary<int, string> newPaths)
    {
        if (newPaths.Count == 0) return Array.Empty<FileItem>();

        var updated = new List<FileItem>(newPaths.Count);
        var items = _snapshot;
        for (var i = 0; i < items.Length; i++)
        {
            var old = items[i];
            if (!newPaths.TryGetValue(old.Id, out var path)) continue;

            var name = Path.GetFileName(path);
            var directory = Path.GetDirectoryName(path) ?? old.Directory;
            var extension = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
            var replacement = new FileItem
            {
                Id = old.Id,
                FullPath = path,
                Name = name,
                Directory = directory,
                Extension = extension,
                Size = old.Size,
                ModifiedUtc = old.ModifiedUtc,
            };

            items[i] = replacement;
            updated.Add(replacement);
        }

        if (updated.Count > 0) RebuildKeyIndex();
        return updated;
    }

    /// <summary>
    /// Geri yüklenen dosyaları indekse tekrar ekler (yeni kimliklerle).
    /// </summary>
    public void Reinsert(IReadOnlyList<FileItem> files)
    {
        if (files.Count == 0) return;

        var existing = new HashSet<string>(
            _snapshot.Where(f => !_removed.ContainsKey(f.Id)).Select(f => f.FullPath),
            StringComparer.OrdinalIgnoreCase);

        var items = _snapshot.ToList();
        var added = false;
        foreach (var file in files)
        {
            if (string.IsNullOrWhiteSpace(file.FullPath)) continue;
            if (!existing.Add(file.FullPath)) continue;

            items.Add(new FileItem
            {
                Id = ++_nextId,
                FullPath = file.FullPath,
                Name = file.Name,
                Directory = file.Directory,
                Extension = file.Extension,
                Size = file.Size,
                ModifiedUtc = file.ModifiedUtc,
            });
            added = true;
        }

        if (!added) return;
        _snapshot = items.ToArray();
        RebuildKeyIndex();
    }

    private void RebuildKeyIndex()
    {
        var byKey = new Dictionary<string, List<FileItem>>(StringComparer.Ordinal);
        foreach (var item in _snapshot)
        {
            if (_removed.ContainsKey(item.Id)) continue;
            if (!byKey.TryGetValue(item.DuplicateKey, out var list))
                byKey[item.DuplicateKey] = list = new List<FileItem>(2);
            list.Add(item);
        }

        _byKey = byKey;
    }

    public SearchResult Search(SearchRequest request, CancellationToken token = default)
    {
        var items = _snapshot;
        var tokens = TextNormalizer.Tokenize(request.Text);
        var allTypes = string.Equals(request.Type, "all", StringComparison.OrdinalIgnoreCase);
        var needGroups = request.GroupMode || request.OnlyDuplicates;

        var matched = new List<FileItem>(Math.Min(items.Length, 4096));
        var typeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        Dictionary<string, List<FileItem>>? byKey = null;

        for (var i = 0; i < items.Length; i++)
        {
            if ((i & 0x3FF) == 0) token.ThrowIfCancellationRequested();
            var f = items[i];
            if (_removed.ContainsKey(f.Id)) continue;

            if (tokens.Length > 0)
            {
                var hay = request.SearchPath ? f.FoldedPath : f.FoldedName;
                var ok = true;
                foreach (var t in tokens)
                {
                    if (hay.Contains(t, StringComparison.Ordinal)) continue;
                    ok = false;
                    break;
                }
                if (!ok) continue;
            }

            typeCounts[f.Extension] = typeCounts.GetValueOrDefault(f.Extension) + 1;
            if (!allTypes && !string.Equals(f.Extension, request.Type, StringComparison.OrdinalIgnoreCase))
                continue;

            matched.Add(f);
            if (!needGroups) continue;

            byKey ??= new Dictionary<string, List<FileItem>>(StringComparer.Ordinal);
            if (!byKey.TryGetValue(f.DuplicateKey, out var list))
                byKey[f.DuplicateKey] = list = new List<FileItem>(2);
            list.Add(f);
        }

        var groups = new List<DuplicateGroup>();
        long waste = 0;
        var duplicateFileCount = 0;
        if (byKey is not null)
        {
            foreach (var (key, list) in byKey)
            {
                if (list.Count < 2) continue;
                list.Sort((a, b) => string.Compare(a.FullPath, b.FullPath, StringComparison.OrdinalIgnoreCase));
                var first = list[0];
                var group = new DuplicateGroup(key, first.Name, first.Extension, first.Size, list);
                groups.Add(group);
                waste += group.Waste;
                duplicateFileCount += list.Count;
            }
        }

        SortGroups(groups, request.Sort);
        var totalGroupCount = groups.Count;
        var truncated = totalGroupCount > request.MaxGroups;
        if (truncated) groups = groups.Take(request.MaxGroups).ToList();

        var flat = new List<FileItem>();
        if (!request.GroupMode)
        {
            IEnumerable<FileItem> source = matched;
            if (request.OnlyDuplicates)
            {
                var keys = new HashSet<string>(byKey?.Where(kv => kv.Value.Count > 1).Select(kv => kv.Key)
                    ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
                source = matched.Where(f => keys.Contains(f.DuplicateKey));
            }

            flat = SortFiles(source, request.Sort).Take(request.MaxFiles).ToList();
            truncated |= matched.Count > request.MaxFiles;
        }

        return new SearchResult
        {
            Groups = groups,
            Files = flat,
            TypeCounts = typeCounts,
            MatchedCount = matched.Count,
            DuplicateGroupCount = totalGroupCount,
            DuplicateFileCount = duplicateFileCount,
            TotalWaste = waste,
            Truncated = truncated,
        };
    }

    private static void SortGroups(List<DuplicateGroup> groups, string sort)
    {
        switch (sort)
        {
            case "name":
                groups.Sort((a, b) =>
                {
                    var c = TrComparer.Compare(a.Name, b.Name);
                    return c != 0 ? c : string.Compare(a.Files[0].Directory, b.Files[0].Directory, StringComparison.OrdinalIgnoreCase);
                });
                break;
            case "size":
                groups.Sort((a, b) =>
                {
                    var c = b.Waste.CompareTo(a.Waste);
                    return c != 0 ? c : TrComparer.Compare(a.Name, b.Name);
                });
                break;
            case "path":
                groups.Sort((a, b) => string.Compare(a.Files[0].Directory, b.Files[0].Directory, StringComparison.OrdinalIgnoreCase));
                break;
            default:
                groups.Sort((a, b) =>
                {
                    var c = b.Waste.CompareTo(a.Waste);
                    return c != 0 ? c : TrComparer.Compare(a.Name, b.Name);
                });
                break;
        }
    }

    private static IEnumerable<FileItem> SortFiles(IEnumerable<FileItem> files, string sort)
    {
        return sort switch
        {
            "name" => files.OrderBy(f => f.Name, TrComparer).ThenBy(f => f.Directory, StringComparer.OrdinalIgnoreCase),
            "path" => files.OrderBy(f => f.Directory, StringComparer.OrdinalIgnoreCase).ThenBy(f => f.Name, TrComparer),
            _ => files.OrderByDescending(f => f.Size).ThenBy(f => f.Name, TrComparer),
        };
    }
}
