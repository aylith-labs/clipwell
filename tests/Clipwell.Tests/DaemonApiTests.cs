using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Clipwell.Protocol;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Clipwell.Tests;

/// <summary>
/// Boots the real daemon over a throwaway data dir and drives its public HTTP
/// surface. The env vars the app reads at startup are process-wide, so every test
/// here shares one host — see <see cref="DaemonApiCollection"/>.
/// </summary>
public sealed class DaemonFixture : WebApplicationFactory<Program>
{
    public DaemonFixture()
    {
        DataDir = new TempDataDir();
        // Never touch the developer's real clipboard: isolate the DB, keep the
        // OS watcher off, and disable the destructive retention sweep.
        Environment.SetEnvironmentVariable("CLIPWELL_DATA_DIR", DataDir.Path);
        Environment.SetEnvironmentVariable("CLIPWELL_NO_WATCH", "1");
        Environment.SetEnvironmentVariable("CLIPWELL_NO_SWEEP", "1");
        Environment.SetEnvironmentVariable("CLIPWELL_ALLOW_SEED", "1");
        Environment.SetEnvironmentVariable("CLIPWELL_PLUGINS_DIR", Path.Combine(DataDir.Path, "plugins"));
    }

    public TempDataDir DataDir { get; }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) DataDir.Dispose();
    }
}

[CollectionDefinition(nameof(DaemonApiCollection), DisableParallelization = true)]
public sealed class DaemonApiCollection : ICollectionFixture<DaemonFixture>;

[Collection(nameof(DaemonApiCollection))]
public sealed class DaemonApiTests(DaemonFixture fixture)
{
    private HttpClient Client() => fixture.CreateClient();

    private async Task<string> SeedAsync(HttpClient client, string text, int minutesAgo = 1)
    {
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(-minutesAgo).ToString("o");
        var response = await client.PostAsJsonAsync(
            "/api/clipboard/_seed",
            new { timestamp, text, hasImage = false, imagePath = (string?)null, sourceApp = "tests" },
            TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return timestamp;
    }

    private async Task ResetAsync(HttpClient client) =>
        (await client.PostAsync("/api/clipboard/clear", null, TestContext.Current.CancellationToken))
            .EnsureSuccessStatusCode();

    private static async Task<JsonElement> JsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))
            .RootElement.Clone();

    // ── Health ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Health_ReportsOkAndTheResolvedDatabasePath()
    {
        using var client = Client();

        var body = await JsonAsync(await client.GetAsync("/health", TestContext.Current.CancellationToken));

        Assert.Equal("ok", body.GetProperty("status").GetString());
        Assert.StartsWith(fixture.DataDir.Path, body.GetProperty("db").GetString()!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Health_ConfirmsTheTestHostIsNotPointedAtRealHistory()
    {
        using var client = Client();

        var body = await JsonAsync(await client.GetAsync("/health", TestContext.Current.CancellationToken));

        Assert.DoesNotContain(
            "Clipwell" + Path.DirectorySeparatorChar + "history.db",
            body.GetProperty("db").GetString()!,
            StringComparison.Ordinal);
    }

    // ── History ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistory_ReturnsSeededItemsNewestFirst()
    {
        using var client = Client();
        await ResetAsync(client);
        await SeedAsync(client, "older", minutesAgo: 5);
        await SeedAsync(client, "newer", minutesAgo: 1);

        var body = await JsonAsync(
            await client.GetAsync("/api/clipboard", TestContext.Current.CancellationToken));
        var items = body.GetProperty("items").EnumerateArray().ToList();

        Assert.Equal(2, items.Count);
        Assert.Equal("newer", items[0].GetProperty("textContent").GetString());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(999_999)]
    public async Task GetHistory_OutOfRangeLimitIsClampedRatherThanRejected(int limit)
    {
        using var client = Client();
        await ResetAsync(client);
        await SeedAsync(client, "only");

        var response = await client.GetAsync(
            $"/api/clipboard?limit={limit}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotEmpty((await JsonAsync(response)).GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task GetHistory_NonNumericLimitIsRejected()
    {
        using var client = Client();

        var response = await client.GetAsync(
            "/api/clipboard?limit=abc", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Counts ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCounts_ReflectsSeededContentAndTheQuery()
    {
        using var client = Client();
        await ResetAsync(client);
        await SeedAsync(client, "https://example.com/one", minutesAgo: 2);
        await SeedAsync(client, "plain prose", minutesAgo: 1);

        var all = await JsonAsync(
            await client.GetAsync("/api/clipboard/counts", TestContext.Current.CancellationToken));
        var scoped = await JsonAsync(
            await client.GetAsync("/api/clipboard/counts?q=prose", TestContext.Current.CancellationToken));

        Assert.Equal(2, all.GetProperty("total").GetInt32());
        Assert.Equal(1, all.GetProperty("kinds").GetProperty("url").GetInt32());
        Assert.Equal(1, scoped.GetProperty("total").GetInt32());
    }

    // ── Search / single item ────────────────────────────────────────────

    [Fact]
    public async Task Search_FindsAMatch()
    {
        using var client = Client();
        await ResetAsync(client);
        await SeedAsync(client, "the needle is here");

        var body = await JsonAsync(
            await client.GetAsync("/api/clipboard/search?q=needle", TestContext.Current.CancellationToken));

        Assert.Single(body.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task Search_ReportsNoMatchesAsAnEmptyListNotAnError()
    {
        using var client = Client();
        await ResetAsync(client);
        await SeedAsync(client, "something");

        var response = await client.GetAsync(
            "/api/clipboard/search?q=absent", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty((await JsonAsync(response)).GetProperty("items").EnumerateArray());
    }

    [Theory]
    [InlineData("/api/clipboard/search")]
    [InlineData("/api/clipboard/search?q=")]
    [InlineData("/api/clipboard/search?q=%20%20")]
    public async Task Search_BlankQueryIsRejected(string url)
    {
        using var client = Client();

        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await client.GetAsync(url, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task GetItem_ReturnsTheItemForAKnownTimestamp()
    {
        using var client = Client();
        await ResetAsync(client);
        var timestamp = await SeedAsync(client, "target");

        var body = await JsonAsync(await client.GetAsync(
            $"/api/clipboard/item/{Uri.EscapeDataString(timestamp)}", TestContext.Current.CancellationToken));

        Assert.Equal("target", body.GetProperty("textContent").GetString());
    }

    [Fact]
    public async Task GetItem_UnknownTimestampIsNotFound()
    {
        using var client = Client();

        var response = await client.GetAsync(
            "/api/clipboard/item/2000-01-01T00%3A00%3A00Z", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── Settings validation, both directions ────────────────────────────

    [Theory]
    [InlineData(7)]
    [InlineData(30)]
    [InlineData(90)]
    [InlineData(null)]
    public async Task SaveSettings_AcceptsEveryValidRetention(int? retentionDays)
    {
        using var client = Client();

        var response = await client.PostAsJsonAsync(
            "/api/clipboard/settings",
            new ClipboardSettings { RetentionDays = retentionDays },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reloaded = await client.GetFromJsonAsync<ClipboardSettings>(
            "/api/clipboard/settings", TestContext.Current.CancellationToken);
        Assert.Equal(retentionDays, reloaded!.RetentionDays);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-30)]
    [InlineData(99_999)]
    public async Task SaveSettings_RejectsAnOutOfRangeRetention(int retentionDays)
    {
        using var client = Client();
        await client.PostAsJsonAsync(
            "/api/clipboard/settings",
            new ClipboardSettings { RetentionDays = 30 },
            TestContext.Current.CancellationToken);

        var response = await client.PostAsJsonAsync(
            "/api/clipboard/settings",
            new ClipboardSettings { RetentionDays = retentionDays },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // The rejected value must not have been persisted.
        var reloaded = await client.GetFromJsonAsync<ClipboardSettings>(
            "/api/clipboard/settings", TestContext.Current.CancellationToken);
        Assert.Equal(30, reloaded!.RetentionDays);
    }

    [Fact]
    public async Task SaveSettings_RoundTripsTheUiPreferences()
    {
        using var client = Client();

        await client.PostAsJsonAsync(
            "/api/clipboard/settings",
            new ClipboardSettings { RetentionDays = 90, Theme = "dark", OpenAtCursor = true },
            TestContext.Current.CancellationToken);

        var reloaded = await client.GetFromJsonAsync<ClipboardSettings>(
            "/api/clipboard/settings", TestContext.Current.CancellationToken);
        Assert.Equal("dark", reloaded!.Theme);
        Assert.True(reloaded.OpenAtCursor);
    }

    // ── Mutations: required-field validation, both directions ───────────

    [Fact]
    public async Task DeleteItem_RemovesAKnownItem()
    {
        using var client = Client();
        await ResetAsync(client);
        var timestamp = await SeedAsync(client, "doomed");

        var body = await JsonAsync(await client.PostAsJsonAsync(
            "/api/clipboard/delete", new { timestamp }, TestContext.Current.CancellationToken));

        Assert.True(body.GetProperty("deleted").GetBoolean());
    }

    [Fact]
    public async Task DeleteItem_MissingTimestampIsRejectedRatherThanCrashing()
    {
        // A null timestamp used to reach the SQLite parameter binder and surface
        // as a 500.
        using var client = Client();

        var response = await client.PostAsJsonAsync(
            "/api/clipboard/delete", new { timestamp = (string?)null }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteItem_EmptyTimestampIsRejected()
    {
        using var client = Client();

        var response = await client.PostAsJsonAsync(
            "/api/clipboard/delete", new { timestamp = "" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/clipboard/pin")]
    [InlineData("/api/clipboard/sensitive")]
    [InlineData("/api/clipboard/rename")]
    [InlineData("/api/clipboard/edit")]
    [InlineData("/api/clipboard/delete")]
    public async Task Mutations_RejectABlankTimestamp(string url)
    {
        using var client = Client();

        var response = await client.PostAsJsonAsync(
            url, new { timestamp = "" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PinItem_PinningIsReflectedInTheItemAndTheCounts()
    {
        using var client = Client();
        await ResetAsync(client);
        var timestamp = await SeedAsync(client, "keeper");

        var response = await client.PostAsJsonAsync(
            "/api/clipboard/pin", new { timestamp, pinned = true }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var counts = await JsonAsync(
            await client.GetAsync("/api/clipboard/counts", TestContext.Current.CancellationToken));
        Assert.Equal(1, counts.GetProperty("pinned").GetInt32());
    }

    [Fact]
    public async Task PinItem_UnpinningClearsIt()
    {
        using var client = Client();
        await ResetAsync(client);
        var timestamp = await SeedAsync(client, "keeper");
        await client.PostAsJsonAsync(
            "/api/clipboard/pin", new { timestamp, pinned = true }, TestContext.Current.CancellationToken);

        await client.PostAsJsonAsync(
            "/api/clipboard/pin", new { timestamp, pinned = false }, TestContext.Current.CancellationToken);

        var counts = await JsonAsync(
            await client.GetAsync("/api/clipboard/counts", TestContext.Current.CancellationToken));
        Assert.Equal(0, counts.GetProperty("pinned").GetInt32());
    }

    [Fact]
    public async Task MarkSensitive_IsReflectedInTheCounts()
    {
        using var client = Client();
        await ResetAsync(client);
        var timestamp = await SeedAsync(client, "secret");

        await client.PostAsJsonAsync(
            "/api/clipboard/sensitive",
            new { timestamp, sensitive = true },
            TestContext.Current.CancellationToken);

        var counts = await JsonAsync(
            await client.GetAsync("/api/clipboard/counts", TestContext.Current.CancellationToken));
        Assert.Equal(1, counts.GetProperty("sensitive").GetInt32());
    }

    [Fact]
    public async Task EditItem_OverridesTheTextAndRestoresItWhenCleared()
    {
        using var client = Client();
        await ResetAsync(client);
        var timestamp = await SeedAsync(client, "original prose");

        await client.PostAsJsonAsync(
            "/api/clipboard/edit",
            new { timestamp, text = "https://example.com/edited" },
            TestContext.Current.CancellationToken);
        var edited = await JsonAsync(await client.GetAsync(
            $"/api/clipboard/item/{Uri.EscapeDataString(timestamp)}", TestContext.Current.CancellationToken));

        Assert.Equal("https://example.com/edited", edited.GetProperty("textContent").GetString());
        Assert.True(edited.GetProperty("isEdited").GetBoolean());
        Assert.Equal("url", edited.GetProperty("kind").GetString());

        await client.PostAsJsonAsync(
            "/api/clipboard/edit", new { timestamp, text = "" }, TestContext.Current.CancellationToken);
        var restored = await JsonAsync(await client.GetAsync(
            $"/api/clipboard/item/{Uri.EscapeDataString(timestamp)}", TestContext.Current.CancellationToken));

        Assert.Equal("original prose", restored.GetProperty("textContent").GetString());
        Assert.False(restored.GetProperty("isEdited").GetBoolean());
    }

    [Fact]
    public async Task ClearHistory_EmptiesTheStore()
    {
        using var client = Client();
        await SeedAsync(client, "one");

        await ResetAsync(client);

        var body = await JsonAsync(
            await client.GetAsync("/api/clipboard", TestContext.Current.CancellationToken));
        Assert.Empty(body.GetProperty("items").EnumerateArray());
    }

    // ── Images ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetImage_ServesAPngHeldInTheCacheDirectory()
    {
        using var client = Client();
        await ResetAsync(client);
        var cacheDir = Path.Combine(fixture.DataDir.Path, "cache");
        Directory.CreateDirectory(cacheDir);
        var imagePath = Path.Combine(cacheDir, "shot.png");
        await File.WriteAllBytesAsync(
            imagePath, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], TestContext.Current.CancellationToken);
        var timestamp = DateTimeOffset.UtcNow.ToString("o");
        await client.PostAsJsonAsync(
            "/api/clipboard/_seed",
            new { timestamp, text = (string?)null, hasImage = true, imagePath, sourceApp = "tests" },
            TestContext.Current.CancellationToken);

        var response = await client.GetAsync(
            $"/api/clipboard/image/{Uri.EscapeDataString(timestamp)}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetImage_RefusesAPathOutsideTheCacheDirectory()
    {
        // image_path is data. A history row naming a file elsewhere on disk must
        // not turn this endpoint into a general file reader.
        using var client = Client();
        await ResetAsync(client);
        var outsidePath = Path.Combine(fixture.DataDir.Path, "outside-the-cache.png");
        await File.WriteAllTextAsync(outsidePath, "not yours", TestContext.Current.CancellationToken);
        var timestamp = DateTimeOffset.UtcNow.ToString("o");
        await client.PostAsJsonAsync(
            "/api/clipboard/_seed",
            new { timestamp, text = (string?)null, hasImage = true, imagePath = outsidePath, sourceApp = "tests" },
            TestContext.Current.CancellationToken);

        var response = await client.GetAsync(
            $"/api/clipboard/image/{Uri.EscapeDataString(timestamp)}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetImage_UnknownTimestampIsNotFound()
    {
        using var client = Client();

        var response = await client.GetAsync(
            "/api/clipboard/image/2000-01-01T00%3A00%3A00Z", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── CORS: the allow path AND the deny path ──────────────────────────

    [Theory]
    [InlineData("http://127.0.0.1:5173")]
    [InlineData("http://localhost:1420")]
    [InlineData("https://localhost:8787")]
    [InlineData("http://[::1]:3000")]
    [InlineData("tauri://localhost")]
    [InlineData("http://tauri.localhost")]
    [InlineData("https://tauri.localhost")]
    public async Task Cors_AllowsTheLocalWebUiOrigins(string origin)
    {
        // The allow path is the one every real request takes; a guard that
        // rejected these would break the web UI completely.
        using var client = Client();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", origin);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.True(
            response.Headers.Contains("Access-Control-Allow-Origin"),
            $"expected {origin} to be an allowed origin");
    }

    [Theory]
    [InlineData("https://evil.example.com")]
    [InlineData("http://127.0.0.1.evil.example.com")]
    [InlineData("http://notlocalhost")]
    [InlineData("file://")]
    [InlineData("null")]
    public async Task Cors_RejectsEverythingElse(string origin)
    {
        using var client = Client();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.TryAddWithoutValidation("Origin", origin);

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.False(
            response.Headers.Contains("Access-Control-Allow-Origin"),
            $"expected {origin} to be rejected");
    }

    [Fact]
    public async Task Cors_RejectsFileOriginsEvenThoughTheyLookLoopbackToUri()
    {
        // Uri.IsLoopback is true for file:// URIs, so a loopback-only check that
        // ignores the scheme would hand a local HTML file the whole history. Assert
        // the trap is real, then assert the endpoint does not fall into it.
        Assert.True(Uri.TryCreate("file:///etc/passwd", UriKind.Absolute, out var asUri));
        Assert.True(asUri.IsLoopback);

        using var client = Client();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.TryAddWithoutValidation("Origin", "file:///etc/passwd");

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    // ── Spec ────────────────────────────────────────────────────────────

    [Fact]
    public async Task OpenApi_DocumentIsServedAndDescribesTheHistoryEndpoint()
    {
        using var client = Client();

        var body = await JsonAsync(
            await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken));

        Assert.True(body.GetProperty("paths").TryGetProperty("/api/clipboard", out _));
    }
}
