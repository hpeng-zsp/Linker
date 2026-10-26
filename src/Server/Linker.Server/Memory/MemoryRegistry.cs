using System.Collections.Concurrent;

namespace Linker.Server.Memory;

public sealed class MemoryRegistry
{
    private readonly ConcurrentDictionary<string, ServiceSnapshot> _services = new();
    private readonly ConcurrentDictionary<string, ConfigSnapshot> _configs = new();
    private readonly ConcurrentDictionary<string, ClusterNodeSnapshot> _clusterNodes = new();
    private readonly ConcurrentDictionary<string, string> _defaultNode = new();
    private readonly ConcurrentDictionary<string, string> _leaderNode = new();

    public IReadOnlyCollection<ServiceSnapshot> GetServices() => _services.Values.ToList();

    public ServiceSnapshot? GetService(string name) =>
        _services.TryGetValue(name, out var service) ? service : null;

    public void UpsertService(ServiceSnapshot service)
    {
        _services[service.Name] = service;
    }

    public IReadOnlyCollection<ConfigSnapshot> GetConfigs() => _configs.Values.ToList();

    public ConfigSnapshot? GetConfig(string serviceName, string key, long version)
    {
        var id = BuildConfigId(serviceName, key);
        if (!_configs.TryGetValue(id, out var config))
        {
            return null;
        }

        if (version <= 0)
        {
            return config;
        }

        return config.Version <= version ? config : null;
    }

    public void UpsertConfig(ConfigSnapshot config)
    {
        var id = BuildConfigId(config.ServiceName, config.Key);
        _configs[id] = config;
    }

    public IReadOnlyCollection<ClusterNodeSnapshot> GetClusterNodes() => _clusterNodes.Values.ToList();

    public ClusterNodeSnapshot? GetDefaultNode() =>
        _defaultNode.TryGetValue("default", out var nodeId) && _clusterNodes.TryGetValue(nodeId, out var node)
            ? node
            : _clusterNodes.Values.FirstOrDefault();

    public ClusterNodeSnapshot? GetLeaderNode() =>
        _leaderNode.TryGetValue("leader", out var nodeId) && _clusterNodes.TryGetValue(nodeId, out var node)
            ? node
            : _clusterNodes.Values.FirstOrDefault();

    public void UpsertClusterNode(ClusterNodeSnapshot node)
    {
        _clusterNodes[node.Id] = node;

        if (node.Role == "leader")
        {
            _leaderNode["leader"] = node.Id;
        }

        if (node.IsDefault)
        {
            _defaultNode["default"] = node.Id;
        }
    }

    public void RemoveClusterNode(string id)
    {
        _clusterNodes.TryRemove(id, out _);
    }

    private static string BuildConfigId(string serviceName, string key) => $"{serviceName}:{key}";
}

public sealed class ServiceSnapshot
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
    public List<ServiceInstanceSnapshot> Instances { get; set; } = new();
}

public sealed class ServiceInstanceSnapshot
{
    public string InstanceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string NodeAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; }
    public string Status { get; set; } = "active";
    public DateTimeOffset LastHeartbeat { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ConfigSnapshot
{
    public string ServiceName { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public long Version { get; set; }
    public string Payload { get; set; } = "{}";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ClusterNodeSnapshot
{
    public string Id { get; set; } = string.Empty;
    public string Address { get; set; } = "127.0.0.1";
    public int Port { get; set; }
    public string Role { get; set; } = "follower";
    public string Status { get; set; } = "active";
    public bool IsDefault { get; set; }
}
