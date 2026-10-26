using Linker.Core.Scanning;
using Microsoft.Extensions.DependencyInjection;

namespace Linker.Core.Processors;

public sealed class RepositoryProcessor : AttributeDiProcessor
{
    public override int Order => 200;

    protected override IEnumerable<TypeMetadata> Select(
        TypeCatalog catalog)
    {
        return catalog.Repositories
            .Where(x => x.RepositoryAttribute is not null);
    }

    protected override ServiceLifetime GetLifetime(
        TypeMetadata metadata)
    {
        return metadata.RepositoryAttribute!.Lifetime;
    }
}