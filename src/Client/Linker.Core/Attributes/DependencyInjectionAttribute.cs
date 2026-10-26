using Microsoft.Extensions.DependencyInjection;

namespace Linker.Core.Attributes;

/// <summary>
/// 所有自动依赖注入特性的基类。
/// </summary>
public abstract class DependencyInjectionAttribute :
    Attribute
{
    protected DependencyInjectionAttribute(
        ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
    }

    /// <summary>
    /// 当前类型注册到 DI 容器时使用的生命周期。
    /// </summary>
    public ServiceLifetime Lifetime { get; }
}