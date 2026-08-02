using System.ComponentModel;
using System.Text;
using Clipwell.Protocol;
using ModelContextProtocol.Server;

namespace Clipwell.Daemon.Mcp;

/// <summary>
/// MCP tools served over HTTP/SSE directly by the daemon (see <c>app.MapMcp</c>).
/// Unlike the stdio server in <c>mcp/</c> — which proxies to the REST API — these
/// talk to <see cref="HistoryStore"/> in-process, so no second hop is involved.
/// The tool surface is kept identical to the stdio server's.
/// </summary>
[McpServerToolType]
public sealed class DaemonClipboardTools(HistoryStore store)
{
    [McpServerTool(Name = "clipboard_recent")]
    [Description("List the most recent clipboard history items, newest first. Returns timestamp, kind, source app, and a text preview for each.")]
    public string Recent(
        [Description("Maximum number of items to return (1-200).")] int limit = 20)
        => Format(store.QueryPage(Math.Clamp(limit, 1, 200), null));

    [McpServerTool(Name = "clipboard_search")]
    [Description("Search clipboard history for items whose text contains the query (case-insensitive). Returns matching items newest first.")]
    public string Search(
        [Description("Text to search for within clipboard items.")] string query,
        [Description("Maximum number of matches to return (1-200).")] int limit = 50)
    {
        if (string.IsNullOrWhiteSpace(query)) return "Provide a search query.";
        var matches = store.Search(query, Math.Clamp(limit, 1, 200));
        return matches.Count == 0 ? $"No clipboard items matching \"{query}\"." : Format(matches);
    }

    [McpServerTool(Name = "clipboard_get_text")]
    [Description("Get the full, untruncated text of a clipboard item by its exact timestamp (as returned by clipboard_recent/clipboard_search).")]
    public string GetText(
        [Description("The item's timestamp, e.g. 2026-06-14T01:23:45.6789012+00:00.")] string timestamp)
    {
        if (string.IsNullOrWhiteSpace(timestamp)) return "Provide an item timestamp.";
        var item = store.FindByTimestamp(timestamp);
        if (item is null) return "No clipboard item with that timestamp.";
        return item.TextContent ?? (item.HasImage ? "(image item — no text)" : "(empty)");
    }

    [McpServerTool(Name = "clipboard_clear")]
    [Description("Delete ALL clipboard history. This is irreversible.")]
    public string Clear()
    {
        var deleted = store.ClearAll();
        return $"Clipboard history cleared ({deleted} items).";
    }

    private static string Format(IReadOnlyList<ClipItem> items)
    {
        var builder = new StringBuilder();
        foreach (var item in items)
        {
            var preview = (item.TextContent ?? (item.HasImage ? "<image>" : "<empty>"))
                .ReplaceLineEndings(" ").Trim();
            if (preview.Length > 120) preview = preview[..120] + "…";
            var source = string.IsNullOrEmpty(item.SourceApp) ? "" : $" [{item.SourceApp}]";
            builder.AppendLine($"{item.Timestamp}{source}: {preview}");
        }
        return builder.ToString().TrimEnd();
    }
}
