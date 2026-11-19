namespace Ikiz.Core;

public sealed record ScanProgress(int Files, string CurrentDirectory);

public static class FolderScanner
{
    private static readonly HashSet<string> SkipNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "$recycle.bin",
        "system volume information",
        "node_modules",
        ".git",
        ".svn",
        ".hg",
        ".vs",
        ".idea",
        "__pycache__",
        "winsxs",
        "windows",
        "windows.old",
        "recovery",
        "documents and settings",
        "appdata",
        "program files",
        "program files (x86)",
        "programdata",
    };

    public static List<FileCandidate> Scan(
        IEnumerable<string> roots,
        IProgress<ScanProgress>? progress,
        CancellationToken token)
    {
        var files = new List<FileCandidate>(4096);
        var seenRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root)) continue;
            var fullRoot = Path.GetFullPath(root);
            if (!seenRoots.Add(fullRoot)) continue;
            if (!System.IO.Directory.Exists(fullRoot)) continue;

            var stack = new Stack<string>();
            stack.Push(fullRoot);
            while (stack.Count > 0)
            {
                token.ThrowIfCancellationRequested();
                var dir = stack.Pop();
                if (ShouldSkipDirectory(dir)) continue;

                try
                {
                    foreach (var file in System.IO.Directory.EnumerateFiles(dir))
                    {
                        token.ThrowIfCancellationRequested();
                        FileInfo info;
                        try
                        {
                            info = new FileInfo(file);
                            if (!info.Exists) continue;

                            // Sistem, gizli, çevrimdışı ve sembolik bağlantı (reparse point) dosyaları listeye girmez.
                            var attrs = info.Attributes;
                            if ((attrs & (FileAttributes.Hidden | FileAttributes.System | FileAttributes.Offline | FileAttributes.ReparsePoint)) != 0)
                                continue;
                        }
                        catch
                        {
                            continue;
                        }

                        var extension = info.Extension.TrimStart('.').ToLowerInvariant();

                        // Yalnızca belge, görsel, video ve ses dosyaları indekslenir.
                        if (!FileCategories.IsUserContent(extension)) continue;

                        files.Add(new FileCandidate(
                            info.FullName,
                            info.Name,
                            dir,
                            extension,
                            info.Length,
                            info.LastWriteTimeUtc));

                        if (files.Count % 400 == 0)
                            progress?.Report(new ScanProgress(files.Count, dir));
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
                catch (System.Security.SecurityException) { }

                try
                {
                    foreach (var sub in System.IO.Directory.EnumerateDirectories(dir))
                    {
                        try
                        {
                            var attrs = File.GetAttributes(sub);
                            if ((attrs & FileAttributes.ReparsePoint) != 0) continue;
                        }
                        catch
                        {
                            continue;
                        }

                        stack.Push(sub);
                    }
                }
                catch (UnauthorizedAccessException) { }
                catch (IOException) { }
                catch (System.Security.SecurityException) { }
            }
        }

        progress?.Report(new ScanProgress(files.Count, string.Empty));
        return files;
    }

    private static bool ShouldSkipDirectory(string dir)
    {
        var name = Path.GetFileName(dir);
        if (!string.IsNullOrEmpty(name) && SkipNames.Contains(name)) return true;

        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        return !string.IsNullOrEmpty(windows) && dir.Equals(windows, StringComparison.OrdinalIgnoreCase);
    }
}
