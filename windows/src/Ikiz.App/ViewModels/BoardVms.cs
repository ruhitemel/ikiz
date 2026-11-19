using Ikiz.Core;

namespace Ikiz.App.ViewModels;

public sealed class BoardRowVm : BindableBase
{
    private bool _isSelected;

    public event EventHandler? SelectionChangedByUser;

    public required FileItem File { get; init; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!Set(ref _isSelected, value)) return;
            Raise(nameof(IsKept));
            SelectionChangedByUser?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsKept => !IsSelected;
    public string Name => File.Name;
    public string Path => File.Directory;
    public string SizeText => File.SizeText;
}

public sealed class BoardGroupVm : BindableBase
{
    private bool _allSelected;

    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string ExtLabel { get; init; }
    public required string Color { get; init; }
    public required int AliveCount { get; init; }
    public required int InBoard { get; init; }
    public required string Meta { get; init; }
    public required bool CanKeepOne { get; init; }
    public required IReadOnlyList<BoardRowVm> Rows { get; init; }

    public bool AllSelected
    {
        get => _allSelected;
        set
        {
            if (!Set(ref _allSelected, value)) return;
            Raise(nameof(IsFullGroup));
        }
    }

    public string CopyBadge => AliveCount > 1 ? $"{AliveCount} kopya" : "tek dosya";
    public bool IsFullGroup => AliveCount > 1 && AllSelected;
}
