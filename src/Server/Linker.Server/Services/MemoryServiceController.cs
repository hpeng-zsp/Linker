using Linker.Server.Memory;

namespace Linker.Server.Services;

public sealed class MemoryServiceController
{
    private readonly MemoryRegistry _memoryRegistry;
    private readonly SystemInitializationService _systemInitializationService;

    public MemoryServiceController(MemoryRegistry memoryRegistry, SystemInitializationService systemInitializationService)
    {
        _memoryRegistry = memoryRegistry;
        _systemInitializationService = systemInitializationService;
    }

    public void RegisterServiceInstance(string serviceName, string instanceId, string nodeAddress, int port)
    {
        var services = _memoryRegistry.GetServices();
        var service = services.FirstOrDefault(x => x.Name == serviceName);

        if (service is null)
        {
            service = new ServiceSnapshot
            {
                Name = serviceName,
                Status = "active",
                LastUpdated = DateTimeOffset.UtcNow,
                Instances = new List<ServiceInstanceSnapshot>()
            };
        }

        var instance = service.Instances.FirstOrDefault(x => x.InstanceId == instanceId);
        if (instance is null)
        {
            service.Instances.Add(new ServiceInstanceSnapshot
            {
                InstanceId = instanceId,
                ServiceName = serviceName,
                NodeAddress = nodeAddress,
                Port = port,
                Status = "active",
                LastHeartbeat = DateTimeOffset.UtcNow
            });
        }
        else
        {
            instance.NodeAddress = nodeAddress;
            instance.Port = port;
            instance.Status = "active";
            instance.LastHeartbeat = DateTimeOffset.UtcNow;
        }

        service.LastUpdated = DateTimeOffset.UtcNow;
        _memoryRegistry.UpsertService(service);

        _ = _systemInitializationService.UpsertServiceRegistrationAsync(serviceName, instanceId, nodeAddress, port).GetAwaiter().GetResult();
    }

    public IReadOnlyCollection<ServiceSnapshot> GetServices() => _memoryRegistry.GetServices();

    public void UpsertConfig(string serviceName, string configKey, string payload, long version)
    {
        var config = new ConfigSnapshot
        {
            ServiceName = serviceName,
            Key = configKey,
            Version = version > 0 ? version : 1,
            Payload = payload,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _memoryRegistry.UpsertConfig(config);
        _ = _systemInitializationService.UpsertConfigRecordAsync(serviceName, configKey, payload, version).GetAwaiter().GetResult();
    }

    public ConfigSnapshot? GetConfig(string serviceName, string configKey, long version)
    {
        var config = _memoryRegistry.GetConfig(serviceName, configKey, version);
        if (config is not null)
        {
            return config;
        }

        var persisted = _systemInitializationService.GetStoredConfigAsync(serviceName, configKey, version).GetAwaiter().GetResult();
        if (persisted is null)
        {
            return null;
        }

        var hydrated = new ConfigSnapshot
        {
            ServiceName = persisted.ServiceName,
            Key = persisted.ConfigKey,
            Version = persisted.Version,
            Payload = persisted.Payload,
            UpdatedAt = persisted.UpdatedAt
        };

        _memoryRegistry.UpsertConfig(hydrated);
        return hydrated;
    }

    public void RegisterClusterNode(string id, string address, int port, string role, bool isDefault)
    {
        _memoryRegistry.UpsertClusterNode(new ClusterNodeSnapshot
        {
            Id = id,
            Address = address,
            Port = port,
            Role = role,
            Status = "active",
            IsDefault = isDefault
        });
    }
}
