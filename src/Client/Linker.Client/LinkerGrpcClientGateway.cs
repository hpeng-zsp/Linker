using System.Collections.Concurrent;
using System.Threading.Channels;
using Grpc.Core;
using Grpc.Net.Client;
using Linker.Server.Protos;

namespace Linker.Client;

public sealed class LinkerGrpcClientGateway : IAsyncDisposable
{
    private readonly LinkerClientOptions _options;
    private readonly Channel<StreamRequest> _requestQueue;
    private readonly ConcurrentDictionary<string, LinkerClusterNode> _clusterNodes = new();
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly object _connectSync = new();

    private AsyncDuplexStreamingCall<StreamRequest, StreamResponse>? _call;
    private Task? _readLoop; 
    private Task? _writeLoop;
    private string _currentAddress = string.Empty;
    private int _currentPort;
    private int _reconnectAttempt;
    private bool _isConnected;

    public event Func<StreamResponse, Task>? OnStreamMessage;
    public event Func<string, Task>? OnReconnect;

    public LinkerGrpcClientGateway(LinkerClientOptions options)
    {
        _options = options;
        _requestQueue = Channel.CreateUnbounded<StreamRequest>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public IReadOnlyCollection<LinkerClusterNode> ClusterNodes => _clusterNodes.Values.ToList();

    public LinkerClusterNode? DefaultNode { get; private set; }

    public bool IsConnected => _isConnected;

    public async Task StartAsync()
    {
        var seeds = _options.SeedNodes.Count > 0
            ? _options.SeedNodes
            : [$"{_options.DefaultAddress}:{_options.DefaultPort}"];

        foreach (var seed in seeds)
        {
            if (await TryBootstrapAsync(seed))
            {
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(_currentAddress))
        {
            _currentAddress = _options.DefaultAddress;
            _currentPort = _options.DefaultPort;
        }

        await ConnectAsync();
    }

    public async Task StopAsync()
    {
        _shutdownCts.Cancel();
        _call?.Dispose();

        if (_writeLoop is not null)
        {
            await _writeLoop;
        }

        if (_readLoop is not null)
        {
            await _readLoop;
        }
    }

    public async Task SendAsync(string serviceName, string configKey, long version)
    {
        var request = new StreamRequest
        {
            ClientId = _options.ClientId,
            InstanceId = $"instance-{Guid.NewGuid():N}",
            ServiceName = serviceName,
            ConfigKey = configKey,
            Version = version,
            NodeAddress = _currentAddress,
            Port = _currentPort,
            IsBootstrap = false
        };

        await _requestQueue.Writer.WriteAsync(request, _shutdownCts.Token);
    }

    public async Task<ClusterBootstrapResponse?> BootstrapAsync()
    {
        var address = string.IsNullOrWhiteSpace(_currentAddress) ? _options.DefaultAddress : _currentAddress;
        var port = _currentPort == 0 ? _options.DefaultPort : _currentPort;

        try
        {
            using var channel = GrpcChannel.ForAddress(BuildAddress(address, port));
            var client = new LinkerCluster.LinkerClusterClient(channel);
            return await client.BootstrapAsync(new ClusterBootstrapRequest
            {
                ClientId = _options.ClientId,
                MicroserviceName = _options.ServiceName,
                SeedNodes = { _options.SeedNodes }
            });
        }
        catch
        {
            return null;
        }
    }

    private async Task<bool> TryBootstrapAsync(string seedNode)
    {
        try
        {
            var endpoint = ParseNode(seedNode);
            using var channel = GrpcChannel.ForAddress(BuildAddress(endpoint.address, endpoint.port));
            var client = new LinkerCluster.LinkerClusterClient(channel);
            var response = await client.BootstrapAsync(new ClusterBootstrapRequest
            {
                ClientId = _options.ClientId,
                MicroserviceName = _options.ServiceName,
                SeedNodes = { _options.SeedNodes }
            });

            foreach (var node in response.ClusterNodes)
            {
                _clusterNodes[node.Id] = new LinkerClusterNode
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
                _currentAddress = response.DefaultNode.Address;
                _currentPort = response.DefaultNode.Port;
                DefaultNode = new LinkerClusterNode
                {
                    Id = response.DefaultNode.Id,
                    Address = response.DefaultNode.Address,
                    Port = response.DefaultNode.Port,
                    Role = response.DefaultNode.Role,
                    Status = response.DefaultNode.Status
                };
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
        if (_shutdownCts.IsCancellationRequested)
        {
            return;
        }

        var address = string.IsNullOrWhiteSpace(_currentAddress) ? _options.DefaultAddress : _currentAddress;
        var port = _currentPort == 0 ? _options.DefaultPort : _currentPort;

        lock (_connectSync)
        {
            if (_call is not null)
            {
                _call.Dispose();
            }
        }

        var channel = GrpcChannel.ForAddress(BuildAddress(address, port));
        var client = new LinkerCluster.LinkerClusterClient(channel);
        var call = client.Connect();
        _call = call;
        _isConnected = true;

        var bootstrapRequest = new StreamRequest
        {
            ClientId = _options.ClientId,
            InstanceId = $"instance-{Guid.NewGuid():N}",
            ServiceName = _options.ServiceName,
            ConfigKey = _options.ConfigKey,
            Version = _options.Version,
            NodeAddress = address,
            Port = port,
            IsBootstrap = true
        };

        await call.RequestStream.WriteAsync(bootstrapRequest);

        _readLoop = Task.Run(async () =>
        {
            try
            {
                while (await call.ResponseStream.MoveNext(_shutdownCts.Token))
                {
                    var response = call.ResponseStream.Current;
                    if (OnStreamMessage is not null)
                    {
                        await OnStreamMessage(response);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // shutdown path
            }
            catch
            {
                _isConnected = false;
                await HandleReconnectAsync();
            }
        }, _shutdownCts.Token);

        _writeLoop = Task.Run(async () =>
        {
            await foreach (var request in _requestQueue.Reader.ReadAllAsync(_shutdownCts.Token))
            {
                try
                {
                    await call.RequestStream.WriteAsync(request);
                }
                catch
                {
                    _isConnected = false;
                    await HandleReconnectAsync();
                }
            }
        }, _shutdownCts.Token);
    }

    private async Task HandleReconnectAsync()
    {
        if (_shutdownCts.IsCancellationRequested)
        {
            return;
        }

        _reconnectAttempt++;
        if (_reconnectAttempt > _options.MaxReconnectAttempts)
        {
            _isConnected = false;
            return;
        }

        if (OnReconnect is not null)
        {
            await OnReconnect($"Attempt {_reconnectAttempt} of {_options.MaxReconnectAttempts}");
        }

        await Task.Delay(_options.ReconnectDelay * _reconnectAttempt, _shutdownCts.Token);
        await ConnectAsync();
    }

    private static string BuildAddress(string address, int port)
    {
        return $"http://{address}:{port}";
    }

    private static (string address, int port) ParseNode(string value)
    {
        var segments = value.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length != 2)
        {
            return ("127.0.0.1", 5001);
        }

        return (segments[0], int.Parse(segments[1]));
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _shutdownCts.Dispose();
        await ValueTask.CompletedTask;
    }
}
