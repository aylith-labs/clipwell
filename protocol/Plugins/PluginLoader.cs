using System.Reflection;

namespace Clipwell.Protocol.Plugins;

/// <summary>
/// Loads plugin assemblies from a directory and instantiates every public, concrete
/// type implementing <typeparamref name="T"/> with a parameterless constructor. The
/// daemon loads <see cref="IClipDetector"/>s; the picker loads <see cref="IClipAction"/>s.
/// A bad plugin is skipped, never fatal.
/// </summary>
public static class PluginLoader
{
    /// <summary>
    /// Plugins live in <c>&lt;data dir&gt;/plugins</c> by default (data dir from
    /// CLIPWELL_DATA_DIR, else the OS app-data Clipwell folder). Override the whole
    /// path with CLIPWELL_PLUGINS_DIR.
    /// </summary>
    public static string DefaultDir
    {
        get
        {
            var explicitDir = Environment.GetEnvironmentVariable("CLIPWELL_PLUGINS_DIR");
            if (!string.IsNullOrEmpty(explicitDir)) return explicitDir;
            var dataDir = Environment.GetEnvironmentVariable("CLIPWELL_DATA_DIR")
                ?? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Clipwell");
            return Path.Combine(dataDir, "plugins");
        }
    }

    public static IReadOnlyList<T> Load<T>(string? dir = null)
    {
        dir ??= DefaultDir;
        var found = new List<T>();
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return found;

        string[] dlls;
        try
        {
            dlls = Directory.GetFiles(dir, "*.dll");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return found;
        }

        foreach (var dll in dlls)
        {
            Assembly assembly;
            try
            {
                assembly = Assembly.LoadFrom(dll);
            }
            catch (Exception ex) when (ex is BadImageFormatException or FileLoadException or IOException)
            {
                continue; // not a managed assembly, or unloadable
            }

            foreach (var type in TypesOf(assembly))
            {
                if (!typeof(T).IsAssignableFrom(type) || type.IsAbstract || type.IsInterface) continue;
                if (type.GetConstructor(Type.EmptyTypes) is null) continue;
                try
                {
                    if (Activator.CreateInstance(type) is T instance) found.Add(instance);
                }
                catch (Exception ex) when (ex is TargetInvocationException or MemberAccessException
                    or TypeLoadException or MissingMethodException)
                {
                    // One bad type must not cost us the rest of the assembly.
                }
            }
        }
        return found;
    }

    // A plugin built against a different dependency set can leave some of its
    // types unresolvable; keep the ones that did load instead of dropping the
    // whole assembly.
    private static IEnumerable<Type> TypesOf(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }
}
