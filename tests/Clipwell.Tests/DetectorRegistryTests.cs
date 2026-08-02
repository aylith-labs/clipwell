using Clipwell.Daemon.Detectors;
using Clipwell.Protocol;
using Clipwell.Protocol.Plugins;
using Xunit;

namespace Clipwell.Tests;

public sealed class DetectorRegistryTests
{
    private static ClipItem Item(string? text, bool hasImage = false) => new()
    {
        Id = "db:1",
        Timestamp = "2026-06-14T01:23:45.6789012+00:00",
        TextContent = text,
        TextLength = text?.Length ?? 0,
        HasImage = hasImage,
    };

    // ── Built-in detectors: match ───────────────────────────────────────

    [Theory]
    [InlineData("https://github.com/aylith-labs/clipwell/pull/1", "github-pr")]
    [InlineData("https://github.com/owner/repo/pull/9876/files", "github-pr")]
    [InlineData("HTTPS://GitHub.com/owner/repo/pull/3", "github-pr")]
    [InlineData("https://example.com", "url")]
    [InlineData("http://localhost:8787/app", "url")]
    [InlineData("person@example.com", "email")]
    [InlineData("first.last+tag@sub.example.co.uk", "email")]
    [InlineData("ABC-1", "jira-issue")]
    [InlineData("PROJ2-4512", "jira-issue")]
    [InlineData("#fff", "color")]
    [InlineData("#AABBCC", "color")]
    [InlineData("#aabbccdd", "color")]
    [InlineData("/usr/local/bin/clipwell", "path")]
    [InlineData(@"C:\Users\dev\notes.txt", "path")]
    [InlineData(@"\\server\share\file", "path")]
    [InlineData("const value = 1;", "code")]
    [InlineData("function run() {}", "code")]
    [InlineData("def handler(request):", "code")]
    [InlineData("class Widget:", "code")]
    [InlineData("const double = value => value * 2", "code")]
    [InlineData("<div>hello</div>", "code")]
    [InlineData("if (ok) {\n    run()\n}", "code")]
    public void Classify_RecognisesTheKind(string text, string expected) =>
        Assert.Equal(expected, new DetectorRegistry([]).Classify(Item(text)));

    [Theory]
    [InlineData("the value is {redacted} for now")]
    [InlineData("we shipped a function yesterday")]
    [InlineData("define the class of users first")]
    public void Classify_ProseWithACodeLookingCharacterIsStillText(string text) =>
        Assert.Equal("text", new DetectorRegistry([]).Classify(Item(text)));

    [Fact]
    public void Classify_ImageWinsRegardlessOfText() =>
        Assert.Equal("image", new DetectorRegistry([]).Classify(Item("https://example.com", hasImage: true)));

    [Fact]
    public void Classify_SurroundingWhitespaceDoesNotDefeatDetection() =>
        Assert.Equal("url", new DetectorRegistry([]).Classify(Item("  https://example.com  ")));

    // ── Built-in detectors: no match ────────────────────────────────────

    [Theory]
    [InlineData("just prose")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("ftp://example.com/file")]
    [InlineData("not an email @ all")]
    [InlineData("#ff")]
    [InlineData("#gggggg")]
    [InlineData("abc-123")]
    [InlineData("relative/path/file.txt")]
    [InlineData("https://example.com and some trailing prose")]
    public void Classify_FallsBackToTextWhenNothingMatches(string? text) =>
        Assert.Equal("text", new DetectorRegistry([]).Classify(Item(text)));

    [Fact]
    public void Classify_MultiLineTextIsNotAPath() =>
        Assert.NotEqual("path", new DetectorRegistry([]).Classify(Item("/usr/bin\n/etc/hosts")));

    // ── Priority ordering ───────────────────────────────────────────────

    [Fact]
    public void Classify_GitHubPrBeatsTheGenericUrlDetector() =>
        Assert.Equal(
            "github-pr",
            new DetectorRegistry([]).Classify(Item("https://github.com/owner/repo/pull/1")));

    [Fact]
    public void Classify_LowerPriorityNumberWins()
    {
        var registry = new DetectorRegistry([
            new StubDetector("plugin.late", priority: 900, kind: "late"),
            new StubDetector("plugin.early", priority: 1, kind: "early"),
        ]);

        Assert.Equal("early", registry.Classify(Item("anything")));
    }

    [Fact]
    public void Classify_APluginCanOutrankABuiltIn()
    {
        var registry = new DetectorRegistry([new StubDetector("plugin.first", priority: -1, kind: "custom")]);

        Assert.Equal("custom", registry.Classify(Item("https://example.com")));
    }

    [Fact]
    public void Classify_APluginRunningAfterTheBuiltInsCannotOverrideAMatch()
    {
        var registry = new DetectorRegistry([new StubDetector("plugin.last", priority: 999, kind: "custom")]);

        Assert.Equal("url", registry.Classify(Item("https://example.com")));
    }

    // ── Misbehaving plugins ─────────────────────────────────────────────

    [Fact]
    public void Classify_AThrowingPluginIsSkippedRatherThanFailingTheRead()
    {
        // Classification runs on every history read, so one bad plugin must not
        // take down the whole API.
        var registry = new DetectorRegistry([new ThrowingDetector()]);

        Assert.Equal("url", registry.Classify(Item("https://example.com")));
    }

    [Fact]
    public void Classify_AThrowingPluginIsReported()
    {
        var registry = new DetectorRegistry([new ThrowingDetector()]);
        var failures = new List<string>();
        registry.Failed += (detectorId, _) => failures.Add(detectorId);

        registry.Classify(Item("anything"));

        Assert.Equal(["plugin.throws"], failures);
    }

    [Fact]
    public void Classify_AThrowingPluginDoesNotStopLaterDetectorsFromMatching()
    {
        var registry = new DetectorRegistry([
            new ThrowingDetector(),
            new StubDetector("plugin.after", priority: -1, kind: "custom"),
        ]);

        Assert.Equal("custom", registry.Classify(Item("anything")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Classify_APluginReturningABlankKindIsTreatedAsNoMatch(string kind)
    {
        // A blank kind would otherwise become a nameless filter pill.
        var registry = new DetectorRegistry([new StubDetector("plugin.blank", priority: -1, kind: kind)]);

        Assert.Equal("url", registry.Classify(Item("https://example.com")));
    }

    [Fact]
    public void Classify_WithNoExtraDetectorsStillUsesTheBuiltIns() =>
        Assert.Equal("url", new DetectorRegistry().Classify(Item("https://example.com")));

    private sealed class StubDetector(string id, int priority, string? kind) : IClipDetector
    {
        public string Id => id;
        public int Priority => priority;
        public string? Detect(ClipItem item) => kind;
    }

    private sealed class ThrowingDetector : IClipDetector
    {
        public string Id => "plugin.throws";
        public int Priority => -100;
        public string? Detect(ClipItem item) => throw new InvalidOperationException("plugin is broken");
    }
}
