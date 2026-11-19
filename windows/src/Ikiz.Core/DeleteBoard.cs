namespace Ikiz.Core;

public enum DeleteMode
{
    RecycleBin,
    Permanent,
}

public sealed class DeleteBoard
{
    private readonly Dictionary<int, FileItem> _items = new();
    private readonly HashSet<int> _excluded = new();

    public DeleteMode Mode { get; set; } = DeleteMode.RecycleBin;
    public bool Approved { get; private set; }

    public int Count => _items.Count;
    public int SelectedCount => _items.Count - _excluded.Count;
    public long SelectedBytes => _items.Values.Where(f => !_excluded.Contains(f.Id)).Sum(f => f.Size);
    public IEnumerable<FileItem> Items => _items.Values;

    public IReadOnlyList<FileItem> Selected => _items.Values
        .Where(f => !_excluded.Contains(f.Id))
        .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
        .ThenBy(f => f.FullPath, StringComparer.OrdinalIgnoreCase)
        .ToList();

    public bool Contains(int id) => _items.ContainsKey(id);
    public bool IsSelected(int id) => _items.ContainsKey(id) && !_excluded.Contains(id);
    public IReadOnlyList<FileItem> Plan() => Approved ? Selected : Array.Empty<FileItem>();

    public void Stage(IEnumerable<FileItem> files, bool keepFirstIfNew)
    {
        var list = files.ToList();
        if (list.Count == 0) return;

        var already = list.Any(f => _items.ContainsKey(f.Id));
        foreach (var f in list)
        {
            _items[f.Id] = f;
            if (!already) _excluded.Remove(f.Id);
        }

        if (!already && keepFirstIfNew && list.Count > 1)
        {
            _excluded.Add(list[0].Id);
            foreach (var f in list.Skip(1)) _excluded.Remove(f.Id);
        }

        Approved = false;
    }

    public void SetSelected(int id, bool selected)
    {
        if (!_items.ContainsKey(id)) return;
        if (selected) _excluded.Remove(id);
        else _excluded.Add(id);
        Approved = false;
    }

    public void SelectAll()
    {
        _excluded.Clear();
        Approved = false;
    }

    public void SelectNone()
    {
        foreach (var id in _items.Keys) _excluded.Add(id);
        Approved = false;
    }

    public void KeepOne(int keepId, IEnumerable<int> groupIds)
    {
        foreach (var id in groupIds)
        {
            if (!_items.ContainsKey(id)) continue;
            if (id == keepId) _excluded.Remove(id);
            else _excluded.Add(id);
        }
        Approved = false;
    }

    public void Unstage(int id)
    {
        _items.Remove(id);
        _excluded.Remove(id);
        Approved = false;
    }

    public void Replace(int id, FileItem updated)
    {
        if (!_items.ContainsKey(id)) return;
        _items[id] = updated;
        Approved = false;
    }

    public void Clear()
    {
        _items.Clear();
        _excluded.Clear();
        Mode = DeleteMode.RecycleBin;
        Approved = false;
    }

    public void Approve(bool value) => Approved = value && SelectedCount > 0;
    public void ResetApproval() => Approved = false;
}
