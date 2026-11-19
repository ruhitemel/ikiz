using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;
using Ikiz.Core;

namespace Ikiz.App.ViewModels;

public sealed partial class MainViewModel : BindableBase
{
    private readonly FileIndex _index = new();
    private readonly List<string> _roots = new();
    private readonly HashSet<string> _collapsed = new(StringComparer.Ordinal);
    private readonly DispatcherTimer _toastTimer;
    private CancellationTokenSource? _searchCts;
    private CancellationTokenSource? _scanCts;
    private SearchResult? _lastResult;
    private TypeChipVm? _selectedType;
    private bool _updatingTypes;
    private long _sessionFreed;

    public ObservableCollection<ResultRowVm> Rows { get; } = new();
    public ObservableCollection<TypeChipVm> Types { get; } = new();
    public ObservableCollection<string> SortOptions { get; } = new()
    {
        "Kazanılabilir alan", "Ad (A→Z)", "Boyut (büyük→küçük)", "Yol",
    };

    public MainViewModel()
    {
        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3.6) };
        _toastTimer.Tick += (_, _) =>
        {
            _toastTimer.Stop();
            ToastVisible = false;
            ToastCanUndo = false;
        };
    }

    public TypeChipVm? SelectedType
    {
        get => _selectedType;
        set
        {
            if (ReferenceEquals(_selectedType, value)) return;
            _selectedType = value;
            Raise();
            if (!_updatingTypes && value is not null) TriggerSearch(immediate: true);
        }
    }

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (Set(ref _searchText, value)) TriggerSearch();
        }
    }

    private bool _onlyDuplicates = true;
    public bool OnlyDuplicates
    {
        get => _onlyDuplicates;
        set
        {
            if (Set(ref _onlyDuplicates, value)) TriggerSearch();
        }
    }

    private bool _groupMode = true;
    public bool GroupMode
    {
        get => _groupMode;
        set
        {
            if (Set(ref _groupMode, value)) TriggerSearch();
        }
    }

    private bool _searchPath;
    public bool SearchPath
    {
        get => _searchPath;
        set
        {
            if (Set(ref _searchPath, value)) TriggerSearch();
        }
    }

    private string _selectedSort = "Kazanılabilir alan";
    public string SelectedSort
    {
        get => _selectedSort;
        set
        {
            if (Set(ref _selectedSort, value)) TriggerSearch();
        }
    }

    private string SortKey => _selectedSort switch
    {
        "Ad (A→Z)" => "name",
        "Boyut (büyük→küçük)" => "size",
        "Yol" => "path",
        _ => "waste",
    };

    private string _resultSummary = "0 dosya · 0 çift grup";
    public string ResultSummary
    {
        get => _resultSummary;
        private set => Set(ref _resultSummary, value);
    }

    private string _elapsedText = "";
    public string ElapsedText
    {
        get => _elapsedText;
        private set => Set(ref _elapsedText, value);
    }

    private string _sectionTitle = "Çift kopyalar";
    public string SectionTitle
    {
        get => _sectionTitle;
        private set => Set(ref _sectionTitle, value);
    }

    private string _sectionMeta = "";
    public string SectionMeta
    {
        get => _sectionMeta;
        private set => Set(ref _sectionMeta, value);
    }

    private bool _hasResults;
    public bool HasResults
    {
        get => _hasResults;
        private set => Set(ref _hasResults, value);
    }

    private bool _isEmpty = true;
    public bool IsEmpty
    {
        get => _isEmpty;
        private set => Set(ref _isEmpty, value);
    }

    private string _emptyTitle = "Henüz indeks yok";
    public string EmptyTitle
    {
        get => _emptyTitle;
        private set => Set(ref _emptyTitle, value);
    }

    private string _emptyHint = "Klasör ekleyip indeksi oluştur.";
    public string EmptyHint
    {
        get => _emptyHint;
        private set => Set(ref _emptyHint, value);
    }

    private bool _canStageAll;
    public bool CanStageAll
    {
        get => _canStageAll;
        private set => Set(ref _canStageAll, value);
    }

    private bool _isScanning;
    public bool IsScanning
    {
        get => _isScanning;
        private set => Set(ref _isScanning, value);
    }

    private string _indexStatus = "İndeks hazır değil";
    public string IndexStatus
    {
        get => _indexStatus;
        private set => Set(ref _indexStatus, value);
    }

    private string _scanText = "";
    public string ScanText
    {
        get => _scanText;
        private set => Set(ref _scanText, value);
    }

    private string _rootsText = "Konum yok";
    public string RootsText
    {
        get => _rootsText;
        private set => Set(ref _rootsText, value);
    }

    public string SessionFreedText => $"Bu oturumda kazanılan: {HumanSize.Format(_sessionFreed)}";

    private string _toastText = "";
    public string ToastText
    {
        get => _toastText;
        private set => Set(ref _toastText, value);
    }

    private bool _toastVisible;
    public bool ToastVisible
    {
        get => _toastVisible;
        private set => Set(ref _toastVisible, value);
    }

    private bool _toastCanUndo;
    public bool ToastCanUndo
    {
        get => _toastCanUndo;
        internal set => Set(ref _toastCanUndo, value);
    }

    private bool _toastError;
    public bool ToastError
    {
        get => _toastError;
        private set => Set(ref _toastError, value);
    }

    public async Task StartAsync()
    {
        if (_roots.Count == 0) _roots.AddRange(DefaultRoots());
        await ScanAsync();
    }

    public void Reindex() => _ = ScanAsync();

    public void Stop()
    {
        _scanCts?.Cancel();
        _searchCts?.Cancel();
        _toastTimer.Stop();
    }

    private async Task ScanAsync()
    {
        _scanCts?.Cancel();
        var cts = new CancellationTokenSource();
        _scanCts = cts;

        IsScanning = true;
        ScanText = "Taranıyor…";
        IndexStatus = "İndeksleniyor…";
        Rows.Clear();
        HasResults = false;
        IsEmpty = true;
        EmptyTitle = "Dosyalar taranıyor…";
        EmptyHint = "İlk indeks oluşturuluyor, bu birkaç saniye sürebilir.";
        ResultSummary = "Taranıyor…";
        ElapsedText = "";
        RefreshRoots();

        var progress = new Progress<ScanProgress>(p =>
        {
            if (!cts.IsCancellationRequested)
                ScanText = $"Taranıyor… {HumanSize.Number(p.Files)} dosya";
        });

        try
        {
            var roots = _roots.ToList();
            var candidates = await Task.Run(() => FolderScanner.Scan(roots, progress, cts.Token), cts.Token);
            cts.Token.ThrowIfCancellationRequested();

            _index.ReplaceAll(candidates);
            IndexStatus = $"İndeks hazır · {HumanSize.Number(_index.AliveCount)} dosya";
            ScanText = "";
            Toast($"{HumanSize.Number(candidates.Count)} dosya indekslendi");
            TriggerSearch(immediate: true);
        }
        catch (OperationCanceledException)
        {
            IndexStatus = _index.AliveCount > 0
                ? $"İndeks hazır · {HumanSize.Number(_index.AliveCount)} dosya"
                : "Tarama iptal edildi";
        }
        catch (Exception ex)
        {
            AppLog.Write("scan", ex);
            IndexStatus = "Tarama hatası";
            Toast("Tarama hatası: " + ex.Message, error: true);
        }
        finally
        {
            if (ReferenceEquals(_scanCts, cts))
            {
                IsScanning = false;
                ScanText = "";
            }
        }
    }

    private void TriggerSearch(bool immediate = false)
    {
        var previous = _searchCts;
        _searchCts = null;
        if (previous is not null)
        {
            try { previous.Cancel(); }
            catch (ObjectDisposedException) { }
        }

        var cts = new CancellationTokenSource();
        _searchCts = cts;
        _ = SearchAsync(BuildRequest(), cts, immediate);
    }

    private SearchRequest BuildRequest() => new()
    {
        Text = SearchText,
        SearchPath = SearchPath,
        Type = SelectedType?.Key ?? "all",
        Sort = SortKey,
        OnlyDuplicates = OnlyDuplicates,
        GroupMode = GroupMode,
    };

    private async Task SearchAsync(SearchRequest request, CancellationTokenSource cts, bool immediate)
    {
        try
        {
            if (!immediate) await Task.Delay(40, cts.Token);
            var sw = Stopwatch.StartNew();
            var result = await Task.Run(() => _index.Search(request, cts.Token), cts.Token);
            sw.Stop();
            cts.Token.ThrowIfCancellationRequested();
            ApplyResult(result, sw.Elapsed);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            AppLog.Write("search", ex);
            Toast("Arama hatası: " + ex.Message, error: true);
        }
        finally
        {
            if (ReferenceEquals(_searchCts, cts)) _searchCts = null;
            cts.Dispose();
        }
    }

    private void ApplyResult(SearchResult result, TimeSpan elapsed)
    {
        _lastResult = result;

        Rows.Clear();
        if (GroupMode)
        {
            foreach (var g in result.Groups)
            {
                var expanded = !_collapsed.Contains(g.Key);
                Rows.Add(HeaderVm(g, expanded));
                if (!expanded) continue;
                var index = 0;
                foreach (var f in g.Files)
                {
                    index++;
                    Rows.Add(new FileRowVm
                    {
                        File = f,
                        CopyIndex = index,
                        CopyCount = g.Count,
                        IsDuplicate = true,
                    });
                }
            }
        }
        else
        {
            var counts = result.Groups.ToDictionary(g => g.Key, g => g.Count, StringComparer.Ordinal);
            var seen = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var f in result.Files)
            {
                var count = counts.GetValueOrDefault(f.DuplicateKey);
                var copy = seen.GetValueOrDefault(f.DuplicateKey) + 1;
                seen[f.DuplicateKey] = copy;
                Rows.Add(new FileRowVm
                {
                    File = f,
                    CopyIndex = copy,
                    CopyCount = count,
                    IsDuplicate = count > 1,
                });
            }
        }

        UpdateTypes(result);

        ResultSummary = $"{HumanSize.Number(result.MatchedCount)} dosya · {HumanSize.Number(result.DuplicateGroupCount)} çift grup";
        ElapsedText = elapsed.TotalMilliseconds.ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("tr-TR")) + " ms";
        SectionTitle = GroupMode ? "Çift kopyalar" : "Dosyalar";
        SectionMeta = GroupMode
            ? $"{HumanSize.Number(result.DuplicateGroupCount)} grup · {HumanSize.Format(result.TotalWaste)} kazanılabilir"
            : $"{HumanSize.Number(result.Files.Count)} dosya";
        if (result.Truncated) SectionMeta += " · ilk sonuçlar gösteriliyor";

        HasResults = Rows.Count > 0;
        IsEmpty = !HasResults;
        CanStageAll = GroupMode && result.Groups.Count > 0;

        if (IsEmpty)
        {
            if (_index.AliveCount == 0)
            {
                EmptyTitle = "Henüz indeks yok";
                EmptyHint = "Bir klasör ekleyip indeksi oluştur. Hiçbir dosya izinsiz taranmaz.";
            }
            else if (!string.IsNullOrWhiteSpace(SearchText))
            {
                EmptyTitle = "Eşleşen dosya yok";
                EmptyHint = "Aramayı kısaltmayı veya \"Yol da arama\" seçeneğini deneyin.";
            }
            else if (OnlyDuplicates)
            {
                EmptyTitle = "Çift kopya bulunamadı";
                EmptyHint = "Taranan klasörlerde aynı ad + boyut + türde dosya yok. \"Yalnızca çiftler\" filtresini kapatıp tüm dosyaları görebilirsin.";
            }
            else
            {
                EmptyTitle = "Dosya bulunamadı";
                EmptyHint = "Klasör ekleyerek indeksi genişletebilirsin.";
            }
        }
    }

    private void UpdateTypes(SearchResult result)
    {
        _updatingTypes = true;
        var previous = _selectedType?.Key ?? "all";

        Types.Clear();
        Types.Add(new TypeChipVm { Key = "all", Label = "Tümü", Count = result.MatchedCount, Color = "#2DD4BF" });

        foreach (var (ext, count) in result.TypeCounts
                     .Where(kv => kv.Value > 0)
                     .OrderByDescending(kv => kv.Value)
                     .ThenBy(kv => kv.Key, StringComparer.Ordinal)
                     .Take(7))
        {
            Types.Add(new TypeChipVm
            {
                Key = ext,
                Label = ext.ToUpperInvariant(),
                Count = count,
                Color = FileTypeStyle.Color(ext),
            });
        }

        if (previous != "all" && Types.All(t => t.Key != previous))
        {
            Types.Add(new TypeChipVm
            {
                Key = previous,
                Label = previous.ToUpperInvariant(),
                Count = 0,
                Color = FileTypeStyle.Color(previous),
            });
        }

        _selectedType = Types.FirstOrDefault(t => t.Key == previous) ?? Types[0];
        Raise(nameof(SelectedType));
        _updatingTypes = false;
    }

    private GroupHeaderVm HeaderVm(DuplicateGroup g, bool expanded) => new()
    {
        Key = g.Key,
        Name = g.Name,
        ExtLabel = string.IsNullOrEmpty(g.Extension) ? "DOS" : g.Extension.ToUpperInvariant(),
        Color = FileTypeStyle.Color(g.Extension),
        Count = g.Count,
        Size = g.Size,
        Waste = g.Waste,
        PathHint = g.Files.Count > 0 ? g.Files[0].Directory : "",
        Expanded = expanded,
    };

    public void ToggleGroup(string key)
    {
        if (!_collapsed.Remove(key)) _collapsed.Add(key);
        if (_lastResult is null) return;

        var result = _lastResult;
        Rows.Clear();
        foreach (var g in result.Groups)
        {
            var expanded = !_collapsed.Contains(g.Key);
            Rows.Add(HeaderVm(g, expanded));
            if (!expanded) continue;
            var index = 0;
            foreach (var f in g.Files)
            {
                index++;
                Rows.Add(new FileRowVm
                {
                    File = f,
                    CopyIndex = index,
                    CopyCount = g.Count,
                    IsDuplicate = true,
                });
            }
        }
    }

    private void Toast(string message, bool error = false, bool undoable = false)
    {
        ToastText = message;
        ToastError = error;
        ToastCanUndo = undoable && CanUndoDelete;
        ToastVisible = true;
        _toastTimer.Stop();
        _toastTimer.Interval = TimeSpan.FromSeconds(undoable ? 10 : 3.6);
        _toastTimer.Start();
    }

    private static List<string> DefaultRoots()
    {
        var list = new List<string>();
        void Add(Environment.SpecialFolder folder)
        {
            var path = Environment.GetFolderPath(folder);
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path)) list.Add(path);
        }

        Add(Environment.SpecialFolder.Desktop);
        Add(Environment.SpecialFolder.MyDocuments);
        Add(Environment.SpecialFolder.MyPictures);
        Add(Environment.SpecialFolder.MyMusic);
        Add(Environment.SpecialFolder.MyVideos);

        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var downloads = Path.Combine(profile, "Downloads");
        if (Directory.Exists(downloads)) list.Add(downloads);

        var result = new List<string>();
        foreach (var path in list.OrderBy(p => p.Length))
        {
            if (result.Any(r => IsUnder(path, r))) continue;
            result.Add(path);
        }
        return result;
    }

    private static bool IsUnder(string path, string parent)
    {
        if (string.Equals(path, parent, StringComparison.OrdinalIgnoreCase)) return true;
        var prefix = parent.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                     + Path.DirectorySeparatorChar;
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}
