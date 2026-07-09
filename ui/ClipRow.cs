using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Clipwell.Protocol;

namespace Clipwell.Ui;

/// <summary>Row metadata display options, applied from settings at startup.</summary>
public static class ClipDisplay
{
    public static bool ShowSource = true;
    public static bool ShowTime = true;
    public static bool ShowChars = true;
}

/// <summary>
/// View-side wrapper around a <see cref="ClipItem"/> that exposes display-ready
/// strings (and, for image items, an async-loaded thumbnail).
/// </summary>
public sealed class ClipRow : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public ClipItem Item { get; }

    /// <summary>
    /// The search query active when this row was built (rows are rebuilt on every
    /// keystroke), consumed by the preview's highlight control. Sensitive rows
    /// never highlight — their preview is a mask.
    /// </summary>
    public string SearchQuery { get; }

    public ClipRow(ClipItem item, ClipwellClient? client = null, string searchQuery = "")
    {
        Item = item;
        SearchQuery = item.IsSensitive ? "" : searchQuery;
        if (item.HasImage && !item.IsSensitive && client is not null)
            _ = LoadThumbnailAsync(client);
        else if (!item.IsSensitive && item.Kind is "url" or "github-pr")
            _ = LoadFaviconAsync();
    }

    private async System.Threading.Tasks.Task LoadFaviconAsync()
    {
        var bmp = await FaviconLoader.GetAsync(Item.TextContent);
        if (bmp is null) return;
        await Dispatcher.UIThread.InvokeAsync(() => Thumbnail = bmp); // reuses the thumbnail slot
    }

    private Bitmap? _thumbnail;
    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        private set
        {
            _thumbnail = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Thumbnail)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasThumbnail)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowGlyph)));
        }
    }

    public bool HasThumbnail => _thumbnail is not null;

    /// <summary>Show the kind glyph only when there's no thumbnail to show instead.</summary>
    public bool ShowGlyph => _thumbnail is null;

    private async System.Threading.Tasks.Task LoadThumbnailAsync(ClipwellClient client)
    {
        var bytes = await client.GetImageBytesAsync(Item.Timestamp);
        if (bytes is null) return;
        try
        {
            using var ms = new MemoryStream(bytes);
            // Decode to a small thumbnail width to keep memory and layout cheap.
            var bmp = Bitmap.DecodeToWidth(ms, 48);
            await Dispatcher.UIThread.InvokeAsync(() => Thumbnail = bmp);
        }
        catch
        {
            // ignore undecodable images
        }
    }

    /// <summary>Set by the view-model when this row starts a new group (date/source).</summary>
    public string? GroupHeader { get; set; }
    public bool HasGroupHeader => !string.IsNullOrEmpty(GroupHeader);

    public bool IsPinned => Item.IsUserPinned;
    public bool IsSensitive => Item.IsSensitive;

    /// <summary>Lucide geometry for the row's kind (sensitive rows always show the lock).</summary>
    public StreamGeometry KindIcon => Item.IsSensitive ? Icons.Lock : Icons.ForKind(Item.Kind);

    /// <summary>Code-like kinds render their preview in the mono stack.</summary>
    public bool IsMonoPreview => Item.Kind is "code" or "color" or "path";

    /// <summary>An actual swatch for color items (e.g. <c>#3366ff</c>), or null.</summary>
    public IBrush? ColorSwatch =>
        Item.Kind == "color" && !Item.IsSensitive &&
        Color.TryParse(Item.TextContent?.Trim() ?? "", out var color)
            ? new SolidColorBrush(color)
            : null;

    public bool HasColorSwatch => ColorSwatch is not null;

    public string Preview
    {
        get
        {
            if (Item.IsSensitive) return "•••••••••••  (sensitive)";
            if (!string.IsNullOrEmpty(Item.Alias)) return Item.Alias!;
            var text = Item.TextContent;
            if (string.IsNullOrEmpty(text))
                return Item.HasImage ? "image" : "(empty)";
            var oneLine = text.ReplaceLineEndings(" ").Trim();
            return oneLine.Length > 200 ? oneLine[..200] + "…" : oneLine;
        }
    }

    /// <summary>Full (untruncated) text for the Detail preview pane; masked if sensitive.</summary>
    public string FullText => Item.IsSensitive
        ? "•••••••••••  (sensitive)"
        : Item.TextContent ?? "";

    public string Meta
    {
        get
        {
            var parts = new System.Collections.Generic.List<string>(5);
            if (ClipDisplay.ShowSource && !string.IsNullOrEmpty(Item.SourceApp)) parts.Add(Item.SourceApp!);
            if (ClipDisplay.ShowTime) parts.Add(FormatWhen(Item.Timestamp));
            if (ClipDisplay.ShowChars && Item is { TextLength: > 0, HasImage: false, IsSensitive: false })
                parts.Add(Item.TextLength == 1 ? "1 char" : $"{Item.TextLength:N0} chars");
            if (Item is { ImageWidth: int width, ImageHeight: int height })
                parts.Add($"{width}×{height}");
            if (Item.IsEdited) parts.Add("edited");
            return string.Join(" · ", parts);
        }
    }

    private static string FormatWhen(string timestamp)
    {
        if (!DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var ts))
            return timestamp;
        var delta = DateTimeOffset.Now - ts;
        if (delta < TimeSpan.FromMinutes(1)) return "just now";
        if (delta < TimeSpan.FromHours(1)) return $"{(int)delta.TotalMinutes}m ago";
        if (delta < TimeSpan.FromDays(1)) return $"{(int)delta.TotalHours}h ago";
        if (delta < TimeSpan.FromDays(7)) return $"{(int)delta.TotalDays}d ago";
        return ts.LocalDateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
