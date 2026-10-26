namespace Linker.Server.Services;

public sealed record ClusterNodeInfo(
    string Id,
    string Address,
    int Port,
    string Role,
    string Status = "active");

public sealed class ClusterRegistry
{
    private readonly object _sync = new();
    private readonly List<ClusterNodeInfo> _nodes = [];
    private readonly RaftClusterState _raftState;
    private readonly SystemInitializationService? _systemInitializationService;

    public ClusterRegistry(IConfiguration configuration, SystemInitializationService? systemInitializationService = null)
    {
        _systemInitializationService = systemInitializationService;
        var seedNodes = configuration.GetSection("Cluster:SeedNodes").Get<string[]>() ?? ["127.0.0.1:5001"];
        _raftState = new RaftClusterState();

        if (_systemInitializationService is not null)
        {
            _systemInitializationService.EnsureDatabaseReady();
            var persistedState = _systemInitializationService.GetStoredRaftStateAsync().GetAwaiter().GetResult();
            var persistedLogs = persistedState is null
                ? new List<RaftLogRecord>()
                : _systemInitializationService.GetStoredRaftLogsAsync(persistedState.NodeId).GetAwaiter().GetResult();

            if (persistedState is not null)
            {
                _raftState.RestoreFromSnapshot(
                    new RaftClusterSnapshot(
                        persistedState.NodeId,
                        persistedState.Address,
                        persistedState.Port,
                        Enum.TryParse<RaftRole>(persistedState.Role, ignoreCase: true, out var role) ? role : RaftRole.Leader,
                        persistedState.CurrentTerm,
                        persistedState.VotedFor,
                        persistedState.CommitIndex,
                        persistedState.LastApplied,
                        persistedState.LastLogIndex,
                        persistedState.LastLogTerm,
                        persistedState.Peers.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()),
                    persistedLogs.Select(x => new RaftLogEntry(x.Index, x.Term, x.Command, x.Payload)).ToList());
            }
            else
            {
                _raftState.Initialize("127.0.0.1", 5001, seedNodes);
                _raftState.StartElection();
            }
        }
        else
        {
            _raftState.Initialize("127.0.0.1", 5001, seedNodes);
            _raftState.StartElection();
        }

        foreach (var seed in seedNodes)
        {
            var parsed = ParseSeedNode(seed);
            AddOrUpdateNode(parsed.address, parsed.port, "follower");
        }

        if (_nodes.Count == 0)
        {
            AddOrUpdateNode("127.0.0.1", 5001, "leader");
        }

        PersistState();
    }

    public IReadOnlyCollection<ClusterNodeInfo> GetAll()
    {
        lock (_sync)
        {
            return _nodes.ToList();
        }
    }

    public ClusterNodeInfo GetDefaultNode()
    {
        lock (_sync)
        {
            var leader = _nodes.FirstOrDefault(x => x.Role == "leader");
            return leader ?? _nodes.FirstOrDefault() ?? new ClusterNodeInfo("local", "127.0.0.1", 5001, "leader");
        }
    }

    public void AddOrUpdateNode(string address, int port, string role)
    {
        lock (_sync)
        {
            var existing = _nodes.FirstOrDefault(x => x.Address == address && x.Port == port);
            if (existing is not null)
            {
                var index = _nodes.IndexOf(existing);
                _nodes[index] = existing with { Role = role, Status = "active" };
                PersistState();
                return;
            }

            _nodes.Add(new ClusterNodeInfo(
                Id: $"node-{Guid.NewGuid():N}",
                Address: address,
                Port: port,
                Role: role,
                Status: "active"));
            PersistState();
        }
    }

    public void RestorePersistedState(RaftClusterStateRecord record, IReadOnlyCollection<RaftLogRecord> logs)
    {
        var peers = record.Peers.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var snapshot = new RaftClusterSnapshot(
            record.NodeId,
            record.Address,
            record.Port,
            Enum.TryParse<RaftRole>(record.Role, ignoreCase: true, out var role) ? role : RaftRole.Follower,
            record.CurrentTerm,
            record.VotedFor,
            record.CommitIndex,
            record.LastApplied,
            record.LastLogIndex,
            record.LastLogTerm,
            peers.ToList());

        _raftState.RestoreFromSnapshot(snapshot, logs.Select(x => new RaftLogEntry(x.Index, x.Term, x.Command, x.Payload)).ToList());
    }

    private void PersistState()
    {
        if (_systemInitializationService is not null)
        {
            _systemInitializationService.UpsertRaftStateAsync(_raftState).GetAwaiter().GetResult();
        }
    }

    public long GetClusterVersion()
    {
        return _raftState.GetClusterVersion();
    }

    public RaftClusterState GetRaftState()
    {
        return _raftState;
    }

    private static (string address, int port) ParseSeedNode(string seedNode)
    {
        var segments = seedNode.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 2)
        {
            return ("127.0.0.1", 5001);
        }

        return (segments[0], int.Parse(segments[1]));
    }
}
