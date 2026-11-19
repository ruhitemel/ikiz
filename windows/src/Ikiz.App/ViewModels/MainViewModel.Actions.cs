using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using Ikiz.Core;

namespace Ikiz.App.ViewModels;

public sealed class RenamePreviewVm
{
    public required string OldName { get; init; }
    public required string NewName { get; init; }
}

/// <summary>
/// Sağ tık menüsü, toplu yeniden adlandırma, doğrudan silme ve son silmeyi geri alma.
/// </summary>
public sealed partial class MainViewModel
{
    // ---------------------------------------------------------------- menü yardımcıları

    private List<FileItem> VisibleFiles()
    {
        if (_lastResult is null) return new List<FileItem>();
        return _lastResult.Groups
            .SelectMany(g => g.Files)
            .Concat(_lastResult.Files)
            .DistinctBy(f => f.Id)
            .ToList();
    }

    public int CopyCount(FileItem file) => _index.AliveCountForKey(file.DuplicateKey);

    public bool BoardContains(FileItem file) => _board.Contains(file.Id);

    public bool BoardHasAllCopies(FileItem file)
    {
        var alive = _index.AliveCountForKey(file.DuplicateKey);
        if (alive == 0) return false;
        var staged = _board.Items.Count(f => string.Equals(f.DuplicateKey, file.DuplicateKey, StringComparison.Ordinal));
        return staged >= alive;
    }

    public void StageFile(FileItem file)
    {
        _board.Stage(new[] { file }, keepFirstIfNew: false);
        ResetApproval();
        RefreshBoard();
        RefreshBoardTotals();
        RefreshResultsAfterBoardChange();
        Toast($"{_board.Count} dosya panoda · Panoyu göster ile inceleyebilirsin");
    }

    public void UnstageFile(FileItem file) => Unstage(file.Id);

    public void OpenRenameForGroup(string key) => OpenRenameByKey(key, "group");

    public void OpenRenameForType(string key, string extension) => OpenRenameByKey(key, "type", extension);

    public void OpenQuickDeleteForGroup(string key, string scope = "others")
    {
        var files = _index.GetByKey(key);
        if (files.Count == 0) return;
        OpenQuickDelete(files[0], files.Count > 1 ? scope : "one");
    }

    private void OpenRenameByKey(string key, string scope, string? extension = null)
    {
        var files = _index.GetByKey(key);
        if (files.Count == 0) return;
        var file = extension is null
            ? files[0]
            : files.FirstOrDefault(f => string.Equals(f.Extension, extension, StringComparison.OrdinalIgnoreCase)) ?? files[0];
        OpenRename(file, scope);
    }

    // ---------------------------------------------------------------- yeniden adlandırma

    private FileItem? _renameFile;
    private string _renameScopeKey = "one";
    private string _renameName = "";
    private string _renameError = "";
    private bool _renameApproved;
    private bool _renaming;
    private List<RenamePlanItem> _renamePlan = new();
    private List<FileItem> _renameOne = new();
    private List<FileItem> _renameGroup = new();
    private List<FileItem> _renameType = new();
    private List<FileItem> _renameAll = new();
    private List<FileItem> _renamePicked = new();

    public ObservableCollection<RenamePreviewVm> RenamePreviewRows { get; } = new();

    private bool _renameOpen;
    public bool RenameOpen
    {
        get => _renameOpen;
        private set => Set(ref _renameOpen, value);
    }

    public string RenameSub => _renameFile?.FullPath ?? "";
    public bool RenameHasError => _renameError.Length > 0;
    public string RenameError => _renameError;
    public string RenameInputLabel => KeepRenameExtension ? "Yeni ad · uzantı korunur" : "Yeni ad · uzantı dahil";
    public string RenameExtensionHint => _renameFile is null
        ? ""
        : KeepRenameExtension
            ? "." + _renameFile.Extension.ToUpperInvariant()
            : "tam ad";

    public string RenameName
    {
        get => _renameName;
        set
        {
            if (!Set(ref _renameName, value)) return;
            RecomputeRename();
        }
    }

    public bool RenameApproved
    {
        get => _renameApproved;
        set
        {
            if (!Set(ref _renameApproved, value)) return;
            Raise(nameof(CanApplyRename));
        }
    }

    public bool RenameNeedsApproval => RenameTargets().Count > 1;
    public bool CanApplyRename =>
        RenameOpen && !_renaming && _renameFile is not null && _renameError.Length == 0 &&
        _renamePlan.Count > 0 && (!RenameNeedsApproval || _renameApproved);

    public string RenameSummary => _renamePlan.Count switch
    {
        0 => "Değişiklik yok",
        1 => "1 dosya yeniden adlandırılacak",
        _ => $"{_renamePlan.Count} dosya yeniden adlandırılacak",
    };

    public string RenamePreviewMore { get; private set; } = "";
    public bool RenamePreviewEmpty => _renamePlan.Count == 0;

    public string RenameGroupLabel => $"Bu adın tüm kopyaları ({_renameGroup.Count})";
    public string RenameTypeLabel => _renameFile is null
        ? "Bu türdekiler"
        : $"{_renameFile.ExtLabel} türündeki tüm dosyalar ({_renameType.Count})";
    public string RenameAllLabel => $"Tüm sonuçlar ({_renameAll.Count})";
    public string RenamePickedLabel => $"Seçilenler ({_renamePicked.Count})";

    public bool RenameCanGroup => _renameGroup.Count > 1;
    public bool RenameCanType => _renameType.Count > 1;
    public bool RenameCanAll => _renameAll.Count > 1;
    public bool RenameCanPicked => _renamePicked.Count > 1;

    private bool KeepRenameExtension => _renameScopeKey != "one";

    public bool RenameScopeOne
    {
        get => _renameScopeKey == "one";
        set { if (value) SetRenameScope("one"); }
    }

    public bool RenameScopeGroup
    {
        get => _renameScopeKey == "group";
        set { if (value) SetRenameScope("group"); }
    }

    public bool RenameScopeType
    {
        get => _renameScopeKey == "type";
        set { if (value) SetRenameScope("type"); }
    }

    public bool RenameScopeAll
    {
        get => _renameScopeKey == "all";
        set { if (value) SetRenameScope("all"); }
    }

    public bool RenameScopePicked
    {
        get => _renameScopeKey == "picked";
        set { if (value) SetRenameScope("picked"); }
    }

    private List<FileItem> RenameTargets() => _renameScopeKey switch
    {
        "group" => _renameGroup,
        "type" => _renameType,
        "all" => _renameAll,
        "picked" => _renamePicked,
        _ => _renameOne,
    };

    public void OpenRename(FileItem file, string scope = "one")
    {
        _renameFile = file;
        _renameOne = new List<FileItem> { file };
        _renameGroup = _index.GetByKey(file.DuplicateKey).ToList();
        if (_renameGroup.Count == 0) _renameGroup = new List<FileItem> { file };

        var visible = VisibleFiles();
        _renameType = visible
            .Where(f => string.Equals(f.Extension, file.Extension, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (_renameType.Count == 0) _renameType = new List<FileItem> { file };
        _renameAll = visible.Count > 0 ? visible : new List<FileItem> { file };

        if (scope == "group" && !RenameCanGroup) scope = "one";
        if (scope == "type" && !RenameCanType) scope = "one";
        if (scope == "all" && !RenameCanAll) scope = "one";
        if (scope == "picked" && !RenameCanPicked) scope = "one";

        _renameScopeKey = scope;
        _renameApproved = false;
        _renaming = false;
        _renameName = scope == "one"
            ? file.Name
            : scope == "group"
                ? Path.GetFileNameWithoutExtension(file.Name)
                : "";

        RenameOpen = true;
        RaiseScopeProps();
        RecomputeRename();
    }

    public void OpenPickedRename(IReadOnlyList<FileItem> files)
    {
        if (files.Count == 0) return;
        var first = files[0];
        _renamePicked = files.ToList();
        OpenRename(first, "picked");
        _renamePicked = files.ToList();
        RaiseScopeProps();
        RecomputeRename();
    }

    public void CloseRename()
    {
        RenameOpen = false;
        _renameFile = null;
        _renamePlan = new();
        RenamePreviewRows.Clear();
        _renameError = "";
        Raise(nameof(RenameHasError));
        Raise(nameof(RenameError));
        Raise(nameof(CanApplyRename));
    }

    public void SetRenameScope(string key)
    {
        if (_renameFile is null || _renameScopeKey == key) return;
        if (key == "group" && !RenameCanGroup) return;
        if (key == "type" && !RenameCanType) return;
        if (key == "all" && !RenameCanAll) return;
        if (key == "picked" && !RenameCanPicked) return;

        _renameScopeKey = key;
        _renameApproved = false;
        _renameName = key == "one"
            ? _renameFile.Name
            : key == "group"
                ? Path.GetFileNameWithoutExtension(_renameFile.Name)
                : "";

        RaiseScopeProps();
        RecomputeRename();
    }

    private void RaiseScopeProps()
    {
        Raise(nameof(RenameScopeOne));
        Raise(nameof(RenameScopeGroup));
        Raise(nameof(RenameScopeType));
        Raise(nameof(RenameScopeAll));
        Raise(nameof(RenameScopePicked));
        Raise(nameof(RenameGroupLabel));
        Raise(nameof(RenameTypeLabel));
        Raise(nameof(RenameAllLabel));
        Raise(nameof(RenamePickedLabel));
        Raise(nameof(RenameCanGroup));
        Raise(nameof(RenameCanType));
        Raise(nameof(RenameCanAll));
        Raise(nameof(RenameCanPicked));
        Raise(nameof(RenameInputLabel));
        Raise(nameof(RenameExtensionHint));
        Raise(nameof(RenameSub));
        Raise(nameof(RenameNeedsApproval));
        Raise(nameof(RenameApproved));
    }

    private void RecomputeRename()
    {
        var targets = RenameTargets();
        var error = _renameFile is null
            ? "Dosya seçilmedi."
            : KeepRenameExtension
                ? FileRenamer.ValidateBaseName(_renameName)
                : FileRenamer.ValidateFullName(_renameName);

        _renameError = error ?? "";
        _renamePlan = error is null
            ? FileRenamer.BuildPlan(targets, _renameName, KeepRenameExtension)
            : new List<RenamePlanItem>();

        RenamePreviewRows.Clear();
        foreach (var item in _renamePlan.Take(5))
        {
            RenamePreviewRows.Add(new RenamePreviewVm
            {
                OldName = item.File.Name,
                NewName = Path.GetFileName(item.NewFullPath),
            });
        }

        RenamePreviewMore = _renamePlan.Count > 5 ? $"… ve {_renamePlan.Count - 5} dosya daha" : "";

        Raise(nameof(RenameHasError));
        Raise(nameof(RenameError));
        Raise(nameof(RenameNeedsApproval));
        Raise(nameof(CanApplyRename));
        Raise(nameof(RenameSummary));
        Raise(nameof(RenamePreviewMore));
        Raise(nameof(RenamePreviewEmpty));
        Raise(nameof(RenameInputLabel));
        Raise(nameof(RenameExtensionHint));
    }

    public async Task ApplyRenameAsync()
    {
        if (!CanApplyRename) return;

        var plan = _renamePlan.ToList();
        _renaming = true;
        Raise(nameof(CanApplyRename));

        RenameOutcome outcome;
        try
        {
            outcome = await Task.Run(() => FileRenamer.Apply(plan));
        }
        catch (Exception ex)
        {
            _renaming = false;
            Raise(nameof(CanApplyRename));
            AppLog.Write("rename", ex);
            Toast("Yeniden adlandırma hatası: " + ex.Message, error: true);
            return;
        }

        _renaming = false;

        var renamed = new Dictionary<int, string>();
        foreach (var item in outcome.Results)
        {
            if (item.Ok) renamed[item.Id] = item.NewPath;
        }

        var updated = _index.ApplyRenames(renamed);
        if (updated.Count > 0)
        {
            foreach (var file in updated) _board.Replace(file.Id, file);
            _board.ResetApproval();
            if (_approved)
            {
                _approved = false;
                Raise(nameof(Approved));
            }

            RefreshBoard();
            RefreshBoardTotals();
            IndexStatus = $"İndeks hazır · {HumanSize.Number(_index.AliveCount)} dosya";
            DropPicks();
            TriggerSearch(immediate: true);
        }

        CloseRename();

        if (outcome.FailedCount > 0)
            Toast($"{outcome.SuccessCount} dosya yeniden adlandırıldı, {outcome.FailedCount} dosya başarısız", error: true);
        else
            Toast($"{outcome.SuccessCount} dosya yeniden adlandırıldı");
    }

    // ---------------------------------------------------------------- doğrudan silme

    private FileItem? _quickFile;
    private List<FileItem> _quickOne = new();
    private List<FileItem> _quickGroup = new();
    private List<FileItem> _quickPicked = new();
    private DeleteMode _quickMode = DeleteMode.RecycleBin;
    private string _quickScopeKey = "one";
    private bool _quickApproved;
    private bool _quickDeleting;

    private bool _deleteOpen;
    public bool DeleteOpen
    {
        get => _deleteOpen;
        private set => Set(ref _deleteOpen, value);
    }

    public string QuickDeleteSub => _quickFile?.FullPath ?? "";
    public string QuickDeleteOneLabel => "Yalnızca bu dosya";
    public string QuickDeleteOthersLabel => $"Diğer kopyalar ({Math.Max(0, _quickGroup.Count - 1)}) · bu dosya kalsın";
    public string QuickDeleteCopiesLabel => $"Tüm kopyalar ({_quickGroup.Count}) · diskte hiç kalmaz";
    public string QuickDeletePickedLabel => $"Seçilenler ({_quickPicked.Count})";
    public bool QuickDeleteCanCopies => _quickGroup.Count > 1;
    public bool QuickDeleteCanPicked => _quickPicked.Count > 0;

    public bool QuickDeleteScopeOne
    {
        get => _quickScopeKey == "one";
        set { if (value) SetQuickDeleteScope("one"); }
    }

    public bool QuickDeleteScopeOthers
    {
        get => _quickScopeKey == "others";
        set { if (value) SetQuickDeleteScope("others"); }
    }

    public bool QuickDeleteScopeCopies
    {
        get => _quickScopeKey == "all";
        set { if (value) SetQuickDeleteScope("all"); }
    }

    public bool QuickDeleteScopePicked
    {
        get => _quickScopeKey == "picked";
        set { if (value) SetQuickDeleteScope("picked"); }
    }

    public bool QuickDeleteRecycle
    {
        get => _quickMode == DeleteMode.RecycleBin;
        set { if (value) SetQuickDeleteMode(DeleteMode.RecycleBin); }
    }

    public bool QuickDeletePermanent
    {
        get => _quickMode == DeleteMode.Permanent;
        set { if (value) SetQuickDeleteMode(DeleteMode.Permanent); }
    }

    public bool QuickDeleteApproved
    {
        get => _quickApproved;
        set
        {
            if (!Set(ref _quickApproved, value)) return;
            Raise(nameof(CanConfirmQuickDelete));
        }
    }

    public bool CanConfirmQuickDelete =>
        _quickApproved && !_quickDeleting && QuickDeleteTargets().Count > 0;

    public string QuickDeleteTitle => _quickScopeKey switch
    {
        "others" => "Kopyaları sil",
        "all" => "Her kopyayı sil",
        "picked" => "Seçilenleri sil",
        _ => "Dosyayı sil",
    };

    public string QuickDeleteSummary
    {
        get
        {
            var targets = QuickDeleteTargets();
            var bytes = targets.Sum(f => f.Size);
            var action = _quickMode == DeleteMode.Permanent
                ? "kalıcı olarak silinecek"
                : "geri dönüşüm kutusuna taşınacak";
            var keep = _quickScopeKey == "others" ? " · bu dosya kalacak" : "";
            return $"{targets.Count} dosya · {HumanSize.Format(bytes)} · {action}{keep}";
        }
    }

    public bool QuickDeleteHasWarning => _quickScopeKey == "all" && _quickGroup.Count > 1;
    public string QuickDeleteWarning =>
        "Bu dosyanın tüm kopyaları silinecek. Diskte hiçbir kopya kalmayacak.";
    public string QuickDeleteButtonText => _quickMode == DeleteMode.Permanent
        ? (_quickScopeKey == "one" ? "Kalıcı olarak sil" : $"{QuickDeleteTargets().Count} dosyayı kalıcı sil")
        : (_quickScopeKey == "one" ? "Geri dönüşüm kutusuna taşı" : $"{QuickDeleteTargets().Count} dosyayı geri dönüşüm kutusuna taşı");

    private List<FileItem> QuickDeleteTargets() => _quickScopeKey switch
    {
        "others" => _quickGroup
            .Where(f => _quickFile is null || f.Id != _quickFile.Id)
            .ToList(),
        "all" => _quickGroup,
        "picked" => _quickPicked,
        _ => _quickOne,
    };

    public void OpenPickedQuickDelete(IReadOnlyList<FileItem> files)
    {
        if (files.Count == 0) return;
        _quickPicked = files.ToList();
        OpenQuickDelete(files[0], "picked");
        _quickPicked = files.ToList();
        RaiseQuickDeleteProps();
    }

    public void OpenQuickDelete(FileItem file, string scope = "one")
    {
        _quickFile = file;
        _quickOne = new List<FileItem> { file };
        _quickGroup = _index.GetByKey(file.DuplicateKey).ToList();
        if (_quickGroup.Count == 0) _quickGroup = new List<FileItem> { file };

        if ((scope == "others" || scope == "all") && _quickGroup.Count <= 1)
            scope = "one";

        _quickScopeKey = scope;
        _quickMode = DeleteMode.RecycleBin;
        _quickApproved = false;
        _quickDeleting = false;
        DeleteOpen = true;
        RaiseQuickDeleteProps();
    }

    public void CloseQuickDelete()
    {
        DeleteOpen = false;
        _quickFile = null;
        _quickScopeKey = "one";
        _quickApproved = false;
        Raise(nameof(CanConfirmQuickDelete));
    }

    private void SetQuickDeleteScope(string key)
    {
        if (_quickScopeKey == key) return;
        if ((key == "others" || key == "all") && !QuickDeleteCanCopies) return;
        if (key == "picked" && !QuickDeleteCanPicked) return;
        _quickScopeKey = key;
        _quickApproved = false;
        RaiseQuickDeleteProps();
    }

    private void SetQuickDeleteMode(DeleteMode mode)
    {
        if (_quickMode == mode) return;
        _quickMode = mode;
        _quickApproved = false;
        RaiseQuickDeleteProps();
    }

    private void RaiseQuickDeleteProps()
    {
        Raise(nameof(QuickDeleteScopeOne));
        Raise(nameof(QuickDeleteScopeOthers));
        Raise(nameof(QuickDeleteScopeCopies));
        Raise(nameof(QuickDeleteScopePicked));
        Raise(nameof(QuickDeleteCanPicked));
        Raise(nameof(QuickDeletePickedLabel));
        Raise(nameof(QuickDeleteRecycle));
        Raise(nameof(QuickDeletePermanent));
        Raise(nameof(QuickDeleteApproved));
        Raise(nameof(QuickDeleteCanCopies));
        Raise(nameof(QuickDeleteCopiesLabel));
        Raise(nameof(QuickDeleteOthersLabel));
        Raise(nameof(QuickDeleteOneLabel));
        Raise(nameof(QuickDeleteTitle));
        Raise(nameof(QuickDeleteSummary));
        Raise(nameof(QuickDeleteHasWarning));
        Raise(nameof(QuickDeleteWarning));
        Raise(nameof(QuickDeleteButtonText));
        Raise(nameof(QuickDeleteSub));
        Raise(nameof(CanConfirmQuickDelete));
    }

    public async Task ConfirmQuickDeleteAsync()
    {
        if (!CanConfirmQuickDelete) return;

        var targets = QuickDeleteTargets().ToList();
        var mode = _quickMode;
        var bytes = targets.Sum(f => f.Size);

        _quickDeleting = true;
        Raise(nameof(CanConfirmQuickDelete));

        DeleteOutcome outcome;
        try
        {
            outcome = await Task.Run(() => FileDeleter.Delete(targets, mode));
        }
        catch (Exception ex)
        {
            _quickDeleting = false;
            Raise(nameof(CanConfirmQuickDelete));
            AppLog.Write("quick-delete", ex);
            Toast("Silme hatası: " + ex.Message, error: true);
            return;
        }

        _quickDeleting = false;

        foreach (var path in outcome.Deleted)
        {
            var item = targets.FirstOrDefault(f => string.Equals(f.FullPath, path, StringComparison.OrdinalIgnoreCase));
            if (item is null) continue;
            _index.Remove(item.Id);
            _board.Unstage(item.Id);
        }

        if (outcome.Deleted.Count > 0)
        {
            _sessionFreed += outcome.FreedBytes;
            Raise(nameof(SessionFreedText));
            IndexStatus = $"İndeks hazır · {HumanSize.Number(_index.AliveCount)} dosya";
        }

        RegisterUndo(targets, outcome, mode);
        DeleteOpen = false;
        _quickFile = null;
        ResetApproval();
        RefreshBoard();
        RefreshBoardTotals();
        DropPicks();
        TriggerSearch(immediate: true);

        var action = mode == DeleteMode.Permanent ? "kalıcı olarak silindi" : "geri dönüşüm kutusuna taşındı";
        var canUndo = mode == DeleteMode.RecycleBin && outcome.Deleted.Count > 0;
        if (outcome.Failed.Count > 0)
            Toast($"{outcome.Deleted.Count} dosya {action}, {outcome.Failed.Count} dosya başarısız · {HumanSize.Format(bytes)}",
                error: true, undoable: canUndo);
        else
            Toast($"{outcome.Deleted.Count} dosya {action} · {HumanSize.Format(bytes)}", undoable: canUndo);
    }

    // ---------------------------------------------------------------- geri al

    private List<FileItem> _undoItems = new();
    private bool _undoing;

    public bool CanUndoDelete => _undoItems.Count > 0 && !_undoing;
    public string UndoMenuText => _undoItems.Count switch
    {
        0 => "Son silmeyi geri al",
        1 => "Son silmeyi geri al (1 dosya)",
        _ => $"Son silmeyi geri al ({_undoItems.Count} dosya)",
    };

    private void RegisterUndo(IReadOnlyList<FileItem> plan, DeleteOutcome outcome, DeleteMode mode)
    {
        if (mode != DeleteMode.RecycleBin || outcome.Deleted.Count == 0)
        {
            _undoItems.Clear();
        }
        else
        {
            var deleted = new HashSet<string>(outcome.Deleted, StringComparer.OrdinalIgnoreCase);
            _undoItems = plan.Where(f => deleted.Contains(f.FullPath)).ToList();
        }

        Raise(nameof(CanUndoDelete));
        Raise(nameof(UndoMenuText));
    }

    public async Task UndoLastDeleteAsync()
    {
        if (_undoItems.Count == 0 || _undoing) return;

        var items = _undoItems.ToList();
        _undoItems.Clear();
        _undoing = true;
        ToastCanUndo = false;
        Raise(nameof(CanUndoDelete));
        Raise(nameof(UndoMenuText));

        RestoreOutcome outcome;
        try
        {
            outcome = await Task.Run(() => RecycleBinRestorer.Restore(items.Select(f => f.FullPath).ToList()));
        }
        catch (Exception ex)
        {
            _undoing = false;
            AppLog.Write("undo", ex);
            Raise(nameof(CanUndoDelete));
            Raise(nameof(UndoMenuText));
            Toast("Geri alma hatası: " + ex.Message, error: true);
            return;
        }

        _undoing = false;

        var restored = new HashSet<string>(outcome.Restored, StringComparer.OrdinalIgnoreCase);
        var restoredItems = items.Where(f => restored.Contains(f.FullPath)).ToList();
        if (restoredItems.Count > 0)
        {
            _index.Reinsert(restoredItems);
            _sessionFreed = Math.Max(0, _sessionFreed - restoredItems.Sum(f => f.Size));
            Raise(nameof(SessionFreedText));
            IndexStatus = $"İndeks hazır · {HumanSize.Number(_index.AliveCount)} dosya";
        }

        TriggerSearch(immediate: true);
        Raise(nameof(CanUndoDelete));
        Raise(nameof(UndoMenuText));

        if (outcome.Failed.Count > 0)
            Toast($"{outcome.RestoredCount} dosya geri alındı, {outcome.Failed.Count} dosya başarısız", error: true);
        else
            Toast($"{outcome.RestoredCount} dosya geri alındı");
    }
}
