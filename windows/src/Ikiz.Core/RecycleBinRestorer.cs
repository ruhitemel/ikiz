using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Ikiz.Core;

public sealed record RestoreOutcome(
    IReadOnlyList<string> Restored,
    IReadOnlyList<(string Path, string Error)> Failed)
{
    public int RestoredCount => Restored.Count;
}

/// <summary>
/// Geri dönüşüm kutusundaki öğeleri özgün konumlarına geri yükler.
/// Windows Kabuğu (Shell) üzerinden çalışır; dil bağımsız "undelete" fiilini kullanır.
/// </summary>
public static class RecycleBinRestorer
{
    public static RestoreOutcome Restore(IReadOnlyList<string> originalPaths)
    {
        var restored = new List<string>();
        var failed = new List<(string, string)>();

        if (originalPaths.Count == 0) return new RestoreOutcome(restored, failed);

        if (!OperatingSystem.IsWindows())
        {
            foreach (var path in originalPaths)
                failed.Add((path, "Geri yükleme yalnızca Windows'ta desteklenir"));
            return new RestoreOutcome(restored, failed);
        }

        RestoreOutcome? result = null;
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                // Bu noktaya yalnızca Windows'ta gelinir.
                if (OperatingSystem.IsWindows()) result = RestoreCore(originalPaths);
            }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (error is not null) throw error;
        return result ?? new RestoreOutcome(restored, failed);
    }

    [SupportedOSPlatform("windows")]
    private static RestoreOutcome RestoreCore(IReadOnlyList<string> originalPaths)
    {
        var restored = new List<string>();
        var failed = new List<(string, string)>();
        object? shell = null;

        try
        {
            var shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType is null)
                throw new InvalidOperationException("Windows Kabuğu başlatılamadı.");

            shell = Activator.CreateInstance(shellType);
            if (shell is null)
                throw new InvalidOperationException("Windows Kabuğu başlatılamadı.");

            dynamic app = shell;
            dynamic bin = app.Namespace(10); // ssfBITBUCKET
            dynamic items = bin.Items();
            var count = (int)items.Count;

            var found = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < count; i++)
            {
                dynamic item = items.Item(i);
                string? deletedFrom = null;
                string? name = null;
                try { deletedFrom = item.ExtendedProperty("System.Recycle.DeletedFrom") as string; }
                catch { }
                try { name = item.Name as string; }
                catch { }

                if (string.IsNullOrWhiteSpace(deletedFrom) || string.IsNullOrWhiteSpace(name)) continue;
                found[Path.Combine(deletedFrom!, name!)] = (object)item;
            }

            foreach (var path in originalPaths)
            {
                if (!found.TryGetValue(path, out var foundItem))
                {
                    failed.Add((path, "Geri dönüşüm kutusunda bulunamadı"));
                    continue;
                }

                dynamic item = foundItem;
                var invoked = false;
                foreach (var verb in new[] { "undelete", "Geri Yükle", "Restore" })
                {
                    try
                    {
                        item.InvokeVerb(verb);
                        invoked = true;
                        break;
                    }
                    catch { }
                }

                if (!invoked)
                {
                    failed.Add((path, "Geri yükleme komutu çalıştırılamadı"));
                    continue;
                }

                var ok = false;
                for (var wait = 0; wait < 12; wait++)
                {
                    if (File.Exists(path)) { ok = true; break; }
                    Thread.Sleep(120);
                }

                if (ok) restored.Add(path);
                else failed.Add((path, "Dosya özgün konumuna dönmedi"));
            }
        }
        finally
        {
            if (shell is not null && Marshal.IsComObject(shell))
                Marshal.FinalReleaseComObject(shell);
        }

        return new RestoreOutcome(restored, failed);
    }
}
