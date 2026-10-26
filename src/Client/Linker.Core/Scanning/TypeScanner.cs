using System.Reflection;
using Linker.Core.Attributes;

namespace Linker.Core.Scanning;

/// <summary>
/// 统一程序集扫描器。
/// 每个程序集只扫描一次，每个类型的特性和接口信息只读取一次。
/// </summary>
public sealed class TypeScanner
{
    public TypeCatalog Scan(IEnumerable<Assembly> assemblies)
    {
        var metadataList = new List<TypeMetadata>();

        var distinctAssemblies = assemblies
            .Where(x => x is not null)
            .DistinctBy(x => x.FullName)
            .ToArray();

        foreach (var assembly in distinctAssemblies)
        {
            var types = GetLoadableTypes(assembly);

            foreach (var type in types)
            {
                if (!IsScannableType(type))
                {
                    continue;
                }

                var serviceAttribute = type.GetCustomAttribute<ServiceAttribute>();

                var repositoryAttribute = type.GetCustomAttribute<RepositoryAttribute>();

                var appStartupAttribute = type.GetCustomAttribute<AppStartupAttribute>();

                var hasSupportedAttribute =
                    serviceAttribute is not null ||
                    repositoryAttribute is not null ||
                    appStartupAttribute is not null;

                if (!hasSupportedAttribute)
                {
                    continue;
                }

                var serviceTypes =
                    GetServiceTypes(type);

                metadataList.Add(new TypeMetadata
                {
                    ImplementationType = type,
                    ServiceTypes = serviceTypes,
                    ServiceAttribute = serviceAttribute,
                    RepositoryAttribute = repositoryAttribute,
                    AppStartupAttribute = appStartupAttribute
                });
            }
        }

        return new TypeCatalog(metadataList);
    }

    private static bool IsScannableType(Type type)
    {
        return type is
        {
            IsClass: true,
            IsAbstract: false,
            IsGenericTypeDefinition: false
        };
    }

    private static Type[] GetServiceTypes(Type implementationType)
    {
        return implementationType
            .GetInterfaces()
            .Where(IsApplicationInterface)
            .ToArray();
    }

    private static bool IsApplicationInterface(Type type)
    {
        return type.IsInterface &&
               type != typeof(IDisposable);
    }

    private static Type[] GetLoadableTypes(
        Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types
                .Where(x => x is not null)
                .Cast<Type>()
                .ToArray();
        }
    }
}