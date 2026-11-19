using System.Runtime.InteropServices;

namespace Ikiz.Core;

public sealed record DeleteOutcome(
    IReadOnlyList<string> Deleted,
    IReadOnlyList<(string Path, string Error)> Failed,
    long FreedBytes);

public static class FileDeleter
{
    private const uint FoDelete = 0x0003;
    private const ushort FofSilent = 0x0004;
    private const ushort FofNoConfirmation = 0x0010;
    private const ushort FofAllowUndo = 0x0040;
    private const ushort FofNoErrorUi = 0x0400;
    private const ushort FofWantNukeWarning = 0x4000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShFileOpStruct
    {
        public IntPtr hwnd;
        public uint wFunc;
        [MarshalAs(UnmanagedType.LPWStr)] public string pFrom;
        [MarshalAs(UnmanagedType.LPWStr)] public string? pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        [MarshalAs(UnmanagedType.LPWStr)] public string? lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref ShFileOpStruct fileOp);

    public static DeleteOutcome Delete(IReadOnlyList<FileItem> items, DeleteMode mode)
    {
        if (mode == DeleteMode.Permanent) return DeletePermanent(items);
        if (!OperatingSystem.IsWindows())
        {
            return new DeleteOutcome(
                Array.Empty<string>(),
                items.Select(i => (i.FullPath, "Geri dönüşüm kutusu yalnızca Windows'ta desteklenir")).ToList(),
                0);
        }

        DeleteOutcome? result = null;
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try { result = Recycle(items); }
            catch (Exception ex) { error = ex; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error is not null) throw error;
        return result!;
    }

    private static DeleteOutcome Recycle(IReadOnlyList<FileItem> items)
    {
        var deleted = new List<string>();
        var failed = new List<(string, string)>();
        long freed = 0;

        var alive = new List<FileItem>();
        foreach (var item in items)
        {
            if (File.Exists(item.FullPath)) alive.Add(item);
            else failed.Add((item.FullPath, "Dosya bulunamadı"));
        }

        if (alive.Count == 0) return new DeleteOutcome(deleted, failed, 0);

        var from = string.Join("\0", alive.Select(i => i.FullPath)) + "\0\0";
        var op = new ShFileOpStruct
        {
            wFunc = FoDelete,
            pFrom = from,
            fFlags = (ushort)(FofAllowUndo | FofNoConfirmation | FofSilent | FofNoErrorUi | FofWantNukeWarning),
        };

        var code = SHFileOperation(ref op);
        if (code != 0)
        {
            foreach (var item in alive)
                failed.Add((item.FullPath, $"Geri dönüşüm kutusu hatası (kod {code})"));
            return new DeleteOutcome(deleted, failed, 0);
        }

        foreach (var item in alive)
        {
            if (File.Exists(item.FullPath))
                failed.Add((item.FullPath, op.fAnyOperationsAborted ? "İşlem iptal edildi" : "Dosya taşınamadı"));
            else
            {
                deleted.Add(item.FullPath);
                freed += item.Size;
            }
        }

        return new DeleteOutcome(deleted, failed, freed);
    }

    private static DeleteOutcome DeletePermanent(IReadOnlyList<FileItem> items)
    {
        var deleted = new List<string>();
        var failed = new List<(string, string)>();
        long freed = 0;

        foreach (var item in items)
        {
            if (!File.Exists(item.FullPath))
            {
                failed.Add((item.FullPath, "Dosya bulunamadı"));
                continue;
            }

            try
            {
                File.Delete(item.FullPath);
                deleted.Add(item.FullPath);
                freed += item.Size;
            }
            catch (Exception ex)
            {
                failed.Add((item.FullPath, ex.Message));
            }
        }

        return new DeleteOutcome(deleted, failed, freed);
    }
}
