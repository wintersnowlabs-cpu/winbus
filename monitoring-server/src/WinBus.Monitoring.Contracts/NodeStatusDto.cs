namespace WinBus.Monitoring.Contracts;

public sealed record NodeStatusDto(
    string NodeName,
    string Machine,
    string Fleet,
    string LastStatus,
    string LastEventType,
    string LastModule,
    DateTimeOffset LastSeen,
    string LastMessage);
