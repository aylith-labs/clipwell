using System.Diagnostics;
using Clipwell.Protocol;

namespace Clipwell.Daemon.Unix;

public enum UnixClipboardTool
{
    MacOs,
    Linux,
}

/// <summary>
/// macOS / Linux clipboard watcher. Unix has no event for clipboard changes that
/// is portable across X11/Wayland, so this polls the system clipboard via the
/// platform CLI (<c>pbpaste</c> on macOS; <c>wl-paste</c> or <c>xclip</c> on
/// Linux) and emits when the text changes.
/// </summary>
/// <remarks>
/// Text-only for the first cut. Requires the relevant CLI to be installed
/// (pbpaste ships with macOS; Linux needs wl-clipboard or xclip). Smoke-tested on
/// macOS and Linux in CI via a clipboard round-trip — see .github/workflows/ci.yml.
/// </remarks>
public sealed class UnixPollingClipboardWatcher(UnixClipboardTool tool) : IClipboardWatcher
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(600);

    public event Action<StoreRow>? Changed;
    public event Action<string>? Failed;

    private CancellationTokenSource? _cts;
    private string? _last;
    private bool _disposed;

    public void Start()
    {
        if (_disposed || _cts is not null) return;
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => PollLoopAsync(_cts.Token));
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        // Seed with the current content so we don't emit a spurious "change" for
        // whatever was already on the clipboard at startup.
        _last = ReadClipboardText();
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(PollInterval, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            string? text;
            try
            {
                text = ReadClipboardText();
            }
            catch (Exception ex)
            {
                Failed?.Invoke($"clipboard read failed: {ex.Message}");
                continue;
            }

            if (string.IsNullOrEmpty(text) || text == _last) continue;
            _last = text;
            Changed?.Invoke(new StoreRow
            {
                Timestamp = DateTimeOffset.UtcNow.ToString("o"),
                TextContent = text,
                TextLength = text.Length,
                HasImage = false,
                Formats = ["text"],
            });
        }
    }

    private string? ReadClipboardText() => tool switch
    {
        UnixClipboardTool.MacOs => Run("pbpaste", ""),
        UnixClipboardTool.Linux => RunLinux(),
        _ => null,
    };

    private static string? RunLinux()
    {
        // Prefer Wayland's wl-paste; fall back to X11's xclip.
        return Run("wl-paste", "--no-newline") ?? Run("xclip", "-selection clipboard -o");
    }

    private static readonly TimeSpan ToolTimeout = TimeSpan.FromSeconds(2);

    private static string? Run(string file, string args)
    {
        Process? proc = null;
        try
        {
            proc = Process.Start(new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            if (proc is null) return null;

            // Both pipes must be drained concurrently: a tool that fills the
            // stderr buffer blocks forever while we sit reading stdout.
            var stdout = proc.StandardOutput.ReadToEndAsync();
            var stderr = proc.StandardError.ReadToEndAsync();
            if (!proc.WaitForExit((int)ToolTimeout.TotalMilliseconds))
            {
                // A hung tool would otherwise be re-spawned every poll and never
                // reaped, leaking a process and its handles each time.
                TryKill(proc);
                return null;
            }
            // Lets the async reads observe end-of-stream before the handle closes.
            proc.WaitForExit();
            stderr.GetAwaiter().GetResult();
            return proc.ExitCode == 0 ? stdout.GetAwaiter().GetResult() : null;
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException
            or IOException or ObjectDisposedException)
        {
            // Tool not installed / not on PATH, or it vanished mid-read.
            if (proc is not null) TryKill(proc);
            return null;
        }
        finally
        {
            proc?.Dispose();
        }
    }

    private static void TryKill(Process proc)
    {
        try
        {
            if (!proc.HasExited) proc.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or NotSupportedException
            or System.ComponentModel.Win32Exception)
        {
            // Already gone, or we cannot signal it — nothing further to do.
        }
    }

    public void Dispose()
    {
        _disposed = true;
        var cts = _cts;
        _cts = null;
        if (cts is null) return;
        cts.Cancel();
        cts.Dispose();
    }
}
