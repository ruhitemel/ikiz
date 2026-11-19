using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Ikiz.Core;

namespace Ikiz.App.Controls;

public sealed class HighlightText : TextBlock
{
    public static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
        nameof(Content), typeof(string), typeof(HighlightText),
        new PropertyMetadata(string.Empty, OnChanged));

    public static readonly DependencyProperty QueryProperty = DependencyProperty.Register(
        nameof(Query), typeof(string), typeof(HighlightText),
        new PropertyMetadata(string.Empty, OnChanged));

    public string Content
    {
        get => (string)GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    public string Query
    {
        get => (string)GetValue(QueryProperty);
        set => SetValue(QueryProperty, value);
    }

    private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((HighlightText)d).Rebuild();

    private void Rebuild()
    {
        Inlines.Clear();
        var text = Content ?? string.Empty;
        if (text.Length == 0) return;

        var tokens = TextNormalizer.Tokenize(Query);
        var ranges = TextNormalizer.FindRanges(text, tokens);
        if (ranges.Count == 0)
        {
            Inlines.Add(new Run(text));
            return;
        }

        var index = 0;
        foreach (var (start, end) in ranges)
        {
            if (start < index) continue;
            if (start > index) Inlines.Add(new Run(text[index..start]));
            var run = new Run(text[start..Math.Min(end, text.Length)])
            {
                FontWeight = FontWeights.SemiBold,
            };
            run.SetResourceReference(TextElement.ForegroundProperty, "AccentStrongBrush");
            Inlines.Add(run);
            index = Math.Min(end, text.Length);
        }

        if (index < text.Length) Inlines.Add(new Run(text[index..]));
    }
}
