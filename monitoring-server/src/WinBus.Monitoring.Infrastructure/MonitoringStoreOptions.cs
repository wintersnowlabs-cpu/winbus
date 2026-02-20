namespace WinBus.Monitoring.Infrastructure;

public sealed class MonitoringStoreOptions
{
    public int RetentionHours { get; set; } = 24;
    public int ActiveWindowMinutes { get; set; } = 10;
    public int MaxEventsPerNode { get; set; } = 5000;
}
