using Clipwell.Daemon;
using Clipwell.Daemon.Mcp;
using Clipwell.Protocol;
using Xunit;

namespace Clipwell.Tests;

public sealed class DaemonMcpToolsTests : IDisposable
{
    private readonly TempDataDir _dir = new();
    private readonly MetadataStore _meta;
    private readonly HistoryStore _store;

    public DaemonMcpToolsTests()
    {
        _meta = new MetadataStore(_dir.Path);
        _store = new HistoryStore(_meta, _dir.Path, []);
    }

    [Fact]
    public void Tools_OmitSensitiveContentAndAliasFromDiscoveryAndGetText()
    {
        var publicTime = "2026-09-24T10:00:00+00:00";
        var secretTime = "2026-09-24T10:01:00+00:00";
        _store.Upsert(new StoreRow { Timestamp = publicTime, TextContent = "public needle", TextLength = 13 });
        _store.Upsert(new StoreRow { Timestamp = secretTime, TextContent = "synthetic secret needle", TextLength = 23 });
        _meta.SetAlias(secretTime, "private alias needle");
        _meta.SetSensitive(secretTime, true);
        var tools = new DaemonClipboardTools(_store);

        Assert.Contains("public needle", tools.Recent(1));
        Assert.DoesNotContain("synthetic secret", tools.Recent(1));
        Assert.Contains("public needle", tools.Search("needle", 1));
        Assert.Equal("No clipboard items matching \"alias\".", tools.Search("alias"));
        Assert.Equal("This clipboard item is marked sensitive.", tools.GetText(secretTime));
        Assert.Equal("public needle", tools.GetText(publicTime));
    }

    public void Dispose()
    {
        _store.Dispose();
        _dir.Dispose();
    }
}
