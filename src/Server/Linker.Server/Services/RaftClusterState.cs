namespace Linker.Server.Services;

public enum RaftRole
{
    Follower,
    Candidate,
    Leader
}

public sealed record RaftLogEntry(long Index, long Term, string Command, string Payload);

public sealed record RaftClusterSnapshot(
    string NodeId,
    string Address,
    int Port,
    RaftRole Role,
    long CurrentTerm,
    long VotedFor,
    long CommitIndex,
    long LastApplied,
    long LastLogIndex,
    long LastLogTerm,
    IReadOnlyCollection<string> Peers);

public sealed class RaftClusterState
{
    private readonly object _sync = new();
    private readonly List<RaftLogEntry> _log = [];
    private readonly HashSet<string> _peers = [];

    public string NodeId { get; private set; } = "node-primary";
    public string Address { get; private set; } = "127.0.0.1";
    public int Port { get; private set; } = 5001;
    public RaftRole Role { get; private set; } = RaftRole.Follower;
    public long CurrentTerm { get; private set; }
    public long VotedFor { get; private set; }
    public long CommitIndex { get; private set; }
    public long LastApplied { get; private set; }
    public long LastLogIndex => _log.Count;
    public long LastLogTerm => _log.Count == 0 ? 0 : _log[^1].Term;
    public int PeerCount => _peers.Count;
    public IReadOnlyCollection<string> Peers => _peers.ToList();

    public void Initialize(string address, int port, IEnumerable<string> peers)
    {
        lock (_sync)
        {
            Address = address;
            Port = port;
            NodeId = $"node-{address}:{port}";
            _peers.Clear();
            foreach (var peer in peers.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                _peers.Add(peer.Trim());
            }

            if (_peers.Count == 0)
            {
                _peers.Add($"{address}:{port}");
            }

            Role = RaftRole.Follower;
            CurrentTerm = 0;
            VotedFor = -1;
            CommitIndex = 0;
            LastApplied = 0;
            _log.Clear();
        }
    }

    public void StartElection()
    {
        lock (_sync)
        {
            CurrentTerm += 1;
            VotedFor = 1;
            Role = RaftRole.Candidate;

            if (_peers.Count <= 1)
            {
                BecomeLeader();
                return;
            }

            var quorum = GetQuorumSize();
            if (quorum <= 1)
            {
                BecomeLeader();
            }
        }
    }

    public void AppendLog(string command, string payload)
    {
        lock (_sync)
        {
            if (Role != RaftRole.Leader)
            {
                throw new InvalidOperationException("Only the leader can append to the Raft log.");
            }

            var index = _log.Count + 1;
            var term = CurrentTerm;
            _log.Add(new RaftLogEntry(index, term, command, payload));
            CommitIndex = index;
        }
    }

    public IReadOnlyList<RaftLogEntry> GetLog()
    {
        lock (_sync)
        {
            return _log.ToList();
        }
    }

    public void BecomeLeader()
    {
        lock (_sync)
        {
            Role = RaftRole.Leader;
            VotedFor = 0;
        }
    }

    public void BecomeFollower(long term)
    {
        lock (_sync)
        {
            if (term > CurrentTerm)
            {
                CurrentTerm = term;
            }

            Role = RaftRole.Follower;
            VotedFor = -1;
        }
    }

    public long GetClusterVersion()
    {
        lock (_sync)
        {
            return CurrentTerm;
        }
    }

    public RaftClusterSnapshot GetSnapshot()
    {
        lock (_sync)
        {
            return new RaftClusterSnapshot(
                NodeId,
                Address,
                Port,
                Role,
                CurrentTerm,
                VotedFor,
                CommitIndex,
                LastApplied,
                LastLogIndex,
                LastLogTerm,
                _peers.ToList());
        }
    }

    public void RestoreFromSnapshot(RaftClusterSnapshot snapshot, IReadOnlyCollection<RaftLogEntry>? logs = null)
    {
        lock (_sync)
        {
            NodeId = snapshot.NodeId;
            Address = snapshot.Address;
            Port = snapshot.Port;
            Role = snapshot.Role;
            CurrentTerm = snapshot.CurrentTerm;
            VotedFor = snapshot.VotedFor;
            CommitIndex = snapshot.CommitIndex;
            LastApplied = snapshot.LastApplied;
            _peers.Clear();
            foreach (var peer in snapshot.Peers)
            {
                if (!string.IsNullOrWhiteSpace(peer))
                {
                    _peers.Add(peer.Trim());
                }
            }

            _log.Clear();
            if (logs is not null)
            {
                foreach (var entry in logs.OrderBy(x => x.Index))
                {
                    _log.Add(entry);
                }
            }
        }
    }

    private int GetQuorumSize()
    {
        var size = _peers.Count + 1;
        return Math.Max(1, (size / 2) + 1);
    }
}
