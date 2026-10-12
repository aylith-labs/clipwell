using System.Net;
using Clipwell.Cli;
using Xunit;

namespace Clipwell.Tests;

/// <summary>
/// The reference CLI's HTTP boundary, exercised against a scripted handler:
/// no daemon, network or clipboard is touched.
/// </summary>
public sealed class CliApplicationTests
{
    private const string Sensitive = "private-history-or-server-detail";

    private sealed class ScriptedHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
        : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public bool SawCancellation { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            try { return await respond(request, cancellationToken); }
            catch (OperationCanceledException) { SawCancellation = cancellationToken.IsCancellationRequested; throw; }
        }
    }

    private static ScriptedHandler Hangs() => new(async (_, token) =>
    {
        await Task.Delay(TimeSpan.FromSeconds(5), token);
        return new HttpResponseMessage(HttpStatusCode.OK);
    });

    private static ScriptedHandler Returns(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json) }));

    // `stop` stands in for Ctrl+C; without it only the test run can cancel.
    private static async Task<(int Code, string Out, string Err)> Run(
        ScriptedHandler handler, string[] args, CancellationTokenSource? stop = null, TimeSpan? timeout = null)
    {
        var token = stop?.Token ?? TestContext.Current.CancellationToken;
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://127.0.0.1:8787"),
            Timeout = Timeout.InfiniteTimeSpan,
        };
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        var code = await CliApplication.RunAsync(args, client, stdout, stderr, token, timeout);
        return (code, stdout.ToString(), stderr.ToString());
    }

    [Fact]
    public void RequestTimeout_IsFifteenSeconds() =>
        Assert.Equal(TimeSpan.FromSeconds(15), CliApplication.RequestTimeout);

    [Theory]
    [InlineData("list")]
    [InlineData("clear")]
    public async Task HungDaemon_TimesOutOnceWithoutClaimingSuccess(string command)
    {
        var handler = Hangs();

        var (code, output, error) = await Run(handler, [command], timeout: TimeSpan.FromMilliseconds(50));

        Assert.Equal(1, code);
        Assert.Equal(1, handler.Calls);
        Assert.True(handler.SawCancellation);
        Assert.Equal("", output);
        Assert.Contains("timed out", error);
        Assert.Contains("No retry was made", error);
    }

    [Fact]
    public async Task InterruptedClear_ReportsUnknownOutcomeWithExitCode130()
    {
        var handler = Hangs();
        using var interrupted = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var (code, output, error) = await Run(handler, ["clear"], interrupted);

        Assert.Equal(130, code);
        Assert.Equal(1, handler.Calls);
        Assert.Equal("", output);
        Assert.Contains("interrupted", error);
    }

    [Fact]
    public async Task Watch_IsNotBoundByTheRequestDeadline_AndStopsCleanly()
    {
        using var stopped = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        var (code, _, error) = await Run(Hangs(), ["watch"], stopped, TimeSpan.FromMilliseconds(1));

        Assert.Equal(0, code);
        Assert.Equal("", error);
    }

    [Fact]
    public async Task Watch_PrintsEachServerSentEventPayload()
    {
        var handler = Returns("event: clip\ndata: {\"id\":1}\n\ndata: {\"id\":2}\n\n");

        var (code, output, _) = await Run(handler, ["watch"]);

        Assert.Equal(0, code);
        Assert.Contains("{\"id\":1}", output);
        Assert.Contains("{\"id\":2}", output);
        Assert.DoesNotContain("event: clip", output);
    }

    [Fact]
    public async Task UnreachableDaemon_NamesTheEndpointButNotTheExceptionText()
    {
        var handler = new ScriptedHandler((_, _) => throw new HttpRequestException(Sensitive));

        var (code, output, error) = await Run(handler, ["list"]);

        Assert.Equal(1, code);
        Assert.Equal("", output);
        Assert.Contains("Cannot reach the Clipwell daemon at http://127.0.0.1:8787/", error);
        Assert.DoesNotContain(Sensitive, error);
    }

    [Fact]
    public async Task ErrorStatus_IsReportedAsAnHttpStatus_NotAsUnreachable()
    {
        var (code, _, error) = await Run(Returns(Sensitive, HttpStatusCode.ServiceUnavailable), ["clear"]);

        Assert.Equal(1, code);
        Assert.Contains("answered HTTP 503", error);
        Assert.DoesNotContain(Sensitive, error);
    }

    [Theory]
    [InlineData("{\"private\":\"" + Sensitive + "\"")]
    [InlineData("{\"items\":\"" + Sensitive + "\"}")]
    public async Task InvalidResponse_IsRejectedWithoutEchoingIt(string body)
    {
        var (code, output, error) = await Run(Returns(body), ["list"]);

        Assert.Equal(1, code);
        Assert.Equal("", output);
        Assert.Contains("invalid response", error);
        Assert.DoesNotContain(Sensitive, error);
    }

    [Fact]
    public async Task List_PrintsTruncatedSingleLinePreviews()
    {
        var longText = new string('x', 80) + "\nsecond line";
        var handler = Returns(
            "{\"items\":[{\"timestamp\":\"2026-10-01T10:00:00Z\",\"textContent\":\"" + longText.Replace("\n", "\\n") + "\"}," +
            "{\"timestamp\":\"2026-10-01T09:00:00Z\"}]}");

        var (code, output, _) = await Run(handler, ["list", "5"]);

        Assert.Equal(0, code);
        Assert.Contains("2026-10-01T10:00:00Z  " + new string('x', 70) + "…", output);
        Assert.Contains("2026-10-01T09:00:00Z  <non-text>", output);
    }

    [Fact]
    public async Task List_ClampsTheRequestedLimit()
    {
        string? requested = null;
        var handler = new ScriptedHandler((request, _) =>
        {
            requested = request.RequestUri!.Query;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"items\":[]}") });
        });

        var (code, output, _) = await Run(handler, ["list", "5000"]);

        Assert.Equal(0, code);
        Assert.Equal("?limit=1000", requested);
        Assert.Contains("no history yet", output);
    }

    [Fact]
    public async Task UnknownCommand_PrintsHelpWithoutContactingTheDaemon()
    {
        var handler = Hangs();

        var (code, output, _) = await Run(handler, ["bogus"]);

        Assert.Equal(0, code);
        Assert.Equal(0, handler.Calls);
        Assert.Contains("Usage:", output);
    }
}
