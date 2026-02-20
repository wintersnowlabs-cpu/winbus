using System.Net.Http.Json;
using System.Text.Json;

namespace WinBus.Utility;

internal sealed record RemoteMonitoringSettings(
    bool Enabled,
    string EndpointUrl,
    string ApiKey,
    int HeartbeatSeconds,
    string Fleet,
    string NodeName);

internal sealed record RemoteStatusEvent(
    DateTimeOffset Timestamp,
    string Machine,
    string User,
    string Fleet,
    string NodeName,
    string EventType,
    string Module,
    string Status,
    string Message);

internal static class RemoteMonitoringSettingsStore
{
    public static (RemoteMonitoringSettings Settings, bool CreatedTemplate) LoadOrCreate(string path)
    {
        var defaults = new RemoteMonitoringSettings(
            false,
            "https://your-monitoring-endpoint/api/status/events",
            string.Empty,
            60,
            "default-fleet",
            Environment.MachineName);

        try
        {
            if (!File.Exists(path))
            {
                var json = JsonSerializer.Serialize(defaults, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
                return (defaults, true);
            }

            var text = File.ReadAllText(path);
            var parsed = JsonSerializer.Deserialize<RemoteMonitoringSettings>(text);
            if (parsed is null)
            {
                return (defaults, false);
            }

            var heartbeat = parsed.HeartbeatSeconds <= 0 ? 60 : parsed.HeartbeatSeconds;
            return (parsed with { HeartbeatSeconds = heartbeat }, false);
        }
        catch
        {
            return (defaults, false);
        }
    }
}

internal sealed class RemoteMonitorClient : IDisposable
{
    private readonly RemoteMonitoringSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly object _logLock = new();
    private System.Threading.Timer? _heartbeatTimer;

    public bool IsEnabled { get; }
    public string StatusMessage { get; }

    public RemoteMonitorClient(RemoteMonitoringSettings settings)
    {
        _settings = settings;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        if (!_settings.Enabled)
        {
            IsEnabled = false;
            StatusMessage = "Remote monitoring is disabled in remote-monitoring.json.";
            return;
        }

        if (!Uri.TryCreate(_settings.EndpointUrl, UriKind.Absolute, out var endpoint))
        {
            IsEnabled = false;
            StatusMessage = "Remote monitoring endpoint is invalid.";
            return;
        }

        IsEnabled = true;
        StatusMessage = $"Remote monitoring enabled. Endpoint: {endpoint}";
    }

    public void Start()
    {
        if (!IsEnabled)
        {
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Clamp(_settings.HeartbeatSeconds, 15, 3600));
        _heartbeatTimer = new System.Threading.Timer(_ => Publish("heartbeat", "system", "ok", "Periodic heartbeat"), null, interval, interval);
    }

    public void Publish(string eventType, string module, string status, string message)
    {
        if (!IsEnabled)
        {
            return;
        }

        var payload = new RemoteStatusEvent(
            DateTimeOffset.UtcNow,
            Environment.MachineName,
            Environment.UserName,
            _settings.Fleet,
            string.IsNullOrWhiteSpace(_settings.NodeName) ? Environment.MachineName : _settings.NodeName,
            eventType,
            module,
            status,
            message);

        _ = Task.Run(async () => await SendAsync(payload));
    }

    private async Task SendAsync(RemoteStatusEvent payload)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _settings.EndpointUrl)
            {
                Content = JsonContent.Create(payload)
            };

            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                request.Headers.Add("X-Api-Key", _settings.ApiKey);
            }

            var response = await _httpClient.SendAsync(request);
            var code = (int)response.StatusCode;
            AppendEventLog($"SENT {payload.EventType} | {payload.Module} | {payload.Status} | HTTP {code}");
        }
        catch (Exception ex)
        {
            AppendEventLog($"FAILED {payload.EventType} | {payload.Module} | {payload.Status} | {ex.Message}");
        }
    }

    private void AppendEventLog(string line)
    {
        try
        {
            lock (_logLock)
            {
                AppPaths.RotateIfTooLarge(AppPaths.RemoteMonitoringEventLogPath);
                File.AppendAllText(AppPaths.RemoteMonitoringEventLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {line}{Environment.NewLine}");
            }
        }
        catch
        {
        }
    }

    public void Dispose()
    {
        _heartbeatTimer?.Dispose();
        _httpClient.Dispose();
    }
}

internal static class RemoteStatusPublisher
{
    private static RemoteMonitorClient? _client;

    public static void Configure(RemoteMonitorClient client)
    {
        _client = client;
    }

    public static void Publish(string eventType, string module, string status, string message)
    {
        _client?.Publish(eventType, module, status, message);
    }
}
