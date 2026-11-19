using System.Collections.ObjectModel;
using System.Globalization;
using Ikiz.Core;

namespace Ikiz.App.ViewModels;

public sealed partial class MainViewModel
{
    private readonly DeleteBoard _board = new();
    private bool _suppressRowEvents;
    private bool _deleting;

    public ObservableCollection<BoardGroupVm> BoardGroups { get; } = new();

    private bool _boardOpen;
    public bool BoardOpen
    {
        get => _boardOpen;
        set => Set(ref _boardOpen, value);
    }

    private bool _boardEmpty = true;
    public bool BoardEmpty
    {
        get => _boardEmpty;
        private set => Set(ref _boardEmpty, value);
    }

    private string _boardSub = "Boş · sonuçlardan dosya ekle";
    public string BoardSub
    {
        get => _boardSub;
        private set => Set(ref _boardSub, value);
    }

    private string _boardSelectedText = "Seçili dosya yok";
    public string BoardSelectedText
    {
        get => _boardSelectedText;
        private set => Set(ref _boardSelectedText, value);
    }

    private string _boardBytesText = "0 B";
    public string BoardBytesText
    {
        get => _boardBytesText;
        private set => Set(ref _boardBytesText, value);
    }

    private bool _hasWarning;
    public bool HasWarning
    {
        get => _hasWarning;
        private set => Set(ref _hasWarning, value);
    }

    private string _warningText = "";
    public string WarningText
    {
        get => _warningText;
        private set => Set(ref _warningText, value);
    }

    private bool _approved;
    public bool Approved
    {
        get => _approved;
        set
        {
            if (!Set(ref _approved, value)) return;
            _board.Approve(value);
            Raise(nameof(CanConfirm));
        }
    }

    public bool IsDeleting
    {
        get => _deleting;
        private set
        {
            if (!Set(ref _deleting, value)) return;
            Raise(nameof(CanConfirm));
        }
    }

    public int QueueCount => _board.Count;
    public bool HasQueue => _board.Count > 0;
    public int SelectedCount => _board.SelectedCount;
    public bool HasSelected => _board.SelectedCount > 0;
    public bool CanConfirm => _approved && !_deleting && _board.SelectedCount > 0;
    public string ConfirmText =>
        $"{_board.SelectedCount} dosyayı {(_board.Mode == DeleteMode.Permanent ? "kalıcı sil" : "geri dönüşüm kutusuna taşı")}";

    public bool IsPermanent
    {
        get => _board.Mode == DeleteMode.Permanent;
        set
        {
            var target = value ? DeleteMode.Permanent : DeleteMode.RecycleBin;
            if (_board.Mode == target) return;
            _board.Mode = target;
            ResetApproval();
            Raise(nameof(IsPermanent));
            Raise(nameof(IsRecycle));
            Raise(nameof(ConfirmText));
            Raise(nameof(CanConfirm));
        }
    }

    public bool IsRecycle
    {
        get => !IsPermanent;
        set
        {
            if (value) IsPermanent = false;
        }
    }

    public void OpenBoard()
    {
        RefreshBoard();
        RefreshBoardTotals();
        BoardOpen = true;
    }

    public void CloseBoard()
    {
        BoardOpen = false;
        ResetApproval();
    }

    public void StageGroup(string key)
    {
        var files = _index.GetByKey(key);
        if (files.Count == 0) return;
        _board.Stage(files, keepFirstIfNew: true);
        ResetApproval();
        RefreshBoard();
        RefreshBoardTotals();
        RefreshResultsAfterBoardChange();
        Toast($"{_board.Count} dosya panoda · Panoyu göster ile inceleyebilirsin");
    }

    public void StageAll()
    {
        if (_lastResult is null) return;
        foreach (var group in _lastResult.Groups)
        {
            var files = _index.GetByKey(group.Key);
            if (files.Count > 0) _board.Stage(files, keepFirstIfNew: true);
        }

        ResetApproval();
        RefreshBoard();
        RefreshBoardTotals();
        RefreshResultsAfterBoardChange();
        Toast($"{_board.Count} dosya panoda · Panoyu göster ile inceleyebilirsin");
    }

    public void SelectAllBoard()
    {
        _board.SelectAll();
        ResetApproval();
        RefreshBoard();
        RefreshBoardTotals();
    }

    public void SelectNoneBoard()
    {
        _board.SelectNone();
        ResetApproval();
        RefreshBoard();
        RefreshBoardTotals();
    }

    public void ClearBoard()
    {
        _board.Clear();
        ResetApproval();
        RefreshBoard();
        RefreshBoardTotals();
        var hadPicks = _picked.Count > 0;
        ClearPicks();
        if (!hadPicks) RefreshResultsAfterBoardChange();
    }

    public void KeepOne(BoardGroupVm group)
    {
        var alive = _index.GetByKey(group.Key);
        if (alive.Count == 0) return;
        _board.KeepOne(alive[0].Id, group.Rows.Select(r => r.File.Id));
        ResetApproval();
        RefreshBoard();
        RefreshBoardTotals();
        RefreshResultsAfterBoardChange();
    }

    public void Unstage(int id)
    {
        _board.Unstage(id);
        ResetApproval();
        RefreshBoard();
        RefreshBoardTotals();
        RefreshResultsAfterBoardChange();
    }

    private void RefreshResultsAfterBoardChange()
    {
        if (HideStaged) TriggerSearch(immediate: true);
    }

    public void ResetApproval()
    {
        _board.ResetApproval();
        if (_approved)
        {
            _approved = false;
            Raise(nameof(Approved));
        }
        Raise(nameof(CanConfirm));
        Raise(nameof(ConfirmText));
    }

    private void OnBoardRowToggled(BoardRowVm row)
    {
        if (_suppressRowEvents) return;
        _board.SetSelected(row.File.Id, row.IsSelected);
        _board.ResetApproval();
        if (_approved)
        {
            _approved = false;
            Raise(nameof(Approved));
        }
        RefreshBoardTotals();
    }

    private void RefreshBoard()
    {
        _suppressRowEvents = true;
        BoardGroups.Clear();
        var comparer = StringComparer.Create(CultureInfo.GetCultureInfo("tr-TR"), true);

        foreach (var grouping in _board.Items
                     .GroupBy(f => f.DuplicateKey)
                     .OrderBy(g => g.First().Name, comparer))
        {
            var key = grouping.Key;
            var alive = _index.GetByKey(key);
            var aliveCount = alive.Count;
            var rows = grouping
                .OrderBy(f => f.Directory, StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.FullPath, StringComparer.OrdinalIgnoreCase)
                .Select(f => new BoardRowVm
                {
                    File = f,
                    IsSelected = _board.IsSelected(f.Id),
                })
                .ToList();

            foreach (var row in rows)
                row.SelectionChangedByUser += (_, _) => OnBoardRowToggled(row);

            var selectedInBoard = rows.Count(r => r.IsSelected);
            var first = rows[0].File;
            var group = new BoardGroupVm
            {
                Key = key,
                Name = first.Name,
                ExtLabel = first.ExtLabel,
                Color = FileTypeStyle.Color(first.Extension),
                AliveCount = aliveCount,
                InBoard = rows.Count,
                Meta = $"{rows.Count}/{aliveCount} panoda",
                CanKeepOne = aliveCount > 1,
                Rows = rows,
            };
            group.AllSelected = aliveCount > 0 && selectedInBoard >= aliveCount;
            BoardGroups.Add(group);
        }

        _suppressRowEvents = false;
        Raise(nameof(QueueCount));
        Raise(nameof(HasQueue));
    }

    private void RefreshBoardTotals()
    {
        var selected = _board.Selected;
        var selectedBytes = selected.Sum(f => f.Size);
        BoardSelectedText = selected.Count > 0 ? $"Seçilen {selected.Count} dosya" : "Seçili dosya yok";
        BoardBytesText = HumanSize.Format(selectedBytes);
        BoardEmpty = _board.Count == 0;
        BoardSub = _deleting
            ? "Siliniyor…"
            : _board.Count == 0
                ? "Boş · sonuçlardan dosya ekle"
                : $"{HumanSize.Number(_board.Count)} dosya · {BoardGroups.Count} grup · onaylayana kadar silinmez";

        var fullGroups = 0;
        foreach (var group in BoardGroups)
        {
            var alive = _index.AliveCountForKey(group.Key);
            var selectedInGroup = group.Rows.Count(r => r.IsSelected);
            group.AllSelected = alive > 0 && selectedInGroup >= alive;
            if (group.IsFullGroup) fullGroups++;
        }

        HasWarning = fullGroups > 0;
        WarningText = $"{fullGroups} dosyanın tüm kopyaları seçili. Onaylarsan bu dosyalar diskte hiç kalmayacak.";

        Raise(nameof(QueueCount));
        Raise(nameof(HasQueue));
        Raise(nameof(SelectedCount));
        Raise(nameof(HasSelected));
        Raise(nameof(CanConfirm));
        Raise(nameof(ConfirmText));
    }

    public async Task ConfirmDeleteAsync()
    {
        if (!_approved || _deleting) return;
        var plan = _board.Plan();
        if (plan.Count == 0) return;

        IsDeleting = true;
        BoardSub = "Siliniyor…";
        var mode = _board.Mode;
        var bytes = plan.Sum(f => f.Size);

        DeleteOutcome outcome;
        try
        {
            outcome = await Task.Run(() => FileDeleter.Delete(plan, mode));
        }
        catch (Exception ex)
        {
            IsDeleting = false;
            RefreshBoardTotals();
            Toast("Silme hatası: " + ex.Message, error: true);
            return;
        }

        var byPath = plan.ToDictionary(f => f.FullPath, f => f, StringComparer.OrdinalIgnoreCase);
        foreach (var path in outcome.Deleted)
            if (byPath.TryGetValue(path, out var item))
                _index.Remove(item.Id);

        if (outcome.Deleted.Count > 0)
        {
            _sessionFreed += outcome.FreedBytes;
            Raise(nameof(SessionFreedText));
            IndexStatus = $"İndeks hazır · {HumanSize.Number(_index.AliveCount)} dosya";
        }

        RegisterUndo(plan, outcome, mode);

        _board.Clear();
        _board.ResetApproval();
        if (_approved)
        {
            _approved = false;
            Raise(nameof(Approved));
        }

        IsDeleting = false;
        BoardOpen = false;
        RefreshBoard();
        RefreshBoardTotals();
        TriggerSearch(immediate: true);

        var action = mode == DeleteMode.Permanent ? "kalıcı olarak silindi" : "geri dönüşüm kutusuna taşındı";
        var canUndo = mode == DeleteMode.RecycleBin && outcome.Deleted.Count > 0;
        if (outcome.Failed.Count > 0)
        {
            Toast($"{outcome.Deleted.Count} dosya {action}, {outcome.Failed.Count} dosya başarısız · {HumanSize.Format(outcome.FreedBytes)}", error: true, undoable: canUndo);
        }
        else
        {
            Toast($"{outcome.Deleted.Count} dosya {action} · {HumanSize.Format(bytes)} {(mode == DeleteMode.Permanent ? "yer açıldı" : "geri kazanılacak")}", undoable: canUndo);
        }
    }
}
