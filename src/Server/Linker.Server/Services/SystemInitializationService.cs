using Microsoft.Data.Sqlite;
using SqlSugar;

namespace Linker.Server.Services;

public sealed class SystemInitializationService
{
    private readonly IConfiguration _configuration;
    private ISqlSugarClient _db;

    public SystemInitializationService(IConfiguration configuration)
    {
        _configuration = configuration;
        var dbTypeText = configuration.GetValue<string>("Database:Type") ?? "Sqlite";
        var connectionString = configuration.GetConnectionString("Default") ?? BuildDefaultSqliteConnectionString();

        if (string.Equals(dbTypeText, "Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            EnsureSqliteDirectory(connectionString);
        }

        _db = CreateSqlSugarClient(dbTypeText, connectionString);
    }

    public void EnsureDatabaseReady()
    {
        EnsureDatabaseReadyAsync().GetAwaiter().GetResult();
    }

    public async Task EnsureDatabaseReadyAsync()
    {
        _db.DbMaintenance.CreateDatabase();
        _db.CodeFirst.InitTables<SystemBootstrapRecord, DefaultUserRecord, ServiceRegistrationRecord, ServiceConfigRecord, RaftClusterStateRecord>();
        _db.CodeFirst.InitTables<RaftLogRecord>();
    }

    public async Task<bool> IsInitializedAsync()
    {
        return await _db.Queryable<SystemBootstrapRecord>()
            .AnyAsync(x => x.IsInitialized == true);
    }

    public async Task<bool> UpsertServiceRegistrationAsync(string serviceName, string instanceId, string nodeAddress, int port, string status = "active")
    {
        var now = DateTimeOffset.UtcNow;
        var existing = await _db.Queryable<ServiceRegistrationRecord>()
            .FirstAsync(x => x.ServiceName == serviceName && x.InstanceId == instanceId);

        if (existing is null)
        {
            await _db.Insertable(new ServiceRegistrationRecord
            {
                ServiceName = serviceName,
                InstanceId = instanceId,
                NodeAddress = nodeAddress,
                Port = port,
                Status = status,
                LastHeartbeat = now,
                CreatedAt = now,
                UpdatedAt = now
            }).ExecuteCommandAsync();
            return true;
        }

        existing.NodeAddress = nodeAddress;
        existing.Port = port;
        existing.Status = status;
        existing.LastHeartbeat = now;
        existing.UpdatedAt = now;
        await _db.Updateable(existing).ExecuteCommandAsync();
        return true;
    }

    public async Task<List<ServiceRegistrationRecord>> GetStoredServiceRegistrationsAsync(string? serviceName = null)
    {
        var query = _db.Queryable<ServiceRegistrationRecord>();
        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            query = query.Where(x => x.ServiceName == serviceName);
        }

        return await query.OrderBy(x => x.ServiceName).OrderBy(x => x.InstanceId).ToListAsync();
    }

    public async Task<long> GetNextConfigVersionAsync(string serviceName, string configKey)
    {
        var latest = await _db.Queryable<ServiceConfigRecord>()
            .Where(x => x.ServiceName == serviceName && x.ConfigKey == configKey)
            .OrderByDescending(x => x.Version)
            .FirstAsync();

        return latest is null ? 1 : latest.Version + 1;
    }

    public async Task<bool> UpsertConfigRecordAsync(string serviceName, string configKey, string payload, long version = 0)
    {
        var safeVersion = version > 0 ? version : await GetNextConfigVersionAsync(serviceName, configKey);
        var now = DateTimeOffset.UtcNow;

        var record = new ServiceConfigRecord
        {
            ServiceName = serviceName,
            ConfigKey = configKey,
            Version = safeVersion,
            Payload = payload,
            UpdatedAt = now,
            CreatedAt = now
        };

        await _db.Insertable(record).ExecuteCommandAsync();
        return true;
    }

    public async Task<ServiceConfigRecord?> GetStoredConfigAsync(string serviceName, string configKey, long version)
    {
        var query = _db.Queryable<ServiceConfigRecord>()
            .Where(x => x.ServiceName == serviceName && x.ConfigKey == configKey);

        return version > 0
            ? await query.Where(x => x.Version <= version).OrderByDescending(x => x.Version).FirstAsync()
            : await query.OrderByDescending(x => x.Version).FirstAsync();
    }

    public async Task WarmUpMemoryAsync(MemoryServiceController memoryServiceController)
    {
        var services = await GetStoredServiceRegistrationsAsync();
        foreach (var service in services)
        {
            memoryServiceController.RegisterServiceInstance(service.ServiceName, service.InstanceId, service.NodeAddress, service.Port);
        }

        var configs = await _db.Queryable<ServiceConfigRecord>().OrderBy(x => x.ServiceName).OrderBy(x => x.ConfigKey).OrderBy(x => x.Version).ToListAsync();
        foreach (var config in configs)
        {
            memoryServiceController.UpsertConfig(config.ServiceName, config.ConfigKey, config.Payload, config.Version);
        }
    }

    public async Task UpsertRaftStateAsync(RaftClusterState state)
    {
        var snapshot = state.GetSnapshot();
        var existing = await _db.Queryable<RaftClusterStateRecord>().FirstAsync(x => x.Id == 1);
        var record = new RaftClusterStateRecord
        {
            Id = 1,
            NodeId = snapshot.NodeId,
            Address = snapshot.Address,
            Port = snapshot.Port,
            Role = snapshot.Role.ToString(),
            CurrentTerm = snapshot.CurrentTerm,
            VotedFor = snapshot.VotedFor,
            CommitIndex = snapshot.CommitIndex,
            LastApplied = snapshot.LastApplied,
            LastLogIndex = snapshot.LastLogIndex,
            LastLogTerm = snapshot.LastLogTerm,
            PeerCount = snapshot.Peers.Count,
            Peers = string.Join("|", snapshot.Peers),
            UpdatedAt = DateTimeOffset.UtcNow
        };

        if (existing is null)
        {
            record.CreatedAt = record.UpdatedAt;
            await _db.Insertable(record).ExecuteCommandAsync();
        }
        else
        {
            record.CreatedAt = existing.CreatedAt;
            await _db.Updateable(record).ExecuteCommandAsync();
        }

        var logs = state.GetLog();
        await _db.Deleteable<RaftLogRecord>().Where(x => x.NodeId == snapshot.NodeId).ExecuteCommandAsync();
        if (logs.Count > 0)
        {
            var logRecords = logs.Select(x => new RaftLogRecord
            {
                NodeId = snapshot.NodeId,
                Index = x.Index,
                Term = x.Term,
                Command = x.Command,
                Payload = x.Payload,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            }).ToList();

            await _db.Insertable(logRecords).ExecuteCommandAsync();
        }
    }

    public async Task<RaftClusterStateRecord?> GetStoredRaftStateAsync()
    {
        return await _db.Queryable<RaftClusterStateRecord>().FirstAsync(x => x.Id == 1);
    }

    public async Task<List<RaftLogRecord>> GetStoredRaftLogsAsync(string? nodeId = null)
    {
        var query = _db.Queryable<RaftLogRecord>();
        if (!string.IsNullOrWhiteSpace(nodeId))
        {
            query = query.Where(x => x.NodeId == nodeId);
        }

        return await query.OrderBy(x => x.Index).ToListAsync();
    }

    public async Task WarmUpRaftStateAsync(ClusterRegistry clusterRegistry)
    {
        var persistedState = await GetStoredRaftStateAsync();
        if (persistedState is null)
        {
            return;
        }

        var logs = await GetStoredRaftLogsAsync(persistedState.NodeId);
        clusterRegistry.RestorePersistedState(persistedState, logs);
    }

    public async Task<SystemBootstrapRecord> InitializeAsync(
        string mode,
        string role,
        string clusterAddress,
        int clusterPort,
        string defaultUserName,
        string defaultPassword,
        string? nodeId = null,
        string? nodeName = null,
        string? leaderAddress = null,
        string? dataDirectory = null,
        string? publicEndpoint = null,
        string? clusterName = null,
        bool bootstrap = true,
        string? databaseType = null,
        string? databaseHost = null,
        int? databasePort = null,
        string? databaseName = null,
        string? databaseUsername = null,
        string? databasePassword = null)
    {
        var normalizedRole = string.Equals(role, "leader", StringComparison.OrdinalIgnoreCase)
            ? "leader"
            : string.Equals(role, "follower", StringComparison.OrdinalIgnoreCase)
                ? "follower"
                : string.Equals(mode, "secondary", StringComparison.OrdinalIgnoreCase)
                    ? "follower"
                    : "leader";

        var isFollowerRole = string.Equals(normalizedRole, "follower", StringComparison.OrdinalIgnoreCase);
        var options = DatabaseBootstrapOptions.From(databaseType, databaseHost, databasePort, databaseName, databaseUsername, databasePassword, dataDirectory);
        _db = CreateSqlSugarClient(options.Type, options.ConnectionString);
        await EnsureDatabaseReadyAsync();

        var existing = await _db.Queryable<SystemBootstrapRecord>()
            .FirstAsync(x => x.Id == 1);

        var now = DateTimeOffset.UtcNow;
        var persistedDatabaseType = existing?.DatabaseType ?? "Sqlite";
        var persistedDatabaseHost = existing?.DatabaseHost ?? "127.0.0.1";
        var persistedDatabasePort = existing?.DatabasePort ?? 3306;
        var persistedDatabaseName = existing?.DatabaseName ?? "linker";
        var persistedDatabaseUsername = existing?.DatabaseUsername ?? "root";
        var persistedDatabasePassword = existing?.DatabasePassword ?? "root";
        var persistedConnectionString = existing?.DatabaseConnectionString ?? options.ConnectionString;

        var effectiveType = isFollowerRole ? persistedDatabaseType : options.Type;
        var effectiveHost = isFollowerRole ? persistedDatabaseHost : options.Host;
        var effectivePort = isFollowerRole ? persistedDatabasePort : options.Port;
        var effectiveDatabaseName = isFollowerRole ? persistedDatabaseName : options.DatabaseName;
        var effectiveUsername = isFollowerRole ? persistedDatabaseUsername : options.Username;
        var effectivePassword = isFollowerRole ? persistedDatabasePassword : options.Password;
        var effectiveConnectionString = isFollowerRole ? persistedConnectionString : options.ConnectionString;

        if (existing is null)
        {
            existing = new SystemBootstrapRecord
            {
                Id = 1,
                IsInitialized = true,
                Mode = mode,
                Role = normalizedRole,
                ClusterAddress = clusterAddress,
                ClusterPort = clusterPort,
                NodeId = nodeId ?? "node-primary",
                NodeName = nodeName ?? "primary-node",
                LeaderAddress = leaderAddress,
                DataDirectory = dataDirectory ?? options.DataDirectory,
                PublicEndpoint = publicEndpoint ?? "http://127.0.0.1:8001/",
                ClusterName = clusterName ?? "linker-cluster",
                Bootstrap = bootstrap,
                DatabaseType = effectiveType,
                DatabaseHost = effectiveHost,
                DatabasePort = effectivePort,
                DatabaseName = effectiveDatabaseName,
                DatabaseUsername = effectiveUsername,
                DatabasePassword = effectivePassword,
                DatabaseConnectionString = effectiveConnectionString,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _db.Insertable(existing).ExecuteCommandAsync();
        }
        else
        {
            existing.IsInitialized = true;
            existing.Mode = mode;
            existing.Role = normalizedRole;
            existing.ClusterAddress = clusterAddress;
            existing.ClusterPort = clusterPort;
            existing.NodeId = nodeId ?? existing.NodeId;
            existing.NodeName = nodeName ?? existing.NodeName;
            existing.LeaderAddress = leaderAddress ?? existing.LeaderAddress;
            existing.DataDirectory = dataDirectory ?? existing.DataDirectory;
            existing.PublicEndpoint = publicEndpoint ?? existing.PublicEndpoint;
            existing.ClusterName = clusterName ?? existing.ClusterName;
            existing.Bootstrap = bootstrap;
            existing.DatabaseType = effectiveType;
            existing.DatabaseHost = effectiveHost;
            existing.DatabasePort = effectivePort;
            existing.DatabaseName = effectiveDatabaseName;
            existing.DatabaseUsername = effectiveUsername;
            existing.DatabasePassword = effectivePassword;
            existing.DatabaseConnectionString = effectiveConnectionString;
            existing.UpdatedAt = now;
            await _db.Updateable(existing).ExecuteCommandAsync();
        }

        var defaultUser = new DefaultUserRecord
        {
            Id = 1,
            Username = defaultUserName,
            Password = defaultPassword,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var userExists = await _db.Queryable<DefaultUserRecord>().AnyAsync(x => x.Id == 1);
        if (userExists)
        {
            await _db.Updateable(defaultUser).ExecuteCommandAsync();
        }
        else
        {
            await _db.Insertable(defaultUser).ExecuteCommandAsync();
        }

        return existing;
    }

    public async Task<SystemBootstrapRecord?> GetCurrentStateAsync()
    {
        return await _db.Queryable<SystemBootstrapRecord>()
            .FirstAsync(x => x.Id == 1);
    }

    private static void EnsureSqliteDirectory(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        var target = builder.DataSource;
        if (string.IsNullOrWhiteSpace(target))
        {
            return;
        }

        var filePath = Path.IsPathRooted(target)
            ? target
            : Path.Combine(Directory.GetCurrentDirectory(), target);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string BuildDefaultSqliteConnectionString()
    {
        var directory = Path.Combine(Directory.GetCurrentDirectory(), "data");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, "linker.db");
        return new SqliteConnectionStringBuilder
        {
            DataSource = filePath
        }.ConnectionString;
    }

    private static DbType ParseDbType(string dbTypeText)
    {
        return dbTypeText.Trim() switch
        {
            "MySql" => DbType.MySql,
            "MSSql" => DbType.SqlServer,
            "PostgreSQL" => DbType.PostgreSQL,
            "SqlServer" => DbType.SqlServer,
            "Sqlite" => DbType.Sqlite,
            _ => DbType.Sqlite
        };
    }

    private static ISqlSugarClient CreateSqlSugarClient(string dbTypeText, string connectionString)
    {
        return new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connectionString,
            DbType = ParseDbType(dbTypeText),
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });
    }
}

[SugarTable("SystemBootstrap")]
public sealed class SystemBootstrapRecord
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public int Id { get; set; }

    public bool IsInitialized { get; set; }
    public string Mode { get; set; } = "primary";
    public string Role { get; set; } = "primary";
    public string ClusterAddress { get; set; } = "10.0.0.10";
    public int ClusterPort { get; set; } = 8001;
    public string NodeId { get; set; } = "node-primary";
    public string NodeName { get; set; } = "primary-node";
    public string? LeaderAddress { get; set; }
    public string DataDirectory { get; set; } = "data";
    public string PublicEndpoint { get; set; } = "http://127.0.0.1:8001/";
    public string ClusterName { get; set; } = "linker-cluster";
    public bool Bootstrap { get; set; } = true;
    public string DatabaseType { get; set; } = "Sqlite";
    public string DatabaseHost { get; set; } = "127.0.0.1";
    public int DatabasePort { get; set; } = 3306;
    public string DatabaseName { get; set; } = "linker";
    public string DatabaseUsername { get; set; } = "root";
    public string DatabasePassword { get; set; } = "root";
    public string DatabaseConnectionString { get; set; } = "Data Source=data/linker.db";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[SugarTable("DefaultUsers")]
public sealed class DefaultUserRecord
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public int Id { get; set; }

    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin123";
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[SugarTable("ServiceRegistrations")]
public sealed class ServiceRegistrationRecord
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public string ServiceName { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string NodeAddress { get; set; } = "127.0.0.1";
    public int Port { get; set; }
    public string Status { get; set; } = "active";
    public DateTimeOffset LastHeartbeat { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[SugarTable("ServiceConfigs")]
public sealed class ServiceConfigRecord
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public string ServiceName { get; set; } = string.Empty;
    public string ConfigKey { get; set; } = string.Empty;
    public long Version { get; set; }
    public string Payload { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[SugarTable("RaftClusterState")]
public sealed class RaftClusterStateRecord
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = false)]
    public int Id { get; set; } = 1;

    public string NodeId { get; set; } = "node-primary";
    public string Address { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 5001;
    public string Role { get; set; } = "Leader";
    public long CurrentTerm { get; set; }
    public long VotedFor { get; set; }
    public long CommitIndex { get; set; }
    public long LastApplied { get; set; }
    public long LastLogIndex { get; set; }
    public long LastLogTerm { get; set; }
    public int PeerCount { get; set; }
    public string Peers { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[SugarTable("RaftClusterLogs")]
public sealed class RaftLogRecord
{
    [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
    public int Id { get; set; }

    public string NodeId { get; set; } = "node-primary";
    public long Index { get; set; }
    public long Term { get; set; }
    public string Command { get; set; } = string.Empty;
    public string Payload { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
