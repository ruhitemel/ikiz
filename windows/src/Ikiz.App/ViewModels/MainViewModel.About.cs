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

    private bool _updatePromptOpen;
    public bool UpdatePromptOpen
    {
        get => _updatePromptOpen;
        private set => Set(ref _updatePromptOpen, value);
    }

    private string _updatePromptVersion = "";
    public string UpdatePromptTitle => string.IsNullOrEmpty(_updatePromptVersion)
        ? "Yeni güncelleme mevcut"
        : $"Yeni güncelleme mevcut (v{_updatePromptVersion})";

    public string UpdatePromptDetail => _updatePhase switch
    {
        UpdatePhase.Downloading => $"Güncelleme indiriliyor… %{_updatePercent}",
        UpdatePhase.Ready => "Güncelleme indirildi. Kurulum için yeniden başlatmaya hazır.",
        _ => "GitHub üzerinde yeni sürüm yayınlandı. Şimdi indirip kurabilir veya erteleyebilirsin.",
    };

    public string UpdatePromptPrimaryText => _updatePhase switch
    {
        UpdatePhase.Downloading => $"İndiriliyor… %{_updatePercent}",
        UpdatePhase.Ready => "Şimdi yeniden başlat",
        _ => "İndir ve kur",
    };

    public string AppVersion => _updateService.Version;
    public string AboutVersionText => "Sürüm " + AppVersion;
    public string DeveloperCredit => "Ruhi Temel (@ruhitemel)";
    public string OpenSourceNote => "Açık kaynaklıdır, herkesin kullanımına açıktır.";
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
            Raise(nameof(UpdatePromptDetail));
            Raise(nameof(UpdatePromptPrimaryText));
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
        if (!RuntimeGuard.IsTrustedAttribution(DeveloperCredit, RepositoryDisplay))
            return;

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

    /// <summary>Uygulama açılışında arka planda yeni sürümü denetler ve varsa küçük uyarı kartını açar.</summary>
    public async Task CheckUpdatesSilentlyAsync()
    {
        if (!_updateService.IsInstalled) return;

        try
        {
            var info = await _updateService.CheckAsync();
            UpdateService.MarkChecked();
            if (info is null) return;

            _pendingUpdate = info;
            _updatePhase = UpdatePhase.Available;
            _updatePromptVersion = info.TargetFullRelease.Version.ToString();
            UpdateNotes = info.TargetFullRelease.NotesMarkdown ?? "";
            UpdateStatus = $"Yeni sürüm v{_updatePromptVersion} hazır.";
            UpdateBadge = true;
            UpdatePromptOpen = true;
            RaiseUpdateState();
        }
        catch (Exception ex)
        {
            AppLog.Write("update-silent", ex);
        }
    }

    /// <summary>Küçük güncelleme uyarısındaki "İndir ve kur" düğmesi: indirir ve tamamlanınca kurup yeniden başlatır.</summary>
    public async Task InstallUpdateFromPromptAsync()
    {
        if (UpdateBusy) return;

        if (_updatePhase == UpdatePhase.Ready)
        {
            ApplyUpdate();
            return;
        }

        if (_pendingUpdate is null) return;
        await DownloadUpdateAsync();
        if (_updatePhase == UpdatePhase.Ready)
        {
            ApplyUpdate();
        }
    }

    /// <summary>Küçük güncelleme uyarısını bu oturum için erteler (Hakkında simgesindeki rozet kalır).</summary>
    public void DismissUpdatePrompt() => UpdatePromptOpen = false;

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
                _updatePromptVersion = info.TargetFullRelease.Version.ToString();
                UpdateNotes = info.TargetFullRelease.NotesMarkdown ?? "";
                UpdateStatus = $"Yeni sürüm v{_updatePromptVersion} hazır.";
                UpdateBadge = true;
                if (!AboutOpen) UpdatePromptOpen = true;
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
        Raise(nameof(UpdatePromptTitle));
        Raise(nameof(UpdatePromptDetail));
        Raise(nameof(UpdatePromptPrimaryText));
    }

    private static void OpenUrl(string url)
    {
        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase) ||
                !uri.AbsolutePath.StartsWith("/ruhitemel/ikiz", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            AppLog.Write("open-url", ex);
        }
    }
}
