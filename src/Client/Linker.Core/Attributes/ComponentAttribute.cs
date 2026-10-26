using Microsoft.Extensions.DependencyInjection;

namespace Linker.Core.Attributes;

/// <summary>
/// 通用组件自动注入特性。
/// 默认使用 Scoped 生命周期。
/// </summary>
[AttributeUsage(
    AttributeTargets.Class,
    AllowMultiple = false,
    Inherited = false)]
public sealed class ComponentAttribute :
    DependencyInjectionAttribute
{
    public ComponentAttribute(
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        : base(lifetime)
    {
    }
}