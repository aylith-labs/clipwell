using System.Text.Json;

namespace Clipwell.Cli;

public static class CliApplication
{
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(15);

    public static async Task<int> RunAsync(string[] args, HttpClient http, TextWriter stdout,
        TextWriter stderr, CancellationToken cancellationToken = default, TimeSpan? requestTimeout = null)
    {
        var command = args.Length > 0 ? args[0] : "help";
        if (command is not ("list" or "clear" or "watch"))
        {
            PrintHelp(stdout);
            return 0;
        }
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (command != "watch") deadline.CancelAfter(requestTimeout ?? RequestTimeout);
        try
        {
            switch (command)
            {
                case "list": await ListAsync(http, args, stdout, deadline.Token); break;
                case "clear": await ClearAsync(http, stdout, deadline.Token); break;
                case "watch": await WatchAsync(http, stdout, deadline.Token); break;
            }
            return 0;
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                if (command == "watch") return 0;
                stderr.WriteLine("Clipwell request interrupted; the operation outcome may be unknown. No retry was made.");
                return 130;
            }
            stderr.WriteLine("Clipwell daemon request timed out; the operation outcome may be unknown. No retry was made.");
            return 1;
        }
        catch (HttpRequestException error) when (error.StatusCode is { } status)
        {
            stderr.WriteLine($"The Clipwell daemon at {http.BaseAddress} answered HTTP {(int)status}.");
            return 1;
        }
        catch (HttpRequestException)
        {
            // The endpoint is safe to print: Program rejects URLs with credentials.
            // The exception text is not, as it can echo server-provided content.
            stderr.WriteLine($"Cannot reach the Clipwell daemon at {http.BaseAddress}. Is it running?  dotnet run --project daemon");
            return 1;
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            stderr.WriteLine("The Clipwell daemon returned an invalid response.");
            return 1;
        }
    }

    static async Task ListAsync(HttpClient http, string[] args, TextWriter stdout, CancellationToken cancellationToken)
    {
        var requested = args.Length > 1 && int.TryParse(args[1], out var parsed) ? parsed : 20;
        var limit = Math.Clamp(requested, 1, 1000);
        using var doc = JsonDocument.Parse(await http.GetStringAsync($"/api/clipboard?limit={limit}", cancellationToken));
        var items = doc.RootElement.GetProperty("items");
        if (items.GetArrayLength() == 0)
        {
            stdout.WriteLine("(no history yet — copy something with the daemon running)");
            return;
        }
        foreach (var item in items.EnumerateArray())
        {
            var timestamp = item.GetProperty("timestamp").GetString();
            var text = item.TryGetProperty("textContent", out var textNode) ? textNode.GetString() : null;
            var preview = (text ?? "<non-text>").ReplaceLineEndings(" ");
            if (preview.Length > 70) preview = preview[..70] + "…";
            stdout.WriteLine($"{timestamp}  {preview}");
        }
    }

    static async Task ClearAsync(HttpClient http, TextWriter stdout, CancellationToken cancellationToken)
    {
        using var res = await http.PostAsync("/api/clipboard/clear", null, cancellationToken);
        res.EnsureSuccessStatusCode();
        stdout.WriteLine("History cleared.");
    }

    static async Task WatchAsync(HttpClient http, TextWriter stdout, CancellationToken cancellationToken)
    {
        stdout.WriteLine("Watching for clipboard changes (Ctrl+C to stop)…");
        using var stream = await http.GetStreamAsync("/api/clipboard/stream", cancellationToken);
        using var reader = new StreamReader(stream);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.StartsWith("data: ", StringComparison.Ordinal))
                stdout.WriteLine(line["data: ".Length..]);
        }
    }

    public static void PrintHelp(TextWriter stdout)
    {
        stdout.WriteLine("""
            clipwell — reference client for the Clipwell daemon

            Usage:
              clipwell list [n]   Show the n most recent items (default 20)
              clipwell watch      Live-stream clipboard changes via SSE
              clipwell clear      Delete all history

            Environment:
              CLIPWELL_API        Daemon base URL (default http://127.0.0.1:8787)
            """);
    }
}
