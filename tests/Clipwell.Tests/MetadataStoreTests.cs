using Clipwell.Daemon;
using Xunit;

namespace Clipwell.Tests;

public sealed class MetadataStoreTests : IDisposable
{
    private const string Timestamp = "2026-06-14T01:23:45.6789012+00:00";

    private readonly TempDataDir _dir = new();
    private readonly MetadataStore _meta;

    public MetadataStoreTests() => _meta = new MetadataStore(_dir.Path);

    public void Dispose() => _dir.Dispose();

    private MetadataStore Reopen() => new(_dir.Path);

    // ── Pins ────────────────────────────────────────────────────────────

    [Fact]
    public void IsPinned_DefaultsToFalse() => Assert.False(_meta.IsPinned(Timestamp));

    [Fact]
    public void SetPinned_PinsAndUnpins()
    {
        _meta.SetPinned(Timestamp, true);
        Assert.True(_meta.IsPinned(Timestamp));

        _meta.SetPinned(Timestamp, false);
        Assert.False(_meta.IsPinned(Timestamp));
    }

    [Fact]
    public void SetPinned_SurvivesAReopen()
    {
        _meta.SetPinned(Timestamp, true);

        Assert.True(Reopen().IsPinned(Timestamp));
    }

    [Fact]
    public void PinnedTimestamps_ListsExactlyThePinnedItems()
    {
        _meta.SetPinned("a", true);
        _meta.SetPinned("b", true);
        _meta.SetPinned("b", false);

        Assert.Equal(["a"], _meta.PinnedTimestamps());
    }

    [Fact]
    public void PinnedTimestamps_IsASnapshotNotALiveView()
    {
        _meta.SetPinned("a", true);
        var snapshot = _meta.PinnedTimestamps();
        _meta.SetPinned("b", true);

        Assert.Single(snapshot);
    }

    // ── Sensitive ───────────────────────────────────────────────────────

    [Fact]
    public void SetSensitive_MarksAndUnmarks()
    {
        _meta.SetSensitive(Timestamp, true);
        Assert.True(_meta.IsSensitive(Timestamp));

        _meta.SetSensitive(Timestamp, false);
        Assert.False(_meta.IsSensitive(Timestamp));
    }

    [Fact]
    public void SetSensitive_SurvivesAReopen()
    {
        _meta.SetSensitive(Timestamp, true);

        Assert.True(Reopen().IsSensitive(Timestamp));
    }

    // ── Aliases ─────────────────────────────────────────────────────────

    [Fact]
    public void SetAlias_StoresATrimmedAlias()
    {
        _meta.SetAlias(Timestamp, "   deploy key   ");

        Assert.Equal("deploy key", _meta.Alias(Timestamp));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetAlias_BlankClearsTheAlias(string? alias)
    {
        _meta.SetAlias(Timestamp, "something");
        _meta.SetAlias(Timestamp, alias);

        Assert.Null(_meta.Alias(Timestamp));
    }

    [Fact]
    public void SetAlias_SurvivesAReopen()
    {
        _meta.SetAlias(Timestamp, "label");

        Assert.Equal("label", Reopen().Alias(Timestamp));
    }

    // ── Edits ───────────────────────────────────────────────────────────

    [Fact]
    public void SetEdit_StoresTheOverrideVerbatimIncludingWhitespace()
    {
        _meta.SetEdit(Timestamp, "  indented\n\tcode  ");

        Assert.Equal("  indented\n\tcode  ", _meta.Edit(Timestamp));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SetEdit_NullOrEmptyRestoresTheOriginal(string? text)
    {
        _meta.SetEdit(Timestamp, "edited");
        _meta.SetEdit(Timestamp, text);

        Assert.Null(_meta.Edit(Timestamp));
    }

    [Fact]
    public void SetEdit_WhitespaceIsAMeaningfulOverrideNotAClear()
    {
        // Only null/empty restore; a space is text the user typed.
        _meta.SetEdit(Timestamp, " ");

        Assert.Equal(" ", _meta.Edit(Timestamp));
    }

    [Fact]
    public void SetEdit_SurvivesAReopen()
    {
        _meta.SetEdit(Timestamp, "edited");

        Assert.Equal("edited", Reopen().Edit(Timestamp));
    }

    // ── Forget / clear ──────────────────────────────────────────────────

    [Fact]
    public void Forget_DropsEveryKindOfMetadataForOneTimestamp()
    {
        _meta.SetPinned(Timestamp, true);
        _meta.SetSensitive(Timestamp, true);
        _meta.SetAlias(Timestamp, "label");
        _meta.SetEdit(Timestamp, "edited");

        _meta.Forget(Timestamp);

        Assert.False(_meta.IsPinned(Timestamp));
        Assert.False(_meta.IsSensitive(Timestamp));
        Assert.Null(_meta.Alias(Timestamp));
        Assert.Null(_meta.Edit(Timestamp));
    }

    [Fact]
    public void Forget_LeavesOtherTimestampsAlone()
    {
        _meta.SetPinned("keep", true);
        _meta.SetPinned("drop", true);

        _meta.Forget("drop");

        Assert.True(_meta.IsPinned("keep"));
    }

    [Fact]
    public void Clear_DropsEverythingAndPersists()
    {
        _meta.SetPinned("a", true);
        _meta.SetSensitive("b", true);
        _meta.SetAlias("c", "label");
        _meta.SetEdit("d", "edited");

        _meta.Clear();

        var reopened = Reopen();
        Assert.Empty(reopened.PinnedTimestamps());
        Assert.False(reopened.IsSensitive("b"));
        Assert.Null(reopened.Alias("c"));
        Assert.Null(reopened.Edit("d"));
    }

    // ── Durability ──────────────────────────────────────────────────────

    [Fact]
    public void Load_CorruptFileStartsEmptyInsteadOfThrowing()
    {
        File.WriteAllText(_dir.File("clipboard-meta.json"), "{ truncated");

        var reopened = Reopen();

        Assert.False(reopened.IsPinned(Timestamp));
        Assert.Empty(reopened.PinnedTimestamps());
    }

    [Fact]
    public void Load_EmptyFileStartsEmptyInsteadOfThrowing()
    {
        File.WriteAllText(_dir.File("clipboard-meta.json"), "");

        Assert.Empty(Reopen().PinnedTimestamps());
    }

    [Fact]
    public void Save_LeavesNoTempFileBehind()
    {
        _meta.SetPinned(Timestamp, true);

        Assert.False(File.Exists(_dir.File("clipboard-meta.json.tmp")));
    }

    [Fact]
    public void Save_ReplacesRatherThanAppends()
    {
        _meta.SetAlias(Timestamp, "first");
        _meta.SetAlias(Timestamp, "second");

        var contents = File.ReadAllText(_dir.File("clipboard-meta.json"));

        Assert.DoesNotContain("first", contents, StringComparison.Ordinal);
        Assert.Contains("second", contents, StringComparison.Ordinal);
    }

    [Fact]
    public void ConcurrentWritersDoNotLoseUpdatesOrCorruptTheFile()
    {
        Parallel.For(0, 200, index => _meta.SetPinned($"ts-{index}", true));

        Assert.Equal(200, _meta.PinnedTimestamps().Count);
        Assert.Equal(200, Reopen().PinnedTimestamps().Count);
    }
}
