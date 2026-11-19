using Ikiz.Core;

namespace Ikiz.App.ViewModels;

public sealed class TypeChipVm
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public required int Count { get; init; }
    public required string Color { get; init; }
    public string CountText => Count > 0 ? HumanSize.Number(Count) : "0";
}

public abstract class ResultRowVm
{
}

public sealed class GroupHeaderVm : ResultRowVm
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string ExtLabel { get; init; }
    public required string Color { get; init; }
    public required int Count { get; init; }
    public required long Size { get; init; }
    public required long Waste { get; init; }
    public required string PathHint { get; init; }
    public required bool Expanded { get; init; }

    public string CountText => $"{Count} kopya";
    public string SizeText => HumanSize.Format(Size);
    public string WasteText => HumanSize.Format(Waste);
    public string ChevronGlyph => Expanded ? "\uE70D" : "\uE76C";
    public string ExpandTitle => Expanded ? "Grubu daralt" : "Grubu genişlet";
}

public sealed class FileRowVm : ResultRowVm
{
    public required FileItem File { get; init; }
    public required int CopyIndex { get; init; }
    public required int CopyCount { get; init; }
    public required bool IsDuplicate { get; init; }

    public string Name => File.Name;
    public string Path => File.Directory;
    public string SizeText => File.SizeText;
    public string ExtLabel => File.ExtLabel;
    public string Color => FileTypeStyle.Color(File.Extension);
    public string CopyText => CopyCount > 1 ? $"Kopya {CopyIndex}/{CopyCount}" : "";
    public bool ShowCopy => CopyCount > 1;
}
