namespace Linker.Client;

public sealed class LinkerClientOptions
{
    public string ClientId { get; set; } = "client-default";
    public string ServiceName { get; set; } = "user-service";
    public string ConfigKey { get; set; } = "appsettings";
    public long Version { get; set; }
    public string DefaultAddress { get; set; } = "127.0.0.1";
    public int DefaultPort { get; set; } = 5001;
    public List<string> SeedNodes { get; set; } = new();
    public int MaxReconnectAttempts { get; set; } = 10;
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(2);
}
