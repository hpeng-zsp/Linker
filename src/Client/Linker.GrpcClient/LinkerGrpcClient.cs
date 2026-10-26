using System.Collections.Concurrent;
using System.Threading.Channels;
using Grpc.Core;
using Grpc.Net.Client;
using Linker.Server.Protos;

namespace Linker.GrpcClient;

public sealed class ClusterNodeInfo
{
    public string Id { get; set; } = string.Empty;
    public string Address { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5001;
    public string Role { get; set; } = "follower";
    public string Status { get; set; } = "active";
}

public sealed class LinkerClientOptions
{
    public string ClientId { get; set; } = "client-default";
    public string ServiceName { get; set; } = "user-service";
    public string ConfigKey { get; set; } = "appsettings";
    public long Version { get; set; }
    public string DefaultAddress { get; set; } = "127.0.0.1";
    public int DefaultPort { get; set; } = 5001;
    public List<string> SeedNodes { get; set; } = new();
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(2);
    public int MaxReconnectAttempts { get; set; } = 10;
}

public sealed class ReconnectState
{
    public int Attempt { get; set; }
    public DateTimeOffset LastAttemptUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class LinkerGrpcClient : IAsyncDisposable
{
    private readonly LinkerClientOptions _options;
    private readonly Channel<StreamRequest> _requestQueue;
    private readonly ConcurrentDictionary<string, ClusterNodeInfo> _clusterNodes = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly SemaphoreSlim _streamLock = new(1, 1);
    private readonly ReconnectState _reconnectState = new();

    private AsyncDuplexStreamingCall<StreamRequest, StreamResponse>? _call;
    private Task? _readLoopTask;
    private string _currentNodeAddress = string.Empty;
    private int _currentPort;

    public event Func<StreamResponse, Task>? OnMessageReceived;
    public event Func<string, Task>? OnReconnect;

    public LinkerGrpcClient(LinkerClientOptions options)
    {
        _options = options;
        _requestQueue = Channel.CreateUnbounded<StreamRequest>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public IReadOnlyCollection<ClusterNodeInfo> ClusterNodes => _clusterNodes.Values.ToList();

    public async Task StartAsync()
    {
        var seedNodes = _options.SeedNodes.Count > 0
            ? _options.SeedNodes
            : [$"{_options.DefaultAddress}:{_options.DefaultPort}"];

        foreach (var node in seedNodes)
        {
            if (await TryBootstrapAsync(node))
            {
                break;
            }
        }

        await ConnectAsync();
    }

    public async Task StopAsync()
    {
        _cts.Cancel();
        _call?.Dispose();
        await _streamLock.WaitAsync();
        _streamLock.Release();
    }

    public async Task SendServiceHeartbeatAsync(string serviceName, string configKey, long version)
    {
        var request = new StreamRequest
        {
            ClientId = _options.ClientId,
            InstanceId = $"instance-{Guid.NewGuid():N}",
            ServiceName = serviceName,
            ConfigKey = configKey,
            Version = version,
            NodeAddress = _currentNodeAddress,
            Port = _currentPort,
            IsBootstrap = false
        };

        await _requestQueue.Writer.WriteAsync(request, _cts.Token);
    }

    private async Task<bool> TryBootstrapAsync(string seedNode)
    {
        try
        {
            var endpoint = BuildChannelAddress(seedNode);
            using var channel = GrpcChannel.ForAddress(endpoint);
            var client = new LinkerCluster.LinkerClusterClient(channel);
            var response = await client.BootstrapAsync(new ClusterBootstrapRequest
            {
                ClientId = _options.ClientId,
                MicroserviceName = _options.ServiceName,
                SeedNodes = { _options.SeedNodes }
            });

            foreach (var node in response.ClusterNodes)
            {
                _clusterNodes[node.Id] = new ClusterNodeInfo
                {
                    Id = node.Id,
                    Address = node.Address,
                    Port = node.Port,
                    Role = node.Role,
                    Status = node.Status
                };
            }

            if (response.DefaultNode is not null)
            {
                _currentNodeAddress = response.DefaultNode.Address;
                _currentPort = response.DefaultNode.Port;
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task ConnectAsync()
    {
        if (string.IsNullOrWhiteSpace(_currentNodeAddress))
        {
            _currentNodeAddress = _options.DefaultAddress;
            _currentPort = _options.DefaultPort;
        }

        var endpoint = BuildChannelAddress(_currentNodeAddress, _currentPort);
        var channel = GrpcChannel.ForAddress(endpoint);
        var client = new LinkerCluster.LinkerClusterClient(channel);

        var call = client.Connect();
        _call = call;

        _readLoopTask = Task.Run(async () =>
        {
            try
            {
                while (await call.ResponseStream.MoveNext(_cts.Token))
                {
                    var payload = call.ResponseStream.Current;
                    if (OnMessageReceived is not null)
                    {
                        await OnMessageReceived(payload);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // stream canceled by shutdown or reconnect path
            }
            catch
            {
                await HandleReconnectAsync();
            }
        }, _cts.Token);

        var bootstrapRequest = new StreamRequest
        {
            ClientId = _options.ClientId,
            InstanceId = $"instance-{Guid.NewGuid():N}",
            ServiceName = _options.ServiceName,
            ConfigKey = _options.ConfigKey,
            Version = _options.Version,
            NodeAddress = _currentNodeAddress,
            Port = _currentPort,
            IsBootstrap = true
        };

        await call.RequestStream.WriteAsync(bootstrapRequest);

        _ = Task.Run(async () =>
        {
            await foreach (var request in _requestQueue.Reader.ReadAllAsync(_cts.Token))
            {
                try
                {
                    await call.RequestStream.WriteAsync(request);
                }
                catch
                {
                    await HandleReconnectAsync();
                }
            }
        }, _cts.Token);
    }

    private async Task HandleReconnectAsync()
    {
        if (_cts.IsCancellationRequested)
        {
            return;
        }

        _reconnectState.Attempt++;
        _reconnectState.LastAttemptUtc = DateTimeOffset.UtcNow;
        if (OnReconnect is not null)
        {
            await OnReconnect($"Reconnect attempt {_reconnectState.Attempt}");
        }

        if (_reconnectState.Attempt > _options.MaxReconnectAttempts)
        {
            return;
        }

        await Task.Delay(_options.ReconnectDelay * _reconnectState.Attempt);

        try
        {
            _call?.Dispose();
            await ConnectAsync();
        }
        catch
        {
            await HandleReconnectAsync();
        }
    }

    private static string BuildChannelAddress(string address, int? port = null)
    {
        var targetPort = port ?? 5001;
        return $"http://{address}:{targetPort}";
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _call?.Dispose();
        _readLoopTask?.Dispose();
        _streamLock.Dispose();
        await ValueTask.CompletedTask;
    }
}
