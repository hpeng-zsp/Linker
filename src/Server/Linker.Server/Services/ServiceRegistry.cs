namespace Linker.Server.Services;

public sealed record ServiceInstance(
    string InstanceId,
    string ServiceName,
    string NodeAddress,
    int Port,
    DateTimeOffset LastHeartbeat,
    string Status = "active");

public sealed class ServiceRegistry
{
    private readonly object _sync = new();
    private readonly Dictionary<string, List<ServiceInstance>> _instances = new();

    public IReadOnlyCollection<ServiceInstance> GetInstances(string serviceName)
    {
        lock (_sync)
        {
            return _instances.TryGetValue(serviceName, out var list)
                ? list
                : Array.Empty<ServiceInstance>();
        }
    }

    public ServiceInstance Register(string serviceName, string instanceId, string nodeAddress, int port)
    {
        lock (_sync)
        {
            if (!_instances.TryGetValue(serviceName, out var list))
            {
                list = new List<ServiceInstance>();
                _instances[serviceName] = list;
            }

            var existing = list.FirstOrDefault(x => x.InstanceId == instanceId);
            if (existing is not null)
            {
                var updated = existing with { NodeAddress = nodeAddress, Port = port, LastHeartbeat = DateTimeOffset.UtcNow, Status = "active" };
                var index = list.IndexOf(existing);
                list[index] = updated;
                return updated;
            }

            var instance = new ServiceInstance(instanceId, serviceName, nodeAddress, port, DateTimeOffset.UtcNow, "active");
            list.Add(instance);
            return instance;
        }
    }

    public void MarkDisconnected(string serviceName, string instanceId)
    {
        lock (_sync)
        {
            if (!_instances.TryGetValue(serviceName, out var list))
            {
                return;
            }

            var index = list.FindIndex(x => x.InstanceId == instanceId);
            if (index >= 0)
            {
                var item = list[index];
                list[index] = item with { Status = "disconnected", LastHeartbeat = DateTimeOffset.UtcNow };
            }
        }
    }
}
