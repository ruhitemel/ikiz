using System.Globalization;

namespace Ikiz.Core;

public sealed class FileItem
{
    private string? _foldedName;
    private string? _foldedPath;
    private string? _key;

    public int Id { get; init; }
    public required string FullPath { get; init; }
    public required string Name { get; init; }
    public required string Directory { get; init; }
    public string Extension { get; init; } = "";
    public long Size { get; init; }
    public DateTime ModifiedUtc { get; init; }
    public bool IsDirectory { get; init; }

    public bool IsZeroByteFile => !IsDirectory && Size == 0;
    public bool IsEmptyItem => IsDirectory || Size == 0;

    public string FoldedName => _foldedName ??= TextNormalizer.Fold(Name);
    public string FoldedPath => _foldedPath ??= TextNormalizer.Fold(FullPath);

    public string DuplicateKey => _key ??= IsDirectory
        ? $"dir\u001f{FoldedPath}"
        : $"{FoldedName}\u001f{Size.ToString(CultureInfo.InvariantCulture)}\u001f{Extension}";

    public string SizeText => IsDirectory ? "Boş klasör" : HumanSize.Format(Size);
    public string ExtLabel => IsDirectory ? "KLS" : (string.IsNullOrEmpty(Extension) ? "DOS" : Extension.ToUpperInvariant());
}

public sealed record FileCandidate(
    string FullPath,
    string Name,
    string Directory,
    string Extension,
    long Size,
    DateTime ModifiedUtc,
    bool IsDirectory = false);
