using System.Reflection;
using System.Runtime.Loader;

namespace Linker.Core.Scanning;

public static class ApplicationAssemblyDiscovery
{
    public static IReadOnlyList<Assembly> Discover(
        Assembly entryAssembly,
        LinkerOptions options)
    {
        ArgumentNullException.ThrowIfNull(entryAssembly);
        ArgumentNullException.ThrowIfNull(options);

        var assemblies = new Dictionary<string, Assembly>(
            StringComparer.OrdinalIgnoreCase);

        if (options.ScanLoadedAssemblies)
        {
            AddLoadedAssemblies(assemblies);
        }

        AddAssembly(
            assemblies,
            entryAssembly);

        if (options.ScanReferencedAssemblies)
        {
            AddReferencedAssemblies(
                entryAssembly,
                assemblies);
        }

        if (options.ScanDirectoryAssemblies)
        {
            AddDirectoryAssemblies(assemblies);
        }

        return assemblies.Values.ToArray();
    }

    private static void AddLoadedAssemblies(
        IDictionary<string, Assembly> assemblies)
    {
        foreach (var assembly in
            AppDomain.CurrentDomain.GetAssemblies())
        {
            AddAssembly(
                assemblies,
                assembly);
        }
    }

    private static void AddReferencedAssemblies(
        Assembly rootAssembly,
        IDictionary<string, Assembly> assemblies)
    {
        var visited = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        VisitReferencedAssemblies(
            rootAssembly,
            assemblies,
            visited);
    }

    private static void VisitReferencedAssemblies(
        Assembly assembly,
        IDictionary<string, Assembly> assemblies,
        ISet<string> visited)
    {
        var fullName = assembly.FullName;

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return;
        }

        if (!visited.Add(fullName))
        {
            return;
        }

        AddAssembly(
            assemblies,
            assembly);

        foreach (var reference in
            assembly.GetReferencedAssemblies())
        {
            try
            {
                var referencedAssembly =
                    AssemblyLoadContext.Default
                        .LoadFromAssemblyName(reference);

                VisitReferencedAssemblies(
                    referencedAssembly,
                    assemblies,
                    visited);
            }
            catch (FileNotFoundException)
            {
                // 程序集不存在，跳过。
            }
            catch (FileLoadException)
            {
                // 程序集加载失败，跳过。
            }
            catch (BadImageFormatException)
            {
                // 不是可加载的托管程序集，跳过。
            }
        }
    }

    private static void AddDirectoryAssemblies(
        IDictionary<string, Assembly> assemblies)
    {
        var baseDirectory =
            AppContext.BaseDirectory;

        var assemblyFiles = Directory
            .EnumerateFiles(
                baseDirectory,
                "*.dll",
                SearchOption.TopDirectoryOnly);

        foreach (var assemblyFile in assemblyFiles)
        {
            try
            {
                var assemblyName =
                    AssemblyName.GetAssemblyName(
                        assemblyFile);

                var assembly =
                    AssemblyLoadContext.Default
                        .LoadFromAssemblyName(assemblyName);

                AddAssembly(
                    assemblies,
                    assembly);
            }
            catch (BadImageFormatException)
            {
            }
            catch (FileLoadException)
            {
            }
            catch (FileNotFoundException)
            {
            }
        }
    }

    private static void AddAssembly(
        IDictionary<string, Assembly> assemblies,
        Assembly assembly)
    {
        if (assembly.IsDynamic)
        {
            return;
        }

        var fullName = assembly.FullName;

        if (string.IsNullOrWhiteSpace(fullName))
        {
            return;
        }

        assemblies[fullName] = assembly;
    }
}