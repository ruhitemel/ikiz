using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Ikiz.Core;

namespace Ikiz.App.Services;

/// <summary>
/// Çalışma zamanı bütünlük, güncelleme kaynağı sabitleme (pinning) ve
/// tersine mühendislik / bellek enjeksiyonu koruması.
/// </summary>
public static class RuntimeGuard
{
    // "https://github.com/ruhitemel/ikiz" SHA-256 özeti (hex-edit ile URL yönlendirmesini engeller)
    private static readonly byte[] TrustedRepoHash = Convert.FromHexString(
        "BE35A9DA14A1204E87FB10D4CE74313378A992A24BFFA42F851FD835E365C288");

    // "Ruhi Temel (@ruhitemel)" SHA-256 özeti
    private static readonly byte[] TrustedDeveloperHash = Convert.FromHexString(
        "38BB1048D32202A2EEC3FD55F8914E3C78F59E65CE7A7796EC447F17E33F32F7");

    // "github.com/ruhitemel/ikiz" SHA-256 özeti
    private static readonly byte[] TrustedDisplayHash = Convert.FromHexString(
        "18C979F1960AC02D6F2B97FA79CFB25599D725E307BCCFFBC48BB95389B33FF8");

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool IsDebuggerPresent();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CheckRemoteDebuggerPresent(IntPtr hProcess, out bool pbDebuggerPresent);

    public static void Enforce()
    {
        if (!VerifyCoreIntegrity())
        {
            Environment.Exit(1);
            return;
        }

#if !DEBUG
        if (IsUnderDebuggerOrHook())
        {
            Environment.Exit(1);
        }
#endif
    }

    public static bool VerifyCoreIntegrity()
    {
        if (!IsTrustedRepositoryUrl(UpdateService.RepositoryUrl))
            return false;

        if (!VerifyAssembliesSameDirectory())
            return false;

        return true;
    }

    public static bool IsTrustedRepositoryUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)) return false;

        return MatchesSha256(url, TrustedRepoHash);
    }

    public static bool IsTrustedAttribution(string? developerCredit, string? repositoryDisplay)
    {
        if (string.IsNullOrWhiteSpace(developerCredit) || string.IsNullOrWhiteSpace(repositoryDisplay))
            return false;

        return MatchesSha256(developerCredit, TrustedDeveloperHash) &&
               MatchesSha256(repositoryDisplay, TrustedDisplayHash);
    }

    public static bool IsUnderDebuggerOrHook()
    {
        try
        {
            if (Debugger.IsAttached || Debugger.IsLogging())
                return true;

            if (OperatingSystem.IsWindows())
            {
                if (IsDebuggerPresent()) return true;
                if (CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, out var remote) && remote)
                    return true;
            }

            // CLR profiler veya başlangıç kancası (startup hook) enjeksiyonunu reddet.
            var hooks = Environment.GetEnvironmentVariable("DOTNET_STARTUP_HOOKS");
            if (!string.IsNullOrWhiteSpace(hooks)) return true;

            var corProf = Environment.GetEnvironmentVariable("COR_ENABLE_PROFILING");
            if (string.Equals(corProf, "1", StringComparison.Ordinal)) return true;

            var coreProf = Environment.GetEnvironmentVariable("CORECLR_ENABLE_PROFILING");
            if (string.Equals(coreProf, "1", StringComparison.Ordinal)) return true;
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool VerifyAssembliesSameDirectory()
    {
        try
        {
            var appLoc = typeof(RuntimeGuard).Assembly.Location;
            var coreLoc = typeof(FileIndex).Assembly.Location;
            if (string.IsNullOrEmpty(appLoc) || string.IsNullOrEmpty(coreLoc))
                return true; // single-file bundle

            var appDir = Path.GetFullPath(Path.GetDirectoryName(appLoc) ?? "");
            var coreDir = Path.GetFullPath(Path.GetDirectoryName(coreLoc) ?? "");
            return string.Equals(appDir, coreDir, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private static bool MatchesSha256(string input, ReadOnlySpan<byte> expected)
    {
        Span<byte> actual = stackalloc byte[32];
        var bytes = Encoding.UTF8.GetBytes(input);
        SHA256.HashData(bytes, actual);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
