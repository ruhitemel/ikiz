using System.IO;

namespace Ikiz.App.ViewModels;

/// <summary>Konum panelinde listelenen bir disk / birim.</summary>
public sealed class DriveVm : BindableBase
{
    private bool _selected;
    private readonly Action? _onChanged;

    public DriveVm(string path, string label, string typeName, string sizeText, bool isReady, Action? onChanged = null)
    {
        Path = path;
        Label = label;
        TypeName = typeName;
        SizeText = sizeText;
        IsReady = isReady;
        _onChanged = onChanged;
    }

    public string Path { get; }
    public string Label { get; }
    public string TypeName { get; }
    public string SizeText { get; }
    public bool IsReady { get; }

    public string DisplayName => string.IsNullOrWhiteSpace(Label) ? Path : $"{Path}  {Label}";

    public bool Selected
    {
        get => _selected;
        set
        {
            if (!Set(ref _selected, value)) return;
            _onChanged?.Invoke();
        }
    }
}

/// <summary>Şu anda indekslenecek köklerden biri.</summary>
public sealed record RootVm(string Path, string DisplayName, bool IsDrive);
