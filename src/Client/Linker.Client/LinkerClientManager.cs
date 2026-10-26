using Microsoft.Extensions.Logging;

namespace Linker.Client;

public sealed class LinkerClientManager
{
    private readonly LinkerClientMemoryStore _memoryStore;
    private readonly LinkerClientOptions _options;
    private readonly ILogger<LinkerClientManager> _logger;

    public LinkerClientManager(
        LinkerClientMemoryStore memoryStore,
        LinkerClientOptions options,
        ILogger<LinkerClientManager> logger)
    {
        _memoryStore = memoryStore;
        _options = options;
        _logger = logger;
    }

    public IReadOnlyCollection<LinkerServiceSnapshot> GetServices() => _memoryStore.GetServices();

    public LinkerServiceSnapshot? GetService(string serviceName) => _memoryStore.GetService(serviceName);

    public void RegisterService(LinkerServiceSnapshot snapshot)
    {
        _memoryStore.RegisterService(snapshot);
        _logger.LogInformation("registered service: {ServiceName}", snapshot.Name);
    }

    public LinkerConfigSnapshot? GetConfig(string serviceName, string configKey, long version)
    {
        return _memoryStore.GetConfig(serviceName, configKey, version);
    }

    public void RegisterConfig(LinkerConfigSnapshot config)
    {
        _memoryStore.RegisterConfig(config);
        _logger.LogInformation("registered config: {ServiceName}/{ConfigKey}/{Version}", config.ServiceName, config.Key, config.Version);
    }

    public IReadOnlyCollection<LinkerClusterNode> GetClusterNodes() => _memoryStore.GetClusterNodes();

    public LinkerClusterNode? GetDefaultNode() => _memoryStore.GetDefaultNode();

    public LinkerClientOptions Options => _options;
}
