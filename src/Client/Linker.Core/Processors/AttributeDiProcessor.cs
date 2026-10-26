using Linker.Core.Contracts;
using Linker.Core.Scanning;
using Microsoft.Extensions.DependencyInjection;

namespace Linker.Core.Processors;

public abstract class AttributeDiProcessor : ITypeProcessor
{
    public abstract int Order { get; }

    protected abstract IEnumerable<TypeMetadata> Select(
        TypeCatalog catalog);

    protected abstract ServiceLifetime GetLifetime(
        TypeMetadata metadata);

    public void Process(
        TypeCatalog catalog,
        IServiceCollection services)
    {
        foreach (var metadata in Select(catalog))
        {
            RegisterType(
                services,
                metadata.ImplementationType,
                metadata.ServiceTypes,
                GetLifetime(metadata));
        }
    }

    private static void RegisterType(
        IServiceCollection services,
        Type implementationType,
        Type[] serviceTypes,
        ServiceLifetime lifetime)
    {
        if (serviceTypes.Length == 0)
        {
            services.Add(new ServiceDescriptor(
                implementationType,
                implementationType,
                lifetime));

            return;
        }

        foreach (var serviceType in serviceTypes)
        {
            services.Add(new ServiceDescriptor(
                serviceType,
                implementationType,
                lifetime));
        }
    }
}