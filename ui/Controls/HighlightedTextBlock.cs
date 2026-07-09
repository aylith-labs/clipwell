using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Clipwell.Ui.Controls;

/// <summary>
/// A TextBlock that marks every occurrence of <see cref="Query"/> inside
/// <see cref="HighlightedText"/> (case-insensitive). With an empty query it
/// assigns plain <c>Text</c> — no inline allocation — which is the state the
/// picker shows in on the warm show path (search is reset on every show).
/// </summary>
public sealed class HighlightedTextBlock : TextBlock
{
    public static readonly StyledProperty<string?> HighlightedTextProperty =
        AvaloniaProperty.Register<HighlightedTextBlock, string?>(nameof(HighlightedText));

    public static readonly StyledProperty<string?> QueryProperty =
        AvaloniaProperty.Register<HighlightedTextBlock, string?>(nameof(Query));

    public static readonly StyledProperty<IBrush?> HighlightBrushProperty =
        AvaloniaProperty.Register<HighlightedTextBlock, IBrush?>(nameof(HighlightBrush));

    static HighlightedTextBlock()
    {
        HighlightedTextProperty.Changed.AddClassHandler<HighlightedTextBlock>((block, _) => block.Rebuild());
        QueryProperty.Changed.AddClassHandler<HighlightedTextBlock>((block, _) => block.Rebuild());
        HighlightBrushProperty.Changed.AddClassHandler<HighlightedTextBlock>((block, _) => block.Rebuild());
    }

    public string? HighlightedText
    {
        get => GetValue(HighlightedTextProperty);
        set => SetValue(HighlightedTextProperty, value);
    }

    public string? Query
    {
        get => GetValue(QueryProperty);
        set => SetValue(QueryProperty, value);
    }

    public IBrush? HighlightBrush
    {
        get => GetValue(HighlightBrushProperty);
        set => SetValue(HighlightBrushProperty, value);
    }

    private void Rebuild()
    {
        var text = HighlightedText ?? "";
        var query = Query?.Trim() ?? "";
        if (query.Length == 0 || !text.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            Inlines?.Clear();
            Text = text;
            return;
        }

        var inlines = Inlines ??= [];
        inlines.Clear();
        var position = 0;
        while (position < text.Length)
        {
            var hit = text.IndexOf(query, position, StringComparison.OrdinalIgnoreCase);
            if (hit < 0)
            {
                inlines.Add(new Run(text[position..]));
                break;
            }
            if (hit > position) inlines.Add(new Run(text[position..hit]));
            inlines.Add(new Run(text[hit..(hit + query.Length)]) { Background = HighlightBrush });
            position = hit + query.Length;
        }
    }
}
