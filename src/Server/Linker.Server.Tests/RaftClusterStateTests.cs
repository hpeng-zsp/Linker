using Linker.Server.Memory;
using Linker.Server.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Linker.Server.Tests;

public class RaftClusterStateTests
{
    [Fact]
    public void SingleNodeCluster_ShouldBecomeLeaderAndAcceptLog()
    {
        var state = new RaftClusterState();
        state.Initialize("127.0.0.1", 5001, []);

        state.StartElection();

        Assert.Equal(RaftRole.Leader, state.Role);
        Assert.Equal(1, state.CurrentTerm);

        state.AppendLog("set-config", "{\"mode\":\"primary\"}");

        Assert.Equal(1, state.LastLogIndex);
        Assert.Equal("set-config", state.GetLog().Single().Command);
    }

    [Fact]
    public async Task WarmUpMemoryFromDatabase_ShouldRestoreServicesAndConfigs()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "linker-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:Type"] = "Sqlite",
                ["ConnectionStrings:Default"] = $"Data Source={Path.Combine(tempDirectory, "linker.db")};Cache=Shared"
            })
            .Build();

        var initializer = new SystemInitializationService(configuration);
        await initializer.EnsureDatabaseReadyAsync();

        await initializer.UpsertServiceRegistrationAsync("gateway-service", "gateway-1", "127.0.0.1", 9001);
        await initializer.UpsertConfigRecordAsync("gateway-service", "feature.enabled", "{\"enabled\":true}", 1);

        var memoryRegistry = new MemoryRegistry();
        var controller = new MemoryServiceController(memoryRegistry, initializer);

        await initializer.WarmUpMemoryAsync(controller);

        Assert.NotEmpty(controller.GetServices());
        Assert.NotNull(controller.GetConfig("gateway-service", "feature.enabled", 1));
        Assert.Equal("{\"enabled\":true}", controller.GetConfig("gateway-service", "feature.enabled", 1)!.Payload);
    }
}
