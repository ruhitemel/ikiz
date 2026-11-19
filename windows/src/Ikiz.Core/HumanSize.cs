using System.Globalization;

namespace Ikiz.Core;

public static class HumanSize
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly string[] Units = { "B", "KB", "MB", "GB", "TB", "PB" };

    public static string Format(long bytes)
    {
        if (bytes < 0) bytes = 0;
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0 ? $"{bytes} B" : $"{value.ToString("0.#", Tr)} {Units[unit]}";
    }

    public static string Number(long value) => value.ToString("N0", Tr);
}
