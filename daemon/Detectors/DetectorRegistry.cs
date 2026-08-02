using System.Text.RegularExpressions;
using Clipwell.Protocol;
using Clipwell.Protocol.Plugins;

namespace Clipwell.Daemon.Detectors;

/// <summary>
/// Holds the ordered set of <see cref="IClipDetector"/>s and classifies an item
/// into a <c>Kind</c>. Built-in detectors ship here; plugins can add more (Phase 2).
/// </summary>
public sealed class DetectorRegistry
{
    private readonly IReadOnlyList<IClipDetector> _detectors;

    public DetectorRegistry(IEnumerable<IClipDetector>? extra = null)
    {
        var all = new List<IClipDetector>
        {
            new ImageDetector(),
            new GitHubPrDetector(),
            new UrlDetector(),
            new EmailDetector(),
            new JiraIssueDetector(),
            new ColorDetector(),
            new PathDetector(),
            new CodeDetector(),
        };
        if (extra is not null) all.AddRange(extra.Where(detector => detector is not null));
        _detectors = all.OrderBy(detector => detector.Priority).ToList();
    }

    /// <summary>
    /// Returns the first matching kind, or "text" as the fallback. A plugin
    /// detector that throws or returns a blank kind is skipped: classification
    /// runs on every read, so one bad plugin must not fail the whole history.
    /// </summary>
    public string Classify(ClipItem item)
    {
        foreach (var detector in _detectors)
        {
            string? kind;
            try
            {
                kind = detector.Detect(item);
            }
            catch (Exception ex)
            {
                Failed?.Invoke(detector.Id, ex);
                continue;
            }
            if (!string.IsNullOrWhiteSpace(kind)) return kind;
        }
        return "text";
    }

    /// <summary>Raised with the detector id when a detector throws during classification.</summary>
    public event Action<string, Exception>? Failed;
}

internal sealed class ImageDetector : IClipDetector
{
    public string Id => "builtin.image";
    public int Priority => 0;
    public string? Detect(ClipItem item) => item.HasImage ? "image" : null;
}

internal sealed partial class GitHubPrDetector : IClipDetector
{
    public string Id => "builtin.github-pr";
    public int Priority => 5; // before the generic URL detector
    public string? Detect(ClipItem item) =>
        item.TextContent is { } text && GitHubPrRegex().IsMatch(text.Trim()) ? "github-pr" : null;

    [GeneratedRegex(@"^https?://github\.com/[^/\s]+/[^/\s]+/pull/\d+", RegexOptions.IgnoreCase)]
    private static partial Regex GitHubPrRegex();
}

internal sealed partial class JiraIssueDetector : IClipDetector
{
    public string Id => "builtin.jira-issue";
    public int Priority => 25;
    public string? Detect(ClipItem item) =>
        item.TextContent is { } text && JiraRegex().IsMatch(text.Trim()) ? "jira-issue" : null;

    [GeneratedRegex(@"^[A-Z][A-Z0-9]+-\d+$")]
    private static partial Regex JiraRegex();
}

internal sealed partial class UrlDetector : IClipDetector
{
    public string Id => "builtin.url";
    public int Priority => 10;
    public string? Detect(ClipItem item) =>
        item.TextContent is { } text && UrlRegex().IsMatch(text.Trim()) ? "url" : null;

    [GeneratedRegex(@"^https?://\S+$", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();
}

internal sealed partial class EmailDetector : IClipDetector
{
    public string Id => "builtin.email";
    public int Priority => 20;
    public string? Detect(ClipItem item) =>
        item.TextContent is { } text && EmailRegex().IsMatch(text.Trim()) ? "email" : null;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}

internal sealed partial class ColorDetector : IClipDetector
{
    public string Id => "builtin.color";
    public int Priority => 30;
    public string? Detect(ClipItem item) =>
        item.TextContent is { } text && ColorRegex().IsMatch(text.Trim()) ? "color" : null;

    [GeneratedRegex(@"^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$")]
    private static partial Regex ColorRegex();
}

internal sealed partial class PathDetector : IClipDetector
{
    public string Id => "builtin.path";
    public int Priority => 40;
    public string? Detect(ClipItem item)
    {
        if (item.TextContent is not { } raw) return null;
        var text = raw.Trim();
        if (text.Contains('\n')) return null;
        // Windows drive path, UNC, or POSIX absolute path.
        return WinPath().IsMatch(text) || text.StartsWith(@"\\") || PosixPath().IsMatch(text)
            ? "path"
            : null;
    }

    [GeneratedRegex(@"^[a-zA-Z]:[\\/].+")]
    private static partial Regex WinPath();

    [GeneratedRegex(@"^/(?:[^/\0]+/)*[^/\0]+$")]
    private static partial Regex PosixPath();
}

internal sealed partial class CodeDetector : IClipDetector
{
    public string Id => "builtin.code";
    public int Priority => 50;

    public string? Detect(ClipItem item)
    {
        if (item.TextContent is not { } text || text.Length < 3) return null;

        // An arrow, a closing tag, or a function/def declaration is essentially
        // never prose, so any one of them is enough on its own.
        if (text.Contains("=>", StringComparison.Ordinal) ||
            text.Contains("</", StringComparison.Ordinal) ||
            DeclarationRegex().IsMatch(text))
            return "code";

        // A brace on its own is weak — "{redacted}" is prose — so it needs a
        // semicolon or an indented line to corroborate it.
        if (text.Contains(';', StringComparison.Ordinal)) return "code";
        if (!text.Contains('{', StringComparison.Ordinal)) return null;
        var indented = text.Split('\n')
            .Count(line => line.StartsWith("    ", StringComparison.Ordinal) || line.StartsWith('\t'));
        return indented > 0 ? "code" : null;
    }

    [GeneratedRegex(@"\b(?:function|def|class)\s+\w+\s*[\(:]")]
    private static partial Regex DeclarationRegex();
}
