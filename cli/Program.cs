using System.Text.Json;

// clipwell — a tiny reference client for the Clipwell daemon's public REST/SSE API.
// Demonstrates that the daemon is fully driveable by any external process.

var baseUrl = Environment.GetEnvironmentVariable("CLIPWELL_API") ?? "http://127.0.0.1:8787";
// No overall timeout: `watch` holds an SSE stream open indefinitely, and
// HttpClient.Timeout would abort it mid-stream. Per-request deadlines are set
// by the commands that need one.
var http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = Timeout.InfiniteTimeSpan };
var command = args.Length > 0 ? args[0] : "help";

try
{
    switch (command)
    {
        case "list":
            await ListAsync(http, args);
            break;
        case "clear":
            await ClearAsync(http);
            break;
        case "watch":
            await WatchAsync(http);
            break;
        default:
            PrintHelp();
            break;
    }
    return 0;
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Cannot reach daemon at {baseUrl}: {ex.Message}");
    Console.Error.WriteLine("Is the daemon running?  dotnet run --project daemon");
    return 1;
}
catch (OperationCanceledException)
{
    return 0; // Ctrl+C during `watch`
}

static async Task ListAsync(HttpClient http, string[] args)
{
    var requested = args.Length > 1 && int.TryParse(args[1], out var parsed) ? parsed : 20;
    var limit = Math.Clamp(requested, 1, 1000);
    using var doc = JsonDocument.Parse(await http.GetStringAsync($"/api/clipboard?limit={limit}"));
    var items = doc.RootElement.GetProperty("items");
    if (items.GetArrayLength() == 0)
    {
        Console.WriteLine("(no history yet — copy something with the daemon running)");
        return;
    }
    foreach (var item in items.EnumerateArray())
    {
        var timestamp = item.GetProperty("timestamp").GetString();
        var text = item.TryGetProperty("textContent", out var textNode) ? textNode.GetString() : null;
        var preview = (text ?? "<non-text>").ReplaceLineEndings(" ");
        if (preview.Length > 70) preview = preview[..70] + "…";
        Console.WriteLine($"{timestamp}  {preview}");
    }
}

static async Task ClearAsync(HttpClient http)
{
    using var res = await http.PostAsync("/api/clipboard/clear", null);
    res.EnsureSuccessStatusCode();
    Console.WriteLine("History cleared.");
}

static async Task WatchAsync(HttpClient http)
{
    Console.WriteLine("Watching for clipboard changes (Ctrl+C to stop)…");
    using var stream = await http.GetStreamAsync("/api/clipboard/stream");
    using var reader = new StreamReader(stream);
    while (await reader.ReadLineAsync() is { } line)
    {
        if (line.StartsWith("data: ", StringComparison.Ordinal))
            Console.WriteLine(line["data: ".Length..]);
    }
}

static void PrintHelp()
{
    Console.WriteLine("""
        clipwell — reference client for the Clipwell daemon

        Usage:
          clipwell list [n]   Show the n most recent items (default 20)
          clipwell watch      Live-stream clipboard changes via SSE
          clipwell clear      Delete all history

        Environment:
          CLIPWELL_API        Daemon base URL (default http://127.0.0.1:8787)
        """);
}
