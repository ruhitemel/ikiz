using System.Collections.ObjectModel;
using System.IO;
using Ikiz.Core;

namespace Ikiz.App.ViewModels;

public sealed partial class MainViewModel
{
    private bool _locationsOpen;
    public bool LocationsOpen
    {
        get => _locationsOpen;
        private set => Set(ref _locationsOpen, value);
    }

    public ObservableCollection<DriveVm> Drives { get; } = new();
    public ObservableCollection<RootVm> RootItems { get; } = new();

    public int SelectedDriveCount => Drives.Count(d => d.IsReady && d.Selected);

    public string SelectionSummary
    {
        get
        {
            var count = SelectedDriveCount;
            return count == 0
                ? "Henüz sürücü seçilmedi."
                : count == 1 ? "1 sürücü seçildi." : $"{count} sürücü seçildi.";
        }
    }

    public void OpenLocations()
    {
        RefreshDrives();
        RefreshRoots();
        LocationsOpen = true;
    }

    public void CloseLocations() => LocationsOpen = false;

    public void RefreshDrives()
    {
        var previous = Drives.Where(d => d.Selected).Select(d => d.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        Drives.Clear();

        foreach (var drive in SafeGetDrives())
        {
            string label;
            string sizeText;
            bool ready;
            try
            {
                ready = drive.IsReady;
                label = ready ? drive.VolumeLabel : "";
                sizeText = ready
                    ? $"{HumanSize.Format(drive.AvailableFreeSpace)} boş / {HumanSize.Format(drive.TotalSize)}"
                    : "Sürücü hazır değil";
            }
            catch
            {
                ready = false;
                label = "";
                sizeText = "Sürücü okunamadı";
            }

            var vm = new DriveVm(drive.Name, label, DriveTypeName(drive.DriveType), sizeText, ready, RaiseSelectionChanged)
            {
                Selected = previous.Contains(drive.Name),
            };
            Drives.Add(vm);
        }

        RaiseSelectionChanged();
    }

    public void RefreshRoots()
    {
        RootItems.Clear();
        foreach (var path in _roots.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            var isDrive = path.Length <= 3 && path.EndsWith(":\\", StringComparison.Ordinal);
            RootItems.Add(new RootVm(path, isDrive ? $"{path}  (tüm sürücü)" : path, isDrive));
        }
        RootsText = _roots.Count == 0 ? "Konum yok" : $"{_roots.Count} konum";
    }

    public void ToggleDrive(string path)
    {
        var drive = Drives.FirstOrDefault(d => string.Equals(d.Path, path, StringComparison.OrdinalIgnoreCase));
        if (drive is { IsReady: true }) drive.Selected = !drive.Selected;
    }

    private void RaiseSelectionChanged()
    {
        Raise(nameof(SelectedDriveCount));
        Raise(nameof(SelectionSummary));
    }

    /// <summary>Seçili sürücüleri kök olarak ekler ve indeksi bir kez yeniler.</summary>
    public async Task AddSelectedDrivesAsync()
    {
        var paths = Drives.Where(d => d.IsReady && d.Selected).Select(d => d.Path).ToList();
        if (paths.Count == 0)
        {
            Toast("Önce bir sürücü seç.", error: true);
            return;
        }

        await AddLocationsAsync(paths);
        CloseLocations();
    }

    /// <summary>Bir klasör veya sürücü kökü ekleyip indeksi yeniler.</summary>
    public async Task AddRootAsync(string path)
    {
        await AddLocationsAsync(new[] { path });
    }

    /// <summary>Birden fazla konumu tek taramada ekler; kapsanan kökleri sadeleştirir.</summary>
    public async Task AddLocationsAsync(IEnumerable<string> paths)
    {
        var changed = false;
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path)) continue;
            string full;
            try
            {
                full = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (full.Length == 2 && full[1] == ':') full += Path.DirectorySeparatorChar;
            }
            catch
            {
                continue;
            }

            if (!Directory.Exists(full)) continue;

            if (!_roots.Any(r => IsUnder(full, r)))
            {
                _roots.RemoveAll(r => IsUnder(r, full));
                _roots.Add(full);
                changed = true;
            }
        }

        if (!changed)
        {
            Toast("Bu konumlar zaten taranıyor.");
            RefreshRoots();
            return;
        }

        RefreshRoots();
        await ScanAsync();
        CloseLocations();
    }

    public void RemoveRoot(string path)
    {
        if (_roots.RemoveAll(r => string.Equals(r, path, StringComparison.OrdinalIgnoreCase)) == 0) return;
        RefreshRoots();
        Toast("Konum kaldırıldı. Değişikliğin indekse yansıması için yeniden indeksle.");
    }

    private static IEnumerable<DriveInfo> SafeGetDrives()
    {
        try
        {
            return DriveInfo.GetDrives();
        }
        catch
        {
            return Array.Empty<DriveInfo>();
        }
    }

    private static string DriveTypeName(DriveType type) => type switch
    {
        DriveType.Fixed => "Yerel disk",
        DriveType.Removable => "Çıkarılabilir disk",
        DriveType.CDRom => "Optik sürücü",
        DriveType.Network => "Ağ sürücüsü",
        DriveType.Ram => "RAM diski",
        _ => "Sürücü",
    };
}
