namespace Linker.Core.Scanning;

/// <summary>
/// 保存整个应用程序的类型扫描结果。
/// </summary>
public sealed class TypeCatalog
{
    public TypeCatalog(
        IReadOnlyList<TypeMetadata> types)
    {
        Types = types;
    }

    public IReadOnlyList<TypeMetadata> Types { get; }

    public IEnumerable<TypeMetadata> Services =>
        Types.Where(x => x.IsService);

    public IEnumerable<TypeMetadata> Repositories =>
        Types.Where(x => x.IsRepository);

    public IEnumerable<TypeMetadata> Startups =>
        Types
            .Where(x => x.IsStartup)
            .OrderBy(x => x.StartupOrder);
}