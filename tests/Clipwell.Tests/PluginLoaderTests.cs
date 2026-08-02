using System.Reflection;
using Clipwell.Protocol.Plugins;
using Xunit;

namespace Clipwell.Tests;

public sealed class PluginLoaderTests : IDisposable
{
    private readonly TempDataDir _dir = new();

    public void Dispose() => _dir.Dispose();

    [Fact]
    public void Load_MissingDirectoryReturnsNothing() =>
        Assert.Empty(PluginLoader.Load<IClipDetector>(Path.Combine(_dir.Path, "nope")));

    [Fact]
    public void Load_EmptyDirectoryReturnsNothing() =>
        Assert.Empty(PluginLoader.Load<IClipDetector>(_dir.Path));

    [Fact]
    public void Load_IgnoresNonDllFiles()
    {
        File.WriteAllText(_dir.File("readme.txt"), "not a plugin");
        File.WriteAllText(_dir.File("plugin.json"), "{}");

        Assert.Empty(PluginLoader.Load<IClipDetector>(_dir.Path));
    }

    [Fact]
    public void Load_AFileThatIsNotAManagedAssemblyIsSkippedNotFatal()
    {
        File.WriteAllBytes(_dir.File("garbage.dll"), "this is definitely not a PE image"u8.ToArray());

        Assert.Empty(PluginLoader.Load<IClipDetector>(_dir.Path));
    }

    [Fact]
    public void Load_AnEmptyDllIsSkippedNotFatal()
    {
        File.WriteAllBytes(_dir.File("empty.dll"), []);

        Assert.Empty(PluginLoader.Load<IClipDetector>(_dir.Path));
    }

    [Fact]
    public void Load_FindsConcreteImplementationsInARealAssembly()
    {
        // The test assembly itself carries the fixture types below.
        var loaded = PluginLoader.Load<IClipDetector>(Path.GetDirectoryName(SelfPath)!);

        Assert.Contains(loaded, detector => detector.Id == "fixture.usable");
    }

    [Fact]
    public void Load_SkipsAbstractTypesAndTypesWithoutAParameterlessConstructor()
    {
        var loaded = PluginLoader.Load<IClipDetector>(Path.GetDirectoryName(SelfPath)!);

        Assert.DoesNotContain(loaded, detector => detector.Id == "fixture.needs-args");
    }

    [Fact]
    public void Load_ATypeWhoseConstructorThrowsDoesNotCostTheRestOfTheAssembly()
    {
        var loaded = PluginLoader.Load<IClipDetector>(Path.GetDirectoryName(SelfPath)!);

        Assert.DoesNotContain(loaded, detector => detector.Id == "fixture.explodes");
        Assert.Contains(loaded, detector => detector.Id == "fixture.usable");
    }

    [Fact]
    public void Load_DoesNotReturnImplementationsOfAnUnrelatedContract()
    {
        var actions = PluginLoader.Load<IClipAction>(Path.GetDirectoryName(SelfPath)!);

        Assert.DoesNotContain(actions, action => action.Id == "fixture.usable");
    }

    [Fact]
    public void DefaultDir_PrefersTheExplicitPluginsOverride()
    {
        var previousPlugins = Environment.GetEnvironmentVariable("CLIPWELL_PLUGINS_DIR");
        try
        {
            Environment.SetEnvironmentVariable("CLIPWELL_PLUGINS_DIR", _dir.Path);

            Assert.Equal(_dir.Path, PluginLoader.DefaultDir);
        }
        finally
        {
            Environment.SetEnvironmentVariable("CLIPWELL_PLUGINS_DIR", previousPlugins);
        }
    }

    private static string SelfPath => Assembly.GetExecutingAssembly().Location;
}

internal sealed class UsableFixtureDetector : IClipDetector
{
    public string Id => "fixture.usable";
    public int Priority => 500;
    public string? Detect(Clipwell.Protocol.ClipItem item) => null;
}

internal sealed class ConstructorArgFixtureDetector(string label) : IClipDetector
{
    public string Id => $"fixture.needs-args:{label}";
    public int Priority => 500;
    public string? Detect(Clipwell.Protocol.ClipItem item) => null;
}

internal sealed class ExplodingFixtureDetector : IClipDetector
{
    public ExplodingFixtureDetector() => throw new InvalidOperationException("cannot construct");

    public string Id => "fixture.explodes";
    public int Priority => 500;
    public string? Detect(Clipwell.Protocol.ClipItem item) => null;
}

internal abstract class AbstractFixtureDetector : IClipDetector
{
    public string Id => "fixture.abstract";
    public int Priority => 500;
    public string? Detect(Clipwell.Protocol.ClipItem item) => null;
}
