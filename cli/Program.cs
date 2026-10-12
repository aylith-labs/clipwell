using Clipwell.Cli;

var command = args.Length > 0 ? args[0] : "help";
if (command is not ("list" or "clear" or "watch"))
{
    CliApplication.PrintHelp(Console.Out);
    return 0;
}
var configured = Environment.GetEnvironmentVariable("CLIPWELL_API") ?? "http://127.0.0.1:8787";
if (!Uri.TryCreate(configured, UriKind.Absolute, out var endpoint) ||
    endpoint.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(endpoint.UserInfo))
{
    Console.Error.WriteLine("CLIPWELL_API must be an absolute HTTP(S) URL without embedded credentials.");
    return 2;
}
// Watch is deliberately unbounded; list/clear own a cancellation deadline.
using var http = new HttpClient { BaseAddress = endpoint, Timeout = Timeout.InfiniteTimeSpan };
using var cancelled = new CancellationTokenSource();
Console.CancelKeyPress += (_, signal) => { signal.Cancel = true; cancelled.Cancel(); };
return await CliApplication.RunAsync(args, http, Console.Out, Console.Error, cancelled.Token);
