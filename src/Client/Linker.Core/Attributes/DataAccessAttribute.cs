using Microsoft.Extensions.DependencyInjection;

namespace Linker.Core.Attributes;

/// <summary>
/// 数据访问组件自动注入特性。
/// 默认使用 Scoped 生命周期。
/// </summary>
[AttributeUsage(
    AttributeTargets.Class,
    AllowMultiple = false,
    Inherited = false)]
public sealed class DataAccessAttribute :
    DependencyInjectionAttribute
{
    public DataAccessAttribute(
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
        : base(lifetime)
    {
    }
}