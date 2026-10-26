using Microsoft.Extensions.DependencyInjection;

namespace Linker.Core.Attributes;

/// <summary>
/// 配置映射类特性。
/// </summary>
[AttributeUsage(
    AttributeTargets.Class,
    AllowMultiple = false,
    Inherited = false)]
public sealed class ConfigAttribute :
    DependencyInjectionAttribute
{
    public ConfigAttribute(
        string sectionName,
        ServiceLifetime lifetime =
            ServiceLifetime.Singleton)
        : base(lifetime)
    {
        if (string.IsNullOrWhiteSpace(sectionName))
        {
            throw new ArgumentException(
                "配置节名称不能为空。",
                nameof(sectionName));
        }

        SectionName = sectionName;
    }

    /// <summary>
    /// 配置节名称，例如 Database、Jwt、Redis。
    /// </summary>
    public string SectionName { get; }
}