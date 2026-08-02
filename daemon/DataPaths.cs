namespace Clipwell.Daemon;

/// <summary>
/// Resolves the directory holding the history DB, the metadata overlay, the
/// settings file, and the image cache.
/// </summary>
public static class DataPaths
{
    /// <summary>
    /// <c>CLIPWELL_DATA_DIR</c> when set, else the per-OS app-data Clipwell folder
    /// (<c>%APPDATA%\Roaming\Clipwell</c>, <c>~/.config/Clipwell</c>,
    /// <c>~/Library/Application Support/Clipwell</c>).
    /// </summary>
    public static string Resolve()
    {
        var configured = Environment.GetEnvironmentVariable("CLIPWELL_DATA_DIR");
        return string.IsNullOrEmpty(configured)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Clipwell")
            : configured;
    }
}
