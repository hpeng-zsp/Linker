using Linker.Core.Scanning;
using Microsoft.Extensions.DependencyInjection;

namespace Linker.Core.Processors;

public sealed class ServiceProcessor : AttributeDiProcessor
{
    public override int Order => 100;

    protected override IEnumerable<TypeMetadata> Select(
        TypeCatalog catalog)
    {
        return catalog.Services
            .Where(x => x.ServiceAttribute is not null);
    }

    protected override ServiceLifetime GetLifetime(
        TypeMetadata metadata)
    {
        return metadata.ServiceAttribute!.Lifetime;
    }
}