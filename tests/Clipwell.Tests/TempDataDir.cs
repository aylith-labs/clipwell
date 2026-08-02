namespace Clipwell.Tests;

/// <summary>
/// A throwaway data directory for one test. Every store under test is pointed at
/// one of these, so no test can reach the developer's real clipboard history.
/// </summary>
public sealed class TempDataDir : IDisposable
{
    public TempDataDir()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), "clipwell-tests", Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string File(string name) => System.IO.Path.Combine(Path, name);

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // A still-open SQLite handle on Windows can hold the file; the OS temp
            // sweeper gets it later. Never fail a test on cleanup.
        }
    }
}
