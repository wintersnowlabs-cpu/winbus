namespace WinBus.Monitoring.Contracts;

public sealed record MonitoringSummaryDto(
    int TotalNodes,
    int ActiveNodes,
    int AlertsLastHour,
    int FailuresLastHour,
    DateTimeOffset GeneratedAt);
