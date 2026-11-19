using System.Globalization;
using System.Text;

namespace Ikiz.Core;

public static class TextNormalizer
{
    private static readonly Dictionary<char, char> Turkish = new()
    {
        ['\u0130'] = 'i', ['I'] = 'i', ['\u0131'] = 'i',
        ['\u015E'] = 's', ['\u015F'] = 's',
        ['\u011E'] = 'g', ['\u011F'] = 'g',
        ['\u00C7'] = 'c', ['\u00E7'] = 'c',
        ['\u00D6'] = 'o', ['\u00F6'] = 'o',
        ['\u00DC'] = 'u', ['\u00FC'] = 'u',
    };

    public static string Fold(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (Turkish.TryGetValue(ch, out var mapped))
            {
                sb.Append(mapped);
                continue;
            }

            var lower = char.ToLowerInvariant(ch);
            if (char.IsSurrogate(lower))
            {
                sb.Append(lower);
                continue;
            }

            var decomposed = lower.ToString().Normalize(NormalizationForm.FormD);
            if (decomposed.Length == 1)
            {
                sb.Append(decomposed[0]);
                continue;
            }

            foreach (var c in decomposed)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
        }

        return sb.ToString();
    }

    public static (string Folded, int[] Map) FoldWithMap(string value)
    {
        var sb = new StringBuilder(value.Length);
        var map = new List<int>(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            string piece;
            if (Turkish.TryGetValue(ch, out var mapped))
            {
                piece = mapped.ToString();
            }
            else
            {
                var lower = char.ToLowerInvariant(ch);
                if (char.IsSurrogate(lower))
                {
                    piece = lower.ToString();
                }
                else
                {
                    var decomposed = lower.ToString().Normalize(NormalizationForm.FormD);
                    if (decomposed.Length == 1)
                    {
                        piece = decomposed;
                    }
                    else
                    {
                        var b = new StringBuilder(decomposed.Length);
                        foreach (var c in decomposed)
                            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                                b.Append(c);
                        piece = b.ToString();
                    }
                }
            }

            foreach (var c in piece)
            {
                sb.Append(c);
                map.Add(i);
            }
        }

        return (sb.ToString(), map.ToArray());
    }

    public static string[] Tokenize(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<string>();
        return query
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
            .Select(Fold)
            .Where(t => t.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    public static List<(int Start, int End)> FindRanges(string text, IReadOnlyList<string> tokens)
    {
        var result = new List<(int, int)>();
        if (string.IsNullOrEmpty(text) || tokens.Count == 0) return result;

        var (folded, map) = FoldWithMap(text);
        foreach (var token in tokens)
        {
            var from = 0;
            while (from <= folded.Length - token.Length)
            {
                var idx = folded.IndexOf(token, from, StringComparison.Ordinal);
                if (idx < 0) break;
                var start = map[idx];
                var end = idx + token.Length - 1 < map.Length ? map[idx + token.Length - 1] + 1 : text.Length;
                result.Add((start, end));
                from = idx + Math.Max(1, token.Length);
            }
        }

        if (result.Count <= 1) return result;

        result.Sort((a, b) => a.Item1.CompareTo(b.Item1));
        var merged = new List<(int, int)>(result.Count);
        foreach (var r in result)
        {
            if (merged.Count > 0 && r.Item1 <= merged[^1].Item2)
            {
                var last = merged[^1];
                merged[^1] = (last.Item1, Math.Max(last.Item2, r.Item2));
            }
            else
            {
                merged.Add(r);
            }
        }

        return merged;
    }
}
