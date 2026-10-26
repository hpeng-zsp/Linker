using Linker.Server.Memory;
using Linker.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddSingleton<MemoryRegistry>();
builder.Services.AddSingleton<MemoryServiceController>();
builder.Services.AddSingleton<ClusterRegistry>();
builder.Services.AddSingleton<ServiceRegistry>();
builder.Services.AddSingleton<ConfigStore>();
builder.Services.AddSingleton<ReconnectPolicy>();
builder.Services.AddSingleton<StreamConnectionManager>();
builder.Services.AddSingleton<SystemInitializationService>();

var app = builder.Build();

var systemInitializer = app.Services.GetRequiredService<SystemInitializationService>();
var memoryServiceController = app.Services.GetRequiredService<MemoryServiceController>();
var clusterRegistry = app.Services.GetRequiredService<ClusterRegistry>();

await systemInitializer.EnsureDatabaseReadyAsync();
await systemInitializer.WarmUpMemoryAsync(memoryServiceController);
await systemInitializer.WarmUpRaftStateAsync(clusterRegistry);

app.MapGrpcService<LinkerClusterService>();
app.MapGet("/healthz", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/", () => "Linker gRPC server running");

app.MapGet("/system/init-status", async () =>
{
    var initialized = await systemInitializer.IsInitializedAsync();
    var state = await systemInitializer.GetCurrentStateAsync();

    return Results.Ok(new
    {
        success = true,
        data = new
        {
            initialized,
            mode = state?.Mode ?? "primary",
            role = state?.Role ?? "primary",
            clusterAddress = state?.ClusterAddress ?? "10.0.0.10",
            clusterPort = state?.ClusterPort ?? 8001,
            leaderFrontendUrl = state?.PublicEndpoint ?? "http://127.0.0.1:5173",
            clusterHealth = "healthy",
            clusterNodes = new[]
            {
                new { id = state?.NodeId ?? "node-primary", role = state?.Role ?? "leader", address = state?.ClusterAddress ?? "10.0.0.10", port = state?.ClusterPort ?? 8001, status = "healthy" },
                new { id = "node-follower-1", role = "follower", address = "10.0.0.11", port = 8002, status = "healthy" }
            },
            defaultUser = new { username = "admin", password = "admin123" },
            database = new
            {
                type = state?.DatabaseType ?? "Sqlite",
                host = state?.DatabaseHost ?? "127.0.0.1",
                port = state?.DatabasePort ?? 3306,
                databaseName = state?.DatabaseName ?? "linker",
                username = state?.DatabaseUsername ?? "root",
                password = state?.DatabasePassword ?? "root"
            },
            message = initialized ? "system initialized" : "system not initialized"
        }
    });
});

app.MapPost("/system/init", async (SystemInitRequest request) =>
{
    var state = await systemInitializer.InitializeAsync(
        request.Mode,
        request.Role ?? request.Mode,
        request.ClusterAddress ?? "10.0.0.10",
        request.ClusterPort ?? 8001,
        request.DefaultUserName ?? "admin",
        request.DefaultPassword ?? "admin123",
        request.NodeId,
        request.NodeName,
        request.LeaderAddress,
        request.DataDirectory,
        request.PublicEndpoint,
        request.ClusterName,
        request.Bootstrap ?? true,
        request.DatabaseType,
        request.DatabaseHost,
        request.DatabasePort,
        request.DatabaseName,
        request.DatabaseUsername,
        request.DatabasePassword);

    return Results.Ok(new
    {
        success = true,
        data = new
        {
            initialized = true,
            mode = state.Mode,
            role = state.Role,
            clusterAddress = state.ClusterAddress,
            clusterPort = state.ClusterPort,
            leaderFrontendUrl = state.PublicEndpoint,
            clusterHealth = "healthy",
            clusterNodes = new[]
            {
                new { id = state.NodeId, role = state.Role, address = state.ClusterAddress, port = state.ClusterPort, status = "healthy" },
                new { id = "node-follower-1", role = "follower", address = "10.0.0.11", port = 8002, status = "healthy" }
            },
            defaultUser = new { username = request.DefaultUserName ?? "admin", password = request.DefaultPassword ?? "admin123" },
            database = new
            {
                type = state.DatabaseType,
                host = state.DatabaseHost,
                port = state.DatabasePort,
                databaseName = state.DatabaseName,
                username = state.DatabaseUsername,
                password = state.DatabasePassword
            },
            message = "system initialized"
        }
    });
});

app.MapGet("/system/cluster-status", async () =>
{
    var state = await systemInitializer.GetCurrentStateAsync();
    return Results.Ok(new
    {
        success = true,
        data = new
        {
            mode = state?.Mode ?? "primary",
            role = state?.Role ?? "primary",
            clusterHealth = "healthy",
            clusterNodes = new[]
            {
                new { id = state?.NodeId ?? "node-primary", role = state?.Role ?? "leader", address = state?.ClusterAddress ?? "10.0.0.10", port = state?.ClusterPort ?? 8001, status = "healthy" },
                new { id = "node-follower-1", role = "follower", address = "10.0.0.11", port = 8002, status = "healthy" }
            },
            leaderFrontendUrl = state?.PublicEndpoint ?? "http://127.0.0.1:5173"
        }
    });
});

app.Run();

public sealed class SystemInitRequest
{
    public string Mode { get; set; } = "primary";
    public string? Role { get; set; }
    public string? ClusterAddress { get; set; }
    public int? ClusterPort { get; set; }
    public string? NodeId { get; set; }
    public string? NodeName { get; set; }
    public string? LeaderAddress { get; set; }
    public string? DataDirectory { get; set; }
    public string? PublicEndpoint { get; set; }
    public string? ClusterName { get; set; }
    public string? DefaultUserName { get; set; }
    public string? DefaultPassword { get; set; }
    public bool? Bootstrap { get; set; }
    public string? DatabaseType { get; set; }
    public string? DatabaseHost { get; set; }
    public int? DatabasePort { get; set; }
    public string? DatabaseName { get; set; }
    public string? DatabaseUsername { get; set; }
    public string? DatabasePassword { get; set; }
}
