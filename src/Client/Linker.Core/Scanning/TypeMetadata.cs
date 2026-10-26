using Linker.Core.Attributes;

namespace Linker.Core.Scanning;

public sealed class TypeMetadata
{
    public required Type ImplementationType { get; init; }

    public required Type[] ServiceTypes { get; init; }

    public ServiceAttribute? ServiceAttribute { get; init; }

    public RepositoryAttribute? RepositoryAttribute { get; init; }

    public ComponentAttribute? ComponentAttribute { get; init; }

    public DataAccessAttribute? DataAccessAttribute { get; init; }

    public ConfigAttribute? ConfigAttribute { get; init; }

    public AppStartupAttribute? AppStartupAttribute { get; init; }

    public bool IsService =>
        ServiceAttribute is not null;

    public bool IsRepository =>
        RepositoryAttribute is not null;

    public bool IsComponent =>
        ComponentAttribute is not null;

    public bool IsDataAccess =>
        DataAccessAttribute is not null;

    public bool IsConfig =>
        ConfigAttribute is not null;

    public bool IsStartup =>
        AppStartupAttribute is not null;

    public int StartupOrder =>
        AppStartupAttribute?.Order ?? 0;
}