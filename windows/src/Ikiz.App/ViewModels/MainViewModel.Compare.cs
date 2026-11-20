using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Ikiz.Core;

namespace Ikiz.App.ViewModels;

public sealed class CompareRowVm : BindableBase
{
    private bool _isSelected;

    public event EventHandler? SelectionChanged;

    public required FileItem File { get; init; }
    public required string ExactBytesText { get; init; }
    public required string ModifiedText { get; init; }
    public required string ShortHash { get; init; }
    public required string FullHash { get; init; }
    public required string ContentGroupLabel { get; init; }
    public required string PreviewText { get; init; }
    public required string AgeBadge { get; init; }
    public required bool HasAgeBadge { get; init; }
    public required bool IsOldest { get; init; }
    public required bool IsNewest { get; init; }
    public required DateTime ModifiedUtc { get; init; }

    public string Name => File.Name;
    public string DirectoryPath => File.Directory;
    public string ExtLabel => File.ExtLabel;
    public string Color => FileTypeStyle.Color(File.Extension);

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!Set(ref _isSelected, value)) return;
            Raise(nameof(StatusBadge));
            Raise(nameof(IsKept));
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsKept => !IsSelected;
    public string StatusBadge => IsSelected ? "Silinecek / Panoya" : "Korunacak";
}

public sealed partial class MainViewModel
{
    private bool _compareOpen;
    public bool CompareOpen
    {
        get => _compareOpen;
        private set => Set(ref _compareOpen, value);
    }

    private string _compareTitle = "Dosya karşılaştırma";
    public string CompareTitle
    {
        get => _compareTitle;
        private set => Set(ref _compareTitle, value);
    }

    private string _compareVerdictTitle = "";
    public string CompareVerdictTitle
    {
        get => _compareVerdictTitle;
        private set => Set(ref _compareVerdictTitle, value);
    }

    private string _compareVerdictDetail = "";
    public string CompareVerdictDetail
    {
        get => _compareVerdictDetail;
        private set => Set(ref _compareVerdictDetail, value);
    }

    private bool _compareAllIdentical;
    public bool CompareAllIdentical
    {
        get => _compareAllIdentical;
        private set
        {
            if (!Set(ref _compareAllIdentical, value)) return;
            Raise(nameof(CompareHasDifference));
        }
    }

    public bool CompareHasDifference => !CompareAllIdentical;

    public ObservableCollection<CompareRowVm> CompareRows { get; } = new();

    public int CompareSelectedCount => CompareRows.Count(r => r.IsSelected);
    public bool CanActOnCompared => CompareSelectedCount > 0;
    public string CompareSelectedSummary => CompareSelectedCount == 0
        ? "İşlem için dosya seçilmedi (tümü korunuyor)"
        : $"{CompareSelectedCount} öğe işlem için seçili · {CompareRows.Count - CompareSelectedCount} öğe korunacak";

    public bool CanComparePicked => _picked.Count >= 2;

    // ---------------------------------------------------------------- dosya konumunu aç

    public void OpenFileLocation(FileItem? file)
    {
        if (file is null || string.IsNullOrWhiteSpace(file.FullPath)) return;

        try
        {
            var full = Path.GetFullPath(file.FullPath);
            if (File.Exists(full) || Directory.Exists(full))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{full}\"",
                    UseShellExecute = true,
                });
                return;
            }

            var parent = Path.GetDirectoryName(full);
            if (!string.IsNullOrWhiteSpace(parent) && Directory.Exists(parent))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{parent}\"",
                    UseShellExecute = true,
                });
                return;
            }

            Toast("Dosya veya klasör diskte bulunamadı.", error: true);
        }
        catch (Exception ex)
        {
            AppLog.Write("open-location", ex);
            Toast("Klasör açılamadı: " + ex.Message, error: true);
        }
    }

    public void OpenContainingFolder(FileItem? file)
    {
        if (file is null) return;
        try
        {
            var folder = file.IsDirectory && Directory.Exists(file.FullPath)
                ? Path.GetFullPath(file.FullPath)
                : Path.GetFullPath(file.Directory);

            if (!Directory.Exists(folder))
            {
                Toast("Klasör diskte bulunamadı.", error: true);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{folder}\"",
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            AppLog.Write("open-folder", ex);
            Toast("Klasör açılamadı: " + ex.Message, error: true);
        }
    }

    // ---------------------------------------------------------------- karşılaştırma ekranı

    public void OpenCompareForPicked()
    {
        var picked = PickedFiles();
        if (picked.Count >= 2)
        {
            OpenCompare(picked);
            return;
        }

        if (picked.Count == 1)
        {
            OpenCompareForFile(picked[0]);
            return;
        }

        Toast("Karşılaştırmak için en az 2 dosya seç veya bir çift grubuna sağ tıkla.", error: true);
    }

    public void OpenCompareForFile(FileItem? file)
    {
        if (file is null) return;

        if (_picked.Count >= 2 && _picked.Contains(file.Id))
        {
            OpenCompare(PickedFiles());
            return;
        }

        var groupFiles = _index.GetByKey(file.DuplicateKey);
        if (groupFiles.Count >= 2)
        {
            OpenCompare(groupFiles);
            return;
        }

        if (_picked.Count >= 2)
        {
            OpenCompare(PickedFiles());
            return;
        }

        OpenCompare(new[] { file });
    }

    public FileItem? FirstFileOfGroup(string key) => _index.GetByKey(key).FirstOrDefault();

    public void OpenCompareForGroup(string key)
    {
        var files = _index.GetByKey(key);
        if (files.Count == 0) return;
        OpenCompare(files);
    }

    public void OpenCompare(IReadOnlyList<FileItem> files)
    {
        if (files.Count == 0) return;

        var report = FileComparer.Compare(files);
        var culture = CultureInfo.GetCultureInfo("tr-TR");

        CompareRows.Clear();
        for (var i = 0; i < report.Entries.Count; i++)
        {
            var entry = report.Entries[i];
            var bytesText = entry.File.IsDirectory
                ? "Boş klasör (0 B)"
                : $"{entry.ExactBytes.ToString("N0", culture)} B ({HumanSize.Format(entry.ExactBytes)})";
            var modText = entry.ModifiedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", culture);
            var ageBadge = entry.IsOldest
                ? "En eski (Asıl)"
                : entry.IsNewest
                    ? "En yeni"
                    : "";

            var row = new CompareRowVm
            {
                File = entry.File,
                ExactBytesText = bytesText,
                ModifiedText = modText,
                ShortHash = "SHA-256: " + entry.ShortHash,
                FullHash = entry.Sha256Hex,
                ContentGroupLabel = entry.ContentGroupLabel,
                PreviewText = entry.PreviewText,
                AgeBadge = ageBadge,
                HasAgeBadge = ageBadge.Length > 0,
                IsOldest = entry.IsOldest,
                IsNewest = entry.IsNewest,
                ModifiedUtc = entry.ModifiedUtc,
                // Varsayılan: tüm içerikler birebir aynıysa ilk/en eski kopya korunur, diğerleri işaretlenir
                IsSelected = report.AllIdenticalContent && i > 0,
            };
            row.SelectionChanged += (_, _) => RaiseCompareSelection();
            CompareRows.Add(row);
        }

        CompareTitle = $"Dosya karşılaştırma ({CompareRows.Count} öğe)";
        CompareVerdictTitle = report.VerdictTitle;
        CompareVerdictDetail = report.VerdictDetail;
        CompareAllIdentical = report.AllIdenticalContent;
        CompareOpen = true;
        RaiseCompareSelection();
    }

    public void CloseCompare() => CompareOpen = false;

    public void CompareKeepOldest()
    {
        if (CompareRows.Count == 0) return;
        var oldest = CompareRows.OrderBy(r => r.ModifiedUtc).ThenBy(r => r.File.FullPath, StringComparer.OrdinalIgnoreCase).First();
        foreach (var row in CompareRows)
            row.IsSelected = !ReferenceEquals(row, oldest);
        RaiseCompareSelection();
    }

    public void CompareKeepNewest()
    {
        if (CompareRows.Count == 0) return;
        var newest = CompareRows.OrderByDescending(r => r.ModifiedUtc).ThenBy(r => r.File.FullPath, StringComparer.OrdinalIgnoreCase).First();
        foreach (var row in CompareRows)
            row.IsSelected = !ReferenceEquals(row, newest);
        RaiseCompareSelection();
    }

    public void CompareSelectAll()
    {
        foreach (var row in CompareRows) row.IsSelected = true;
        RaiseCompareSelection();
    }

    public void CompareSelectNone()
    {
        foreach (var row in CompareRows) row.IsSelected = false;
        RaiseCompareSelection();
    }

    public void StageCompared()
    {
        var selected = CompareRows.Where(r => r.IsSelected).Select(r => r.File).ToList();
        if (selected.Count == 0) return;

        _board.Stage(selected, keepFirstIfNew: false);
        ResetApproval();
        RefreshBoard();
        RefreshBoardTotals();
        DropPicks();
        CompareOpen = false;
        TriggerSearch(immediate: true);
        Toast($"{selected.Count} dosya panoda · Panoyu göster ile inceleyebilirsin");
    }

    public void DeleteCompared()
    {
        var selected = CompareRows.Where(r => r.IsSelected).Select(r => r.File).ToList();
        if (selected.Count == 0) return;

        CompareOpen = false;
        OpenPickedQuickDelete(selected);
    }

    private void RaiseCompareSelection()
    {
        Raise(nameof(CompareSelectedCount));
        Raise(nameof(CanActOnCompared));
        Raise(nameof(CompareSelectedSummary));
    }
}
