using System.Diagnostics;
using System.Windows;
using Ikiz.App.Services;
using Velopack;

namespace Ikiz.App.ViewModels;

public enum UpdatePhase
{
    Idle,
    Checking,
    UpToDate,
    Available,
    Downloading,
    Ready,
    Unsupported,
    Failed,
}

public sealed partial class MainViewModel
{
    private readonly UpdateService _updateService = new();
    private UpdateInfo? _pendingUpdate;
    private UpdatePhase _updatePhase = UpdatePhase.Idle;
    private string _updateStatus = "Güncellemeler henüz denetlenmedi.";
    private string _updateNotes = "";
    private int _updatePercent;
    private bool _updateBadge;

    private bool _aboutOpen;
    public bool AboutOpen
    {
        get => _aboutOpen;
        private set => Set(ref _aboutOpen, value);
    }

    public string AppVersion => _updateService.Version;
    public string AboutVersionText => "Sürüm " + AppVersion;
    public string DeveloperCredit => "Ruhi Temel · @ruhitemel";
    public string RepositoryDisplay => "github.com/ruhitemel/ikiz";
    public string LicenseName => "MIT Açık Kaynak Lisansı";
    public string ProjectUrl => UpdateService.RepositoryUrl;
    public string ReleasesUrl => UpdateService.ReleasesUrl;

    public string UpdateStatus
    {
        get => _updateStatus;
        private set => Set(ref _updateStatus, value);
    }

    public string UpdateNotes
    {
        get => _updateNotes;
        private set => Set(ref _updateNotes, value);
    }

    public int UpdatePercent
    {
        get => _updatePercent;
        private set
        {
            if (!Set(ref _updatePercent, value)) return;
            Raise(nameof(UpdateActionText));
            Raise(nameof(UpdateStatus));
            Raise(nameof(UpdateProgress));
        }
    }

    public double UpdateProgress => _updatePercent;

    public bool UpdateBadge
    {
        get => _updateBadge;
        private set => Set(ref _updateBadge, value);
    }

    public bool UpdateBusy => _updatePhase is UpdatePhase.Checking or UpdatePhase.Downloading;

    public bool IsDownloading => _updatePhase == UpdatePhase.Downloading;

    public string UpdateActionText => _updatePhase switch
    {
        UpdatePhase.Checking => "Denetleniyor…",
        UpdatePhase.Downloading => $"İndiriliyor… %{_updatePercent}",
        UpdatePhase.Ready => "Yeniden başlat ve güncelle",
        UpdatePhase.Available => "İndir ve yeniden başlat",
        UpdatePhase.Unsupported => "Sürüm sayfasını aç",
        UpdatePhase.UpToDate => "Yeniden denetle",
        UpdatePhase.Failed => "Yeniden dene",
        _ => "Güncellemeleri denetle",
    };

    public bool CanRunUpdateAction => !UpdateBusy;

    public void OpenAbout()
    {
        AboutOpen = true;
        if (_updatePhase == UpdatePhase.Idle)
            _ = CheckUpdatesAsync(manual: false);
    }

    public void CloseAbout() => AboutOpen = false;

    public void OpenProjectPage() => OpenUrl(ProjectUrl);
    public void OpenReleasesPage() => OpenUrl(ReleasesUrl);
    public void OpenIssuesPage() => OpenUrl(UpdateService.IssuesUrl);

    public async Task RunUpdateActionAsync()
    {
        if (UpdateBusy) return;

        switch (_updatePhase)
        {
            case UpdatePhase.Available:
                await DownloadUpdateAsync();
                break;
            case UpdatePhase.Ready:
                ApplyUpdate();
                break;
            case UpdatePhase.Unsupported:
                OpenReleasesPage();
                break;
            default:
                await CheckUpdatesAsync(manual: true);
                break;
        }
    }

    /// <summary>Uygulama açılışında günde en fazla bir kez sessiz denetim yapar.</summary>
    public async Task CheckUpdatesSilentlyAsync()
    {
        if (!_updateService.IsInstalled) return;
        if (!UpdateService.ShouldCheckSilently()) return;

        try
        {
            var info = await _updateService.CheckAsync();
            UpdateService.MarkChecked();
            if (info is null) return;

            _pendingUpdate = info;
            _updatePhase = UpdatePhase.Available;
            UpdateNotes = info.TargetFullRelease.NotesMarkdown ?? "";
            UpdateStatus = $"Yeni sürüm v{info.TargetFullRelease.Version} hazır.";
            UpdateBadge = true;
            RaiseUpdateState();
        }
        catch (Exception ex)
        {
            AppLog.Write("update-silent", ex);
        }
    }

    private async Task CheckUpdatesAsync(bool manual)
    {
        if (!_updateService.IsInstalled)
        {
            _updatePhase = UpdatePhase.Unsupported;
            UpdateStatus = "Otomatik güncelleme, kurulumla yüklenen sürümde çalışır. " +
                           "Taşınabilir sürümü kullanıyorsan yeni sürümü GitHub'dan indirebilirsin.";
            RaiseUpdateState();
            return;
        }

        _updatePhase = UpdatePhase.Checking;
        UpdateStatus = "Güncellemeler denetleniyor…";
        RaiseUpdateState();

        try
        {
            var info = await _updateService.CheckAsync();
            UpdateService.MarkChecked();

            if (info is null)
            {
                _pendingUpdate = null;
                _updatePhase = UpdatePhase.UpToDate;
                UpdateNotes = "";
                UpdateStatus = $"En son sürümü kullanıyorsun (v{AppVersion}).";
                UpdateBadge = false;
            }
            else
            {
                _pendingUpdate = info;
                _updatePhase = UpdatePhase.Available;
                UpdateNotes = info.TargetFullRelease.NotesMarkdown ?? "";
                UpdateStatus = $"Yeni sürüm v{info.TargetFullRelease.Version} hazır.";
                UpdateBadge = true;
            }
        }
        catch (Exception ex)
        {
            AppLog.Write("update-check", ex);
            _updatePhase = UpdatePhase.Failed;
            UpdateStatus = "Güncellemeler denetlenemedi. İnternet bağlantını kontrol edip yeniden dene.";
        }

        RaiseUpdateState();
    }

    private async Task DownloadUpdateAsync()
    {
        if (_pendingUpdate is null) return;

        _updatePhase = UpdatePhase.Downloading;
        UpdatePercent = 0;
        UpdateStatus = "Güncelleme indiriliyor…";
        RaiseUpdateState();

        try
        {
            await _updateService.DownloadAsync(_pendingUpdate, percent =>
            {
                var app = Application.Current;
                if (app is null) return;
                app.Dispatcher.BeginInvoke(() => UpdatePercent = Math.Clamp(percent, 0, 100));
            });

            _updatePhase = UpdatePhase.Ready;
            UpdateStatus = "Güncelleme hazır. Uygulamayı yeniden başlatınca yeni sürüm açılır.";
        }
        catch (Exception ex)
        {
            AppLog.Write("update-download", ex);
            _updatePhase = UpdatePhase.Failed;
            UpdateStatus = "Güncelleme indirilemedi. Yeniden deneyebilirsin.";
        }

        RaiseUpdateState();
    }

    private void ApplyUpdate()
    {
        if (_pendingUpdate is null) return;
        try
        {
            _updateService.ApplyAndRestart(_pendingUpdate);
        }
        catch (Exception ex)
        {
            AppLog.Write("update-apply", ex);
            Toast("Güncelleme uygulanamadı: " + ex.Message, error: true);
        }
    }

    private void RaiseUpdateState()
    {
        Raise(nameof(UpdateStatus));
        Raise(nameof(UpdateNotes));
        Raise(nameof(UpdateBadge));
        Raise(nameof(UpdateBusy));
        Raise(nameof(IsDownloading));
        Raise(nameof(UpdateActionText));
        Raise(nameof(CanRunUpdateAction));
        Raise(nameof(UpdateProgress));
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            AppLog.Write("open-url", ex);
        }
    }
}
