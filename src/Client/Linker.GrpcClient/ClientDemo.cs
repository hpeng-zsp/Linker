using Linker.GrpcClient;

var client = new LinkerGrpcClient(new LinkerClientOptions
{
    ClientId = "demo-client-001",
    ServiceName = "user-service",
    ConfigKey = "appsettings",
    Version = 0,
    DefaultAddress = "127.0.0.1",
    DefaultPort = 5001,
    SeedNodes = ["127.0.0.1:5001", "127.0.0.1:5002"],
    ReconnectDelay = TimeSpan.FromSeconds(2),
    MaxReconnectAttempts = 10
});

client.OnMessageReceived += async response =>
{
    Console.WriteLine("Received: " + response.ServiceName + " | " + response.ConfigPayload);
    await Task.CompletedTask;
};

client.OnReconnect += async reason =>
{
    Console.WriteLine("Reconnect: " + reason);
    await Task.CompletedTask;
};

await client.StartAsync();

await client.SendServiceHeartbeatAsync("user-service", "appsettings", 0);

Console.WriteLine("Client is running. Press any key to stop.");
Console.ReadKey();

await client.StopAsync();
