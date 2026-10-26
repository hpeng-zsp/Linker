namespace Linker.Core.Attributes;

/// <summary>
/// 标记一个应用启动模块，并通过 Order 控制执行顺序。
/// Order 数字越小，越先执行。
/// </summary>
[AttributeUsage(
    AttributeTargets.Class,
    AllowMultiple = false,
    Inherited = false)]
public sealed class AppStartupAttribute : Attribute
{
    public AppStartupAttribute(int order)
    {
        Order = order;
    }

    public int Order { get; }
}