using Ikiz.Core;

namespace Ikiz.App.ViewModels;

/// <summary>Ana ekrandaki çoklu seçim. Pano seçiminden ayrıdır; işlem sonrası temizlenir.</summary>
public sealed partial class MainViewModel
{
    private readonly HashSet<int> _picked = new();

    public int PickedCount => _picked.Count;
    public bool HasPicked => _picked.Count > 0;
    public string PickedText => _picked.Count == 0 ? "Seçim yok" : $"{HumanSize.Number(_picked.Count)} dosya seçili";

    public bool IsPicked(int id) => _picked.Contains(id);

    public void SetPicked(int id, bool value)
    {
        if (value ? _picked.Add(id) : _picked.Remove(id))
            RaisePicked();
    }

    public void PickAll()
    {
        _picked.Clear();
        foreach (var file in VisibleFiles()) _picked.Add(file.Id);
        RaisePicked();
        TriggerSearch(immediate: true);
    }

    /// <summary>Her gruptan ilk kopyayı korur, fazlalıkları işaretler.</summary>
    public void PickCopies()
    {
        _picked.Clear();
        if (OnlyEmptyItems)
        {
            foreach (var file in VisibleFiles()) _picked.Add(file.Id);
        }
        else if (_lastResult is not null && _lastResult.Groups.Count > 0)
        {
            foreach (var group in _lastResult.Groups)
                foreach (var file in group.Files.Skip(1))
                    _picked.Add(file.Id);
        }
        else
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var file in VisibleFiles())
                if (!seen.Add(file.DuplicateKey)) _picked.Add(file.Id);
        }

        RaisePicked();
        TriggerSearch(immediate: true);
    }

    /// <summary>Boş klasörleri ve 0 KB dosyaları gösterir ve tümünü toplu işlem için seçer.</summary>
    public void PickEmptyItems()
    {
        if (!_onlyEmptyItems || !_groupMode)
        {
            _onlyEmptyItems = true;
            _groupMode = true;
            RaiseTabState();
        }

        var result = _index.Search(BuildRequest());
        _picked.Clear();
        foreach (var group in result.Groups)
            foreach (var f in group.Files)
                _picked.Add(f.Id);
        foreach (var f in result.Files)
            _picked.Add(f.Id);

        RaisePicked();
        ApplyResult(result, TimeSpan.Zero);
    }

    public void ClearPicks()
    {
        if (!DropPicks()) return;
        TriggerSearch(immediate: true);
    }

    /// <summary>Seçimi sessizce temizler; arama yeniden tetiklenmez.</summary>
    private bool DropPicks()
    {
        if (_picked.Count == 0) return false;
        _picked.Clear();
        RaisePicked();
        return true;
    }

    public IReadOnlyList<FileItem> PickedFiles() => _index.Snapshot
        .Where(f => _picked.Contains(f.Id))
        .OrderBy(f => f.Directory, StringComparer.OrdinalIgnoreCase)
        .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
        .ToList();

    public void StagePicked()
    {
        var files = PickedFiles();
        if (files.Count == 0) return;

        _board.Stage(files, keepFirstIfNew: false);
        ResetApproval();
        RefreshBoard();
        RefreshBoardTotals();
        DropPicks();
        TriggerSearch(immediate: true);
        Toast($"{files.Count} dosya panoda · Panoyu göster ile inceleyebilirsin");
    }

    public void DeletePicked()
    {
        var files = PickedFiles();
        if (files.Count == 0) return;
        OpenPickedQuickDelete(files);
    }

    public void RenamePicked()
    {
        var files = PickedFiles().Where(f => !f.IsDirectory).ToList();
        if (files.Count == 0)
        {
            Toast("Klasörler bu menüden adlandırılamaz; yalnızca dosya seç.", error: true);
            return;
        }
        OpenPickedRename(files);
    }

    private void RaisePicked()
    {
        Raise(nameof(PickedCount));
        Raise(nameof(HasPicked));
        Raise(nameof(PickedText));
        Raise(nameof(CanComparePicked));
    }
}
