using Clipwell.Daemon;
using Clipwell.Protocol;
using Xunit;

namespace Clipwell.Tests;

public sealed class HistoryStoreTests : IDisposable
{
    private readonly TempDataDir _dir = new();
    private readonly MetadataStore _meta;
    private readonly HistoryStore _store;

    public HistoryStoreTests()
    {
        _meta = new MetadataStore(_dir.Path);
        // No plugin detectors: keep classification to the documented built-ins.
        _store = new HistoryStore(_meta, _dir.Path, []);
    }

    public void Dispose()
    {
        _store.Dispose();
        _dir.Dispose();
    }

    private static StoreRow Row(string timestamp, string? text, params string[] formats) => new()
    {
        Timestamp = timestamp,
        TextContent = text,
        TextLength = text?.Length ?? 0,
        Formats = formats.Length == 0 ? ["text"] : formats,
    };

    private static string At(int minutesAgo) =>
        DateTimeOffset.UtcNow.AddMinutes(-minutesAgo).ToString("o");

    // ── Upsert / dedup ──────────────────────────────────────────────────

    [Fact]
    public void Upsert_NewRow_ReportsInserted()
    {
        Assert.True(_store.Upsert(Row(At(1), "hello")));
        Assert.Single(_store.QueryPage(10, null));
    }

    [Fact]
    public void Upsert_SameTimestampAndText_MergesInsteadOfDuplicating()
    {
        var timestamp = At(1);
        Assert.True(_store.Upsert(Row(timestamp, "hello")));
        _store.Upsert(Row(timestamp, "hello"));

        Assert.Single(_store.QueryPage(10, null));
    }

    [Fact]
    public void Upsert_SameTimestampDifferentText_KeepsBothRows()
    {
        var timestamp = At(1);
        _store.Upsert(Row(timestamp, "first"));
        _store.Upsert(Row(timestamp, "second"));

        Assert.Equal(2, _store.QueryPage(10, null).Count);
    }

    [Fact]
    public void Upsert_NullTextAndEmptyText_AreDistinctAtTheSameTimestamp()
    {
        // The store hashes null as a sentinel so an empty capture and a
        // format-only capture at the same instant don't collide on the unique key.
        var timestamp = At(1);
        _store.Upsert(Row(timestamp, null, "image"));
        _store.Upsert(Row(timestamp, "", "text"));

        Assert.Equal(2, _store.QueryPage(10, null).Count);
    }

    [Fact]
    public void Upsert_MergePreservesExistingHtmlWhenTheNewCaptureHasNone()
    {
        var timestamp = At(1);
        _store.Upsert(new StoreRow
        {
            Timestamp = timestamp,
            TextContent = "hello",
            TextLength = 5,
            HtmlContent = "<b>hello</b>",
            Formats = ["text", "html"],
        });
        _store.Upsert(Row(timestamp, "hello"));

        Assert.Equal("<b>hello</b>", _store.QueryPage(10, null).Single().HtmlContent);
    }

    [Fact]
    public void Upsert_MergeNeverDowngradesHasImage()
    {
        var timestamp = At(1);
        _store.Upsert(new StoreRow
        {
            Timestamp = timestamp,
            TextContent = "shot",
            HasImage = true,
            Formats = ["image"],
        });
        _store.Upsert(Row(timestamp, "shot"));

        Assert.True(_store.QueryPage(10, null).Single().HasImage);
    }

    [Fact]
    public void Upsert_AcceptsOversizedAndBinaryLookingText()
    {
        var huge = new string('x', 2_000_000);
        var binaryish = "nul\0byte emoji \U0001F600 rtl ‮";
        _store.Upsert(Row(At(2), huge));
        _store.Upsert(Row(At(1), binaryish));

        var items = _store.QueryPage(10, null);
        Assert.Equal(2, items.Count);
        Assert.Contains(items, item => item.TextContent == binaryish);
        Assert.Contains(items, item => item.TextContent?.Length == huge.Length);
    }

    // ── Paging ──────────────────────────────────────────────────────────

    [Fact]
    public void QueryPage_ReturnsNewestFirstAndHonoursLimit()
    {
        for (var minute = 1; minute <= 5; minute++) _store.Upsert(Row(At(minute), $"item-{minute}"));

        var page = _store.QueryPage(3, null);

        Assert.Equal(3, page.Count);
        Assert.Equal("item-1", page[0].TextContent);
        Assert.Equal("item-3", page[2].TextContent);
    }

    [Fact]
    public void QueryPage_BeforeCursor_ContinuesFromWhereThePreviousPageStopped()
    {
        for (var minute = 1; minute <= 5; minute++) _store.Upsert(Row(At(minute), $"item-{minute}"));

        var first = _store.QueryPage(2, null);
        var second = _store.QueryPage(2, first[^1].Timestamp);

        Assert.Equal(["item-3", "item-4"], second.Select(item => item.TextContent));
        Assert.Empty(first.Select(item => item.Timestamp).Intersect(second.Select(item => item.Timestamp)));
    }

    [Fact]
    public void QueryPage_ItemIdsAreStableAcrossPages()
    {
        // A page-relative index would hand the first row of every page the same id.
        for (var minute = 1; minute <= 4; minute++) _store.Upsert(Row(At(minute), $"item-{minute}"));

        var first = _store.QueryPage(2, null);
        var second = _store.QueryPage(2, first[^1].Timestamp);

        Assert.Empty(first.Select(item => item.Id).Intersect(second.Select(item => item.Id)));
    }

    [Fact]
    public void QueryPage_ItemIdIsStableAcrossCalls()
    {
        _store.Upsert(Row(At(1), "hello"));

        Assert.Equal(_store.QueryPage(10, null).Single().Id, _store.QueryPage(10, null).Single().Id);
    }

    [Fact]
    public void QueryPage_EmptyStoreReturnsNoItems() => Assert.Empty(_store.QueryPage(10, null));

    // ── Classification and the edit overlay ─────────────────────────────

    [Theory]
    [InlineData("https://github.com/aylith-labs/clipwell/pull/12", "github-pr")]
    [InlineData("https://example.com/page", "url")]
    [InlineData("someone@example.com", "email")]
    [InlineData("PROJ-1234", "jira-issue")]
    [InlineData("#ff8800", "color")]
    [InlineData("/usr/local/bin/clipwell", "path")]
    [InlineData("const x = 1;", "code")]
    [InlineData("just some prose", "text")]
    public void QueryPage_ClassifiesCapturedText(string text, string expectedKind)
    {
        _store.Upsert(Row(At(1), text));

        Assert.Equal(expectedKind, _store.QueryPage(10, null).Single().Kind);
    }

    [Fact]
    public void QueryPage_EditedTextIsReclassifiedAndFlagged()
    {
        var timestamp = At(1);
        _store.Upsert(Row(timestamp, "just some prose"));
        _meta.SetEdit(timestamp, "https://example.com/page");

        var item = _store.QueryPage(10, null).Single();

        Assert.True(item.IsEdited);
        Assert.Equal("url", item.Kind);
        Assert.Equal("https://example.com/page", item.TextContent);
        Assert.Equal("https://example.com/page".Length, item.TextLength);
    }

    [Fact]
    public void QueryPage_EditDropsTheCapturedHtmlSoItCannotShadowTheEdit()
    {
        var timestamp = At(1);
        _store.Upsert(new StoreRow
        {
            Timestamp = timestamp,
            TextContent = "before",
            HtmlContent = "<b>before</b>",
            Formats = ["text", "html"],
        });
        _meta.SetEdit(timestamp, "after");

        Assert.Null(_store.QueryPage(10, null).Single().HtmlContent);
    }

    [Fact]
    public void QueryPage_ClearingTheEditRestoresTheOriginalCapture()
    {
        var timestamp = At(1);
        _store.Upsert(Row(timestamp, "original"));
        _meta.SetEdit(timestamp, "edited");
        _meta.SetEdit(timestamp, "");

        var item = _store.QueryPage(10, null).Single();

        Assert.False(item.IsEdited);
        Assert.Equal("original", item.TextContent);
    }

    [Fact]
    public void QueryPage_SurfacesPinSensitiveAndAliasFromTheOverlay()
    {
        var timestamp = At(1);
        _store.Upsert(Row(timestamp, "secret"));
        _meta.SetPinned(timestamp, true);
        _meta.SetSensitive(timestamp, true);
        _meta.SetAlias(timestamp, "  my token  ");

        var item = _store.QueryPage(10, null).Single();

        Assert.True(item.IsUserPinned);
        Assert.True(item.IsSensitive);
        Assert.Equal("my token", item.Alias);
    }

    // ── Counts ──────────────────────────────────────────────────────────

    [Fact]
    public void GetCounts_WithoutQueryCountsEverything()
    {
        _store.Upsert(Row(At(3), "https://example.com/one"));
        _store.Upsert(Row(At(2), "plain text"));
        _store.Upsert(Row(At(1), "more prose"));
        _meta.SetPinned(_store.QueryPage(10, null)[0].Timestamp, true);

        var counts = _store.GetCounts(null);

        Assert.Equal(3, counts.Total);
        Assert.Equal(1, counts.Pinned);
        Assert.Equal(0, counts.Sensitive);
        Assert.Equal(1, counts.Kinds["url"]);
        Assert.Equal(2, counts.Kinds["text"]);
    }

    [Fact]
    public void GetCounts_QueryScopesEveryCounter()
    {
        _store.Upsert(Row(At(2), "alpha match"));
        _store.Upsert(Row(At(1), "beta"));

        var counts = _store.GetCounts("alpha");

        Assert.Equal(1, counts.Total);
        Assert.Equal(1, counts.Kinds["text"]);
        Assert.False(counts.Kinds.ContainsKey("url"));
    }

    [Fact]
    public void GetCounts_QueryIsCaseInsensitiveAndMatchesAliases()
    {
        var timestamp = At(1);
        _store.Upsert(Row(timestamp, "opaque blob"));
        _meta.SetAlias(timestamp, "Deploy Key");

        Assert.Equal(1, _store.GetCounts("deploy").Total);
        Assert.Equal(1, _store.GetCounts("OPAQUE").Total);
        Assert.Equal(0, _store.GetCounts("absent").Total);
    }

    [Fact]
    public void GetCounts_BlankQueryIsTreatedAsNoQuery()
    {
        _store.Upsert(Row(At(1), "anything"));

        Assert.Equal(1, _store.GetCounts("   ").Total);
    }

    [Fact]
    public void GetCounts_CountsTheEditedTextNotTheOriginal()
    {
        var timestamp = At(1);
        _store.Upsert(Row(timestamp, "original prose"));
        _meta.SetEdit(timestamp, "https://example.com/edited");

        Assert.Equal(1, _store.GetCounts("edited").Total);
        Assert.Equal(0, _store.GetCounts("original").Total);
        Assert.Equal(1, _store.GetCounts(null).Kinds["url"]);
    }

    // ── Search ──────────────────────────────────────────────────────────

    [Fact]
    public void Search_FindsMatchesAndOrdersThemNewestFirst()
    {
        _store.Upsert(Row(At(3), "needle one"));
        _store.Upsert(Row(At(2), "haystack"));
        _store.Upsert(Row(At(1), "needle two"));

        var matches = _store.Search("needle", 10);

        Assert.Equal(["needle two", "needle one"], matches.Select(item => item.TextContent));
    }

    [Fact]
    public void Search_ReachesPastTheNewestPage()
    {
        // The MCP tools used to filter a fixed 500-row window, so an older item
        // was simply invisible. Search must see the whole history.
        for (var minute = 1; minute <= 60; minute++) _store.Upsert(Row(At(minute), $"filler-{minute}"));
        _store.Upsert(Row(At(600), "buried treasure"));

        Assert.Equal("buried treasure", Assert.Single(_store.Search("treasure", 10)).TextContent);
    }

    [Fact]
    public void Search_HonoursTheLimit()
    {
        for (var minute = 1; minute <= 5; minute++) _store.Upsert(Row(At(minute), $"match-{minute}"));

        Assert.Equal(2, _store.Search("match", 2).Count);
    }

    [Fact]
    public void Search_MatchesAliasesToo()
    {
        var timestamp = At(1);
        _store.Upsert(Row(timestamp, "opaque"));
        _meta.SetAlias(timestamp, "prod database password");

        Assert.Single(_store.Search("database", 10));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Search_BlankQueryMatchesNothingRatherThanEverything(string? query)
    {
        _store.Upsert(Row(At(1), "something"));

        Assert.Empty(_store.Search(query, 10));
    }

    [Fact]
    public void Search_NonPositiveLimitReturnsNothing()
    {
        _store.Upsert(Row(At(1), "match"));

        Assert.Empty(_store.Search("match", 0));
    }

    // ── Single-item lookup ──────────────────────────────────────────────

    [Fact]
    public void FindByTimestamp_ReturnsTheItemAndItsMetadata()
    {
        var timestamp = At(1);
        _store.Upsert(Row(timestamp, "target"));
        _meta.SetAlias(timestamp, "labelled");

        var item = _store.FindByTimestamp(timestamp);

        Assert.NotNull(item);
        Assert.Equal("target", item.TextContent);
        Assert.Equal("labelled", item.Alias);
    }

    [Fact]
    public void FindByTimestamp_ReachesPastTheNewestPage()
    {
        var buried = At(600);
        for (var minute = 1; minute <= 60; minute++) _store.Upsert(Row(At(minute), $"filler-{minute}"));
        _store.Upsert(Row(buried, "buried"));

        Assert.Equal("buried", _store.FindByTimestamp(buried)?.TextContent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2000-01-01T00:00:00.0000000+00:00")]
    [InlineData("not-a-timestamp")]
    public void FindByTimestamp_UnknownOrBlankReturnsNull(string timestamp)
    {
        _store.Upsert(Row(At(1), "present"));

        Assert.Null(_store.FindByTimestamp(timestamp));
    }

    // ── Delete / clear ──────────────────────────────────────────────────

    [Fact]
    public void DeleteByTimestamp_RemovesTheRowAndReportsIt()
    {
        var timestamp = At(1);
        _store.Upsert(Row(timestamp, "doomed"));

        Assert.True(_store.DeleteByTimestamp(timestamp));
        Assert.Empty(_store.QueryPage(10, null));
    }

    [Fact]
    public void DeleteByTimestamp_UnknownTimestampReportsNoDeletion() =>
        Assert.False(_store.DeleteByTimestamp(At(99)));

    [Fact]
    public void DeleteByTimestamp_AlsoForgetsTheItemsMetadata()
    {
        var timestamp = At(1);
        _store.Upsert(Row(timestamp, "doomed"));
        _meta.SetAlias(timestamp, "label");
        _meta.SetSensitive(timestamp, true);

        _store.DeleteByTimestamp(timestamp);

        Assert.Null(_meta.Alias(timestamp));
        Assert.False(_meta.IsSensitive(timestamp));
    }

    [Fact]
    public void ClearAll_RemovesEveryRowAndReportsTheCount()
    {
        _store.Upsert(Row(At(2), "one"));
        _store.Upsert(Row(At(1), "two"));

        Assert.Equal(2, _store.ClearAll());
        Assert.Empty(_store.QueryPage(10, null));
    }

    [Fact]
    public void ClearAll_AlsoDropsTheMetadataOverlay()
    {
        // Otherwise a later capture landing on a cleared item's timestamp would
        // inherit its alias, pin, and sensitive flag.
        var timestamp = At(1);
        _store.Upsert(Row(timestamp, "one"));
        _meta.SetPinned(timestamp, true);
        _meta.SetSensitive(timestamp, true);
        _meta.SetAlias(timestamp, "label");
        _meta.SetEdit(timestamp, "edited");

        _store.ClearAll();

        Assert.False(_meta.IsPinned(timestamp));
        Assert.False(_meta.IsSensitive(timestamp));
        Assert.Null(_meta.Alias(timestamp));
        Assert.Null(_meta.Edit(timestamp));
    }

    [Fact]
    public void ClearAll_OnAnEmptyStoreIsANoOp() => Assert.Equal(0, _store.ClearAll());

    // ── Retention sweep ─────────────────────────────────────────────────

    [Fact]
    public void SweepOlderThan_DeletesOnlyItemsPastTheCutoff()
    {
        var old = DateTimeOffset.UtcNow.AddDays(-40).ToString("o");
        var recent = DateTimeOffset.UtcNow.AddDays(-1).ToString("o");
        _store.Upsert(Row(old, "ancient"));
        _store.Upsert(Row(recent, "fresh"));

        Assert.Equal(1, _store.SweepOlderThan(30));
        Assert.Equal("fresh", _store.QueryPage(10, null).Single().TextContent);
    }

    [Fact]
    public void SweepOlderThan_SparesPinnedItems()
    {
        // Both the docs and the OpenAPI summary promise a pin keeps an item
        // "across retention".
        var old = DateTimeOffset.UtcNow.AddDays(-40).ToString("o");
        var alsoOld = DateTimeOffset.UtcNow.AddDays(-50).ToString("o");
        _store.Upsert(Row(old, "pinned keeper"));
        _store.Upsert(Row(alsoOld, "expendable"));
        _meta.SetPinned(old, true);

        Assert.Equal(1, _store.SweepOlderThan(30));
        Assert.Equal("pinned keeper", _store.QueryPage(10, null).Single().TextContent);
    }

    [Fact]
    public void SweepOlderThan_ForgetsMetadataOfTheItemsItPurges()
    {
        var old = DateTimeOffset.UtcNow.AddDays(-40).ToString("o");
        _store.Upsert(Row(old, "ancient"));
        _meta.SetAlias(old, "label");

        _store.SweepOlderThan(30);

        Assert.Null(_meta.Alias(old));
    }

    [Fact]
    public void SweepOlderThan_NullRetentionKeepsEverythingForever()
    {
        _store.Upsert(Row(DateTimeOffset.UtcNow.AddDays(-4000).ToString("o"), "prehistoric"));

        Assert.Equal(0, _store.SweepOlderThan(null));
        Assert.Single(_store.QueryPage(10, null));
    }

    [Fact]
    public void SweepOlderThan_NegativeRetentionDeletesNothing()
    {
        _store.Upsert(Row(At(1), "recent"));

        Assert.Equal(0, _store.SweepOlderThan(-1));
        Assert.Single(_store.QueryPage(10, null));
    }

    [Fact]
    public void SweepOlderThan_ComparesInstantsNotRawStrings()
    {
        // This row is an hour past the 30-day cutoff, but it is written with a
        // +05:00 offset, so its wall-clock text reads four hours *after* the
        // UTC-formatted cutoff. Comparing the raw strings keeps it; comparing the
        // instants they denote deletes it.
        var expired = new DateTimeOffset(
            DateTimeOffset.UtcNow.AddDays(-30).AddHours(-1).UtcDateTime, TimeSpan.Zero)
            .ToOffset(TimeSpan.FromHours(5));
        var cutoffText = DateTimeOffset.UtcNow.AddDays(-30).ToString("o");
        var expiredText = expired.ToString("o");
        Assert.True(
            string.CompareOrdinal(expiredText, cutoffText) > 0,
            "the fixture must be one a plain string comparison would get wrong");

        _store.Upsert(Row(expiredText, "offset-encoded expired"));
        _store.Upsert(Row(At(1), "fresh"));

        Assert.Equal(1, _store.SweepOlderThan(30));
        Assert.Equal("fresh", _store.QueryPage(10, null).Single().TextContent);
    }

    [Fact]
    public void SweepOlderThan_KeepsAnOffsetEncodedRowThatIsStillInsideRetention()
    {
        // The mirror of the case above: instant comparison must not over-delete
        // either. This row's text sorts before the cutoff, but it is only a day old.
        var recent = new DateTimeOffset(
            DateTimeOffset.UtcNow.AddDays(-1).UtcDateTime, TimeSpan.Zero)
            .ToOffset(TimeSpan.FromHours(-11));
        _store.Upsert(Row(recent.ToString("o"), "offset-encoded recent"));

        Assert.Equal(0, _store.SweepOlderThan(1));
        Assert.Single(_store.QueryPage(10, null));
    }

    [Fact]
    public void SweepOlderThan_KeepsRowsWhoseTimestampCannotBeParsed()
    {
        // Never guess on a destructive path: an unparseable timestamp is kept.
        _store.Upsert(Row("not-a-timestamp", "unparseable"));

        Assert.Equal(0, _store.SweepOlderThan(30));
        Assert.Single(_store.QueryPage(10, null));
    }

    // ── Settings ────────────────────────────────────────────────────────

    [Fact]
    public void LoadSettings_WithNoFileReturnsDefaults()
    {
        var settings = _store.LoadSettings();

        Assert.Equal(30, settings.RetentionDays);
        Assert.Equal("system", settings.Theme);
        Assert.Equal("Alt+Shift+V", settings.Hotkey);
    }

    [Fact]
    public void SaveSettings_RoundTrips()
    {
        _store.SaveSettings(new ClipboardSettings
        {
            RetentionDays = 7,
            Theme = "dark",
            OpenAtCursor = true,
            DefaultView = "detail",
        });

        var reloaded = _store.LoadSettings();

        Assert.Equal(7, reloaded.RetentionDays);
        Assert.Equal("dark", reloaded.Theme);
        Assert.True(reloaded.OpenAtCursor);
        Assert.Equal("detail", reloaded.DefaultView);
    }

    [Fact]
    public void SaveSettings_ForeverRetentionRoundTripsAsNull()
    {
        _store.SaveSettings(new ClipboardSettings { RetentionDays = null });

        Assert.Null(_store.LoadSettings().RetentionDays);
    }

    [Fact]
    public void LoadSettings_CorruptFileFallsBackToDefaults()
    {
        File.WriteAllText(_dir.File("clipboard-settings.json"), "{ this is not json");

        Assert.Equal(30, _store.LoadSettings().RetentionDays);
    }

    [Fact]
    public void LoadSettings_OutOfRangeRetentionFallsBackToDefaults()
    {
        File.WriteAllText(_dir.File("clipboard-settings.json"), """{"retentionDays": 99999}""");

        Assert.Equal(30, _store.LoadSettings().RetentionDays);
    }

    // ── Image paths ─────────────────────────────────────────────────────

    [Fact]
    public void GetImagePath_ReturnsThePathRecordedForTheCapture()
    {
        var timestamp = At(1);
        var imagePath = Path.Combine(_store.CacheDir, "shot.png");
        _store.Upsert(new StoreRow
        {
            Timestamp = timestamp,
            HasImage = true,
            ImagePath = imagePath,
            Formats = ["image"],
        });

        Assert.Equal(imagePath, _store.GetImagePath(timestamp));
    }

    [Fact]
    public void GetImagePath_TextOnlyItemHasNone()
    {
        var timestamp = At(1);
        _store.Upsert(Row(timestamp, "no image here"));

        Assert.Null(_store.GetImagePath(timestamp));
    }

    [Fact]
    public void QueryPage_ReadsPixelSizeFromTheCachedPng()
    {
        var timestamp = At(1);
        var imagePath = Path.Combine(_store.CacheDir, "shot.png");
        File.WriteAllBytes(imagePath, PngHeader(width: 640, height: 480));
        _store.Upsert(new StoreRow
        {
            Timestamp = timestamp,
            HasImage = true,
            ImagePath = imagePath,
            Formats = ["image"],
        });

        var item = _store.QueryPage(10, null).Single();

        Assert.Equal(640, item.ImageWidth);
        Assert.Equal(480, item.ImageHeight);
        Assert.Equal("image", item.Kind);
    }

    [Fact]
    public void QueryPage_MissingCacheFileLeavesDimensionsUnset()
    {
        _store.Upsert(new StoreRow
        {
            Timestamp = At(1),
            HasImage = true,
            ImagePath = Path.Combine(_store.CacheDir, "gone.png"),
            Formats = ["image"],
        });

        var item = _store.QueryPage(10, null).Single();

        Assert.Null(item.ImageWidth);
        Assert.Null(item.ImageHeight);
    }

    [Fact]
    public void QueryPage_TruncatedOrNonPngCacheFileLeavesDimensionsUnset()
    {
        var truncated = Path.Combine(_store.CacheDir, "truncated.png");
        var notPng = Path.Combine(_store.CacheDir, "notreally.png");
        File.WriteAllBytes(truncated, PngHeader(1, 1)[..10]);
        File.WriteAllBytes(notPng, "GIF89a-this-is-not-a-png-at-all"u8.ToArray());
        _store.Upsert(new StoreRow
        {
            Timestamp = At(2), HasImage = true, ImagePath = truncated, Formats = ["image"],
        });
        _store.Upsert(new StoreRow
        {
            Timestamp = At(1), HasImage = true, ImagePath = notPng, Formats = ["image"],
        });

        Assert.All(_store.QueryPage(10, null), item => Assert.Null(item.ImageWidth));
    }

    [Fact]
    public void QueryPage_ZeroSizedPngIsRejectedRatherThanReportedAsZeroByZero()
    {
        var imagePath = Path.Combine(_store.CacheDir, "empty.png");
        File.WriteAllBytes(imagePath, PngHeader(0, 0));
        _store.Upsert(new StoreRow
        {
            Timestamp = At(1), HasImage = true, ImagePath = imagePath, Formats = ["image"],
        });

        Assert.Null(_store.QueryPage(10, null).Single().ImageWidth);
    }

    /// <summary>A PNG signature plus an IHDR carrying the given dimensions.</summary>
    private static byte[] PngHeader(int width, int height)
    {
        var bytes = new byte[24];
        ReadOnlySpan<byte> signature = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];
        signature.CopyTo(bytes);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(16, 4), width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(bytes.AsSpan(20, 4), height);
        return bytes;
    }

    // ── Corrupt rows ────────────────────────────────────────────────────

    [Fact]
    public void QueryPage_CorruptFormatsJsonYieldsAnEmptyFormatListRatherThanThrowing()
    {
        using var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={_store.DbPath}");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "INSERT INTO items (timestamp, text_content, text_length, formats_json, text_sha1) " +
            "VALUES ($ts, 'text', 4, '{not json', 'sha')";
        command.Parameters.AddWithValue("$ts", At(1));
        command.ExecuteNonQuery();

        Assert.Empty(_store.QueryPage(10, null).Single().Formats);
    }
}
