using System.Globalization;

namespace Ikiz.App;

public static class FileTypeStyle
{
    private static readonly Dictionary<string, string> Colors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pdf"] = "#F87171",
        ["png"] = "#A78BFA",
        ["jpg"] = "#FBBF24",
        ["jpeg"] = "#FBBF24",
        ["gif"] = "#34D399",
        ["webp"] = "#34D399",
        ["svg"] = "#F472B6",
        ["doc"] = "#60A5FA",
        ["docx"] = "#60A5FA",
        ["xls"] = "#34D399",
        ["xlsx"] = "#34D399",
        ["ppt"] = "#FB923C",
        ["pptx"] = "#FB923C",
        ["txt"] = "#94A3B8",
        ["md"] = "#94A3B8",
        ["zip"] = "#FACC15",
        ["rar"] = "#FACC15",
        ["7z"] = "#FACC15",
        ["mp4"] = "#F472B6",
        ["mkv"] = "#F472B6",
        ["mp3"] = "#22D3EE",
        ["wav"] = "#22D3EE",
        ["exe"] = "#A3E635",
        ["msi"] = "#A3E635",
        ["json"] = "#FDE68A",
        ["csv"] = "#34D399",
    };

    private static readonly string[] Palette =
    {
        "#2DD4BF", "#60A5FA", "#A78BFA", "#F472B6", "#FBBF24", "#FB923C", "#34D399", "#22D3EE", "#F87171",
    };

    public static string Color(string extension)
    {
        if (string.IsNullOrEmpty(extension)) return Shade("#647182");
        if (!Colors.TryGetValue(extension, out var color))
        {
            var hash = 0;
            foreach (var c in extension) hash = (hash * 31 + c) & 0x7FFFFFFF;
            color = Palette[hash % Palette.Length];
        }

        return Shade(color);
    }

    /// <summary>Koyu temada renk olduğu gibi, açık temada okunur olması için koyulaştırılır.</summary>
    private static string Shade(string hex)
    {
        if (!ThemeManager.IsLight) return hex;
        if (hex.Length != 7 || hex[0] != '#') return hex;
        if (!int.TryParse(hex.AsSpan(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            return hex;

        var r = (int)(((value >> 16) & 0xFF) * 0.62);
        var g = (int)(((value >> 8) & 0xFF) * 0.62);
        var b = (int)((value & 0xFF) * 0.62);
        return $"#{r:X2}{g:X2}{b:X2}";
    }
}
