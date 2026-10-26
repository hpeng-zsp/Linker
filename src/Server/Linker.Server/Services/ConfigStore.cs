using System.Text.Json;

namespace Linker.Server.Services;

public sealed class ConfigEntry
{
    public string ServiceName { get; set; } = string.Empty;
    public string ConfigKey { get; set; } = string.Empty;
    public long Version { get; set; }
    public string Payload { get; set; } = "{}";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ConfigStore
{
    private readonly object _sync = new();
    private readonly Dictionary<string, List<ConfigEntry>> _store = new();

    public void Upsert(string serviceName, string configKey, string payload, long version = 0)
    {
        lock (_sync)
        {
            var key = BuildKey(serviceName, configKey);
            if (!_store.TryGetValue(key, out var items))
            {
                items = new List<ConfigEntry>();
                _store[key] = items;
            }

            var safeVersion = version > 0 ? version : items.Count == 0 ? 1 : items.Max(x => x.Version) + 1;
            items.Add(new ConfigEntry
            {
                ServiceName = serviceName,
                ConfigKey = configKey,
                Version = safeVersion,
                Payload = payload,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
    }

    public string GetLatest(string serviceName, string configKey, long version)
    {
        lock (_sync)
        {
            var key = BuildKey(serviceName, configKey);
            if (!_store.TryGetValue(key, out var items) || items.Count == 0)
            {
                return "{}";
            }

            var entry = version > 0
                ? items.OrderByDescending(x => x.Version).FirstOrDefault(x => x.Version <= version)
                : items.OrderByDescending(x => x.Version).First();

            return entry?.Payload ?? "{}";
        }
    }

    public string MergeWithBase(string serviceName, string configKey, long version, string basePayload)
    {
        var latest = GetLatest(serviceName, configKey, version);
        if (string.IsNullOrWhiteSpace(latest) || latest == "{}")
        {
            return basePayload;
        }

        try
        {
            using var baseDoc = JsonDocument.Parse(basePayload);
            using var serviceDoc = JsonDocument.Parse(latest);

            var merged = JsonDocument.Parse("{}").RootElement.Clone();
            var result = new Dictionary<string, JsonElement>();

            foreach (var pair in baseDoc.RootElement.EnumerateObject())
            {
                result[pair.Name] = pair.Value.Clone();
            }

            foreach (var pair in serviceDoc.RootElement.EnumerateObject())
            {
                result[pair.Name] = pair.Value.Clone();
            }

            var json = JsonSerializer.Serialize(result);
            return json;
        }
        catch
        {
            return latest;
        }
    }

    private static string BuildKey(string serviceName, string configKey)
    {
        return $"{serviceName}:{configKey}";
    }
}
