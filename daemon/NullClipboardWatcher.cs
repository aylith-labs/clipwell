using Clipwell.Protocol;

namespace Clipwell.Daemon;

/// <summary>
/// Inert watcher: serves existing history but captures nothing. Used on an OS
/// with no native implementation, and when CLIPWELL_NO_WATCH is set so the
/// docs-capture scripts can't record the user's live clipboard.
/// </summary>
public sealed class NullClipboardWatcher : IClipboardWatcher
{
    // No-op accessors: nothing is ever raised, so a handler is discarded rather
    // than stored.
    public event Action<StoreRow>? Changed { add { } remove { } }

    public event Action<string>? Failed { add { } remove { } }

    public void Start() { }

    public void Dispose() { }
}
