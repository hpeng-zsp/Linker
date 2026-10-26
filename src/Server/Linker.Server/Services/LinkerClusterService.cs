using Google.Protobuf.Collections;
using Grpc.Core;
using Linker.Server.Memory;
using Linker.Server.Protos;

namespace Linker.Server.Services;

public sealed class LinkerClusterService : LinkerCluster.LinkerClusterBase
{
    private readonly MemoryRegistry _memoryRegistry;
    private readonly MemoryServiceController _memoryServiceController;
    private readonly ClusterRegistry _clusterRegistry;
    private readonly ServiceRegistry _serviceRegistry;
    private readonly ConfigStore _configStore;
    private readonly ReconnectPolicy _reconnectPolicy;
    private readonly StreamConnectionManager _streamConnectionManager;

    public LinkerClusterService(
        MemoryRegistry memoryRegistry,
        MemoryServiceController memoryServiceController,
        ClusterRegistry clusterRegistry,
        ServiceRegistry serviceRegistry,
        ConfigStore configStore,
        ReconnectPolicy reconnectPolicy,
        StreamConnectionManager streamConnectionManager)
    {
        _memoryRegistry = memoryRegistry;
        _memoryServiceController = memoryServiceController;
        _clusterRegistry = clusterRegistry;
        _serviceRegistry = serviceRegistry;
        _configStore = configStore;
        _reconnectPolicy = reconnectPolicy;
        _streamConnectionManager = streamConnectionManager;
    }

    public override Task<ClusterBootstrapResponse> Bootstrap(ClusterBootstrapRequest request, ServerCallContext context)
    {
        var clusterNodes = _clusterRegistry.GetAll()
            .Select(node => new ClusterNode
            {
                Id = node.Id,
                Address = node.Address,
                Port = node.Port,
                Role = node.Role,
                Status = node.Status
            })
            .ToList();

        var defaultNode = _clusterRegistry.GetDefaultNode();
        var leaderNode = _clusterRegistry.GetAll().FirstOrDefault(x => x.Role == "leader") ?? defaultNode;

        var response = new ClusterBootstrapResponse
        {
            ClusterVersion = _clusterRegistry.GetClusterVersion(),
            DefaultNode = new ClusterNode
            {
                Id = defaultNode.Id,
                Address = defaultNode.Address,
                Port = defaultNode.Port,
                Role = defaultNode.Role,
                Status = defaultNode.Status
            },
            LeaderNode = new ClusterNode
            {
                Id = leaderNode.Id,
                Address = leaderNode.Address,
                Port = leaderNode.Port,
                Role = leaderNode.Role,
                Status = leaderNode.Status
            }
        };

        response.ClusterNodes.AddRange(clusterNodes);
        return Task.FromResult(response);
    }

    public override async Task Connect(IAsyncStreamReader<StreamRequest> requestStream, IServerStreamWriter<StreamResponse> responseStream, ServerCallContext context)
    {
        while (await requestStream.MoveNext(context.CancellationToken))
        {
            var request = requestStream.Current;
            var clientId = string.IsNullOrWhiteSpace(request.ClientId) ? $"client-{Guid.NewGuid():N}" : request.ClientId;
            var instanceId = string.IsNullOrWhiteSpace(request.InstanceId) ? $"instance-{Guid.NewGuid():N}" : request.InstanceId;

            if (request.IsBootstrap)
            {
                var boot = BuildStreamResponse(request, clientId, instanceId, true, "bootstrap");
                await responseStream.WriteAsync(boot);
                continue;
            }

            _streamConnectionManager.Register(clientId, instanceId, request.ServiceName);
            _memoryServiceController.RegisterServiceInstance(request.ServiceName, instanceId, request.NodeAddress, request.Port);

            var latestConfig = _memoryServiceController.GetConfig(request.ServiceName, request.ConfigKey, request.Version);
            var configPayload = latestConfig is not null
                ? latestConfig.Payload
                : "{}";

            var response = BuildStreamResponse(request, clientId, instanceId, true, configPayload);
            response.InstanceId = instanceId;
            response.Accepted = true;
            response.ConfigVersion = request.Version <= 0 ? 1 : request.Version;
            response.ReconnectRequired = false;

            await responseStream.WriteAsync(response);
        }

        _streamConnectionManager.Remove(string.Empty);
        _serviceRegistry.MarkDisconnected(string.Empty, string.Empty);
    }

    private StreamResponse BuildStreamResponse(StreamRequest request, string clientId, string instanceId, bool accepted, string configPayload)
    {
        var clusterNodes = _clusterRegistry.GetAll()
            .Select(x => new ClusterNode
            {
                Id = x.Id,
                Address = x.Address,
                Port = x.Port,
                Role = x.Role,
                Status = x.Status
            })
            .ToList();

        var defaultNode = _clusterRegistry.GetDefaultNode();
        var response = new StreamResponse
        {
            InstanceId = instanceId,
            ServiceName = request.ServiceName,
            Accepted = accepted,
            ConfigPayload = configPayload,
            ConfigVersion = request.Version <= 0 ? 1 : request.Version,
            ReconnectRequired = false,
            DefaultNode = new ClusterNode
            {
                Id = defaultNode.Id,
                Address = defaultNode.Address,
                Port = defaultNode.Port,
                Role = defaultNode.Role,
                Status = defaultNode.Status
            }
        };

        response.ClusterNodes.AddRange(clusterNodes);
        return response;
    }
}
