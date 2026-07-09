namespace Clipwell.Protocol;

/// <summary>
/// Aggregate counts over the clipboard history, optionally scoped to a search
/// query. Backs the pickers' filter pills ("All 128 · Pinned 12 · Sensitive 3")
/// and per-kind dropdown counts.
/// </summary>
public sealed record ClipCounts
{
    public int Total { get; init; }

    public int Pinned { get; init; }

    public int Sensitive { get; init; }

    /// <summary>Item count per detector kind (e.g. <c>url</c>, <c>code</c>).</summary>
    public IReadOnlyDictionary<string, int> Kinds { get; init; } =
        new Dictionary<string, int>();
}
