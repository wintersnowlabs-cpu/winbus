namespace WinBus.Monitoring.Contracts;

public sealed record StatusEventDto(
    DateTimeOffset Timestamp,
    string Machine,
    string User,
    string Fleet,
    string NodeName,
    string EventType,
    string Module,
    string Status,
    string Message);
