namespace Linker.Server.Services;

public sealed class ReconnectPolicy
{
    private readonly TimeSpan _baseDelay = TimeSpan.FromSeconds(1);
    private readonly Dictionary<string, DateTimeOffset> _lastReconnect = new();

    public TimeSpan GetDelay(string clientId)
    {
        lock (_lastReconnect)
        {
            var last = _lastReconnect.TryGetValue(clientId, out var value) ? value : DateTimeOffset.MinValue;
            var elapsed = DateTimeOffset.UtcNow - last;
            var multiplier = Math.Min(6, Math.Max(1, elapsed.TotalSeconds / 2));
            return TimeSpan.FromSeconds(Math.Max(_baseDelay.TotalSeconds, _baseDelay.TotalSeconds * multiplier));
        }
    }

    public void MarkReconnectAttempt(string clientId)
    {
        lock (_lastReconnect)
        {
            _lastReconnect[clientId] = DateTimeOffset.UtcNow;
        }
    }
}
