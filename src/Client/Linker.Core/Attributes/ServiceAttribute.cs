using Microsoft.Extensions.DependencyInjection;

namespace Linker.Core.Attributes;

[AttributeUsage(
    AttributeTargets.Class,
    AllowMultiple = false,
    Inherited = false)]
public sealed class ServiceAttribute :
    DependencyInjectionAttribute
{
    public ServiceAttribute(
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        : base(lifetime)
    {
    }
}