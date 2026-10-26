using Linker.Core.Scanning;
using Microsoft.Extensions.DependencyInjection;

namespace Linker.Core.Contracts;

public interface ITypeProcessor
{
    int Order { get; }

    void Process(
        TypeCatalog catalog,
        IServiceCollection services);
}