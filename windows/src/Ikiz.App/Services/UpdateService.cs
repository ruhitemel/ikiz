using System.IO;
using System.Reflection;
using Velopack;
using Velopack.Sources;

namespace Ikiz.App.Services;

/// <summary>
/// GitHub Releases üzerinden dağıtılan sürümleri denetler.
/// Uygulama Velopack kurulumuyla yüklendiğinde otomatik güncelleme kullanılabilir.
/// </summary>
public sealed class UpdateService
{
    public const string RepositoryUrl = "https://github.com/ruhitemel/ikiz";
    public const string ReleasesUrl = RepositoryUrl + "/releases";
    public const string IssuesUrl = RepositoryUrl + "/issues/new/choose";

    private readonly UpdateManager? _manager;

    public UpdateService()
    {
        try
        {
            if (!RuntimeGuard.IsTrustedRepositoryUrl(RepositoryUrl))
            {
                _manager = null;
                return;
            }
            _manager = new UpdateManager(new GithubSource(RepositoryUrl, null, false));
        }
        catch (Exception ex)
        {
            AppLog.Write("update-init", ex);
            _manager = null;
        }
    }

    public string Version
    {
        get
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version is null) return "1.0.0";
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }
    }

    public bool IsInstalled => _manager?.IsInstalled == true;

    public bool HasPendingRestart => _manager?.UpdatePendingRestart is not null;

    public async Task<UpdateInfo?> CheckAsync()
    {
        if (_manager is null || !_manager.IsInstalled) return null;
        return await _manager.CheckForUpdatesAsync().ConfigureAwait(false);
    }

    public async Task DownloadAsync(UpdateInfo info, Action<int> progress, CancellationToken token = default)
    {
        if (_manager is null) return;
        await _manager.DownloadUpdatesAsync(info, progress, token).ConfigureAwait(false);
    }

    public void ApplyAndRestart(UpdateInfo info)
    {
        _manager?.ApplyUpdatesAndRestart(info.TargetFullRelease);
    }

    public static bool ShouldCheckSilently() => true;

    public static void MarkChecked()
    {
        try
        {
            var path = Path.Combine(AppPaths.DataDirectory, "last-update-check.txt");
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // eski denetim damgası silinemezse sessizce devam et
        }
    }
}
