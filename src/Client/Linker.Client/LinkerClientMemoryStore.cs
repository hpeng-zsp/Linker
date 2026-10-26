using System.Collections.Concurrent;

namespace Linker.Client;

public sealed class LinkerClientMemoryStore
{
    private readonly ConcurrentDictionary<string, LinkerServiceSnapshot> _services = new();
    private readonly ConcurrentDictionary<string, LinkerConfigSnapshot> _configs = new();
    private readonly ConcurrentDictionary<string, LinkerClusterNode> _nodes = new();

    public IReadOnlyCollection<LinkerServiceSnapshot> GetServices() => _services.Values.ToList();

    public void RegisterService(LinkerServiceSnapshot snapshot)
    {
        _services[snapshot.Name] = snapshot;
    }

    public LinkerServiceSnapshot? GetService(string serviceName)
    {
        return _services.TryGetValue(serviceName, out var snapshot) ? snapshot : null;
    }

    public void RegisterConfig(LinkerConfigSnapshot config)
    {
        var id = BuildConfigKey(config.ServiceName, config.Key);
        _configs[id] = config;
    }

    public LinkerConfigSnapshot? GetConfig(string serviceName, string key, long version)
    {
        var id = BuildConfigKey(serviceName, key);
        if (!_configs.TryGetValue(id, out var config))
        {
            return null;
        }

        return version <= 0 || config.Version <= version ? config : null;
    }

    public void RegisterClusterNode(LinkerClusterNode node)
    {
        _nodes[node.Id] = node;
    }

    public IReadOnlyCollection<LinkerClusterNode> GetClusterNodes() => _nodes.Values.ToList();

    public LinkerClusterNode? GetDefaultNode() =>
        _nodes.Values.FirstOrDefault(x => x.IsDefault) ?? _nodes.Values.FirstOrDefault();

    private static string BuildConfigKey(string serviceName, string key)
    {
        return $"{serviceName}:{key}";
    }
}

public sealed class LinkerServiceSnapshot
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    public List<LinkerServiceInstance> Instances { get; set; } = new();
}

public sealed class LinkerServiceInstance
{
    public string InstanceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string NodeAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; }
    public string Status { get; set; } = "active";
    public DateTimeOffset LastHeartbeat { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LinkerConfigSnapshot
{
    public string ServiceName { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public long Version { get; set; }
    public string Payload { get; set; } = "{}";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LinkerClusterNode
{
    public string Id { get; set; } = string.Empty;
    public string Address { get; set; } = "127.0.0.1";
    public int Port { get; set; }
    public string Role { get; set; } = "follower";
    public string Status { get; set; } = "active";
    public bool IsDefault { get; set; }
}
