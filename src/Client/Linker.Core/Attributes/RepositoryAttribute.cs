using Microsoft.Extensions.DependencyInjection;

namespace Linker.Core.Attributes;

[AttributeUsage(
    AttributeTargets.Class,
    AllowMultiple = false,
    Inherited = false)]
public sealed class RepositoryAttribute :
    DependencyInjectionAttribute
{
    public RepositoryAttribute(
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        : base(lifetime)
    {
    }
}