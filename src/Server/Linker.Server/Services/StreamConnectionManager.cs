using System.Collections.Concurrent;

namespace Linker.Server.Services;

public sealed class StreamConnectionManager
{
    private readonly ConcurrentDictionary<string, StreamConnectionState> _connections = new();

    public void Register(string clientId, string instanceId, string serviceName)
    {
        _connections[clientId] = new StreamConnectionState
        {
            ClientId = clientId,
            InstanceId = instanceId,
            ServiceName = serviceName,
            ConnectedAt = DateTimeOffset.UtcNow,
            LastHeartbeat = DateTimeOffset.UtcNow
        };
    }

    public void UpdateHeartbeat(string clientId)
    {
        if (_connections.TryGetValue(clientId, out var state))
        {
            state.LastHeartbeat = DateTimeOffset.UtcNow;
        }
    }

    public void Remove(string clientId)
    {
        _connections.TryRemove(clientId, out _);
    }

    public bool RequiresReconnect(string clientId)
    {
        return _connections.ContainsKey(clientId) == false;
    }
}

public sealed class StreamConnectionState
{
    public string ClientId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public DateTimeOffset ConnectedAt { get; set; }
    public DateTimeOffset LastHeartbeat { get; set; }
}
