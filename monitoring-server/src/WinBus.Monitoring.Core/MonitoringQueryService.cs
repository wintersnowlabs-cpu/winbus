using WinBus.Monitoring.Contracts;

namespace WinBus.Monitoring.Core;

public sealed class MonitoringQueryService(IStatusEventStore store) : IMonitoringQueryService
{
    public Task<MonitoringSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default) =>
        store.GetSummaryAsync(cancellationToken);

    public Task<IReadOnlyList<NodeStatusDto>> GetNodesAsync(CancellationToken cancellationToken = default) =>
        store.GetNodeStatusesAsync(cancellationToken);

    public Task<IReadOnlyList<StatusEventDto>> GetNodeEventsAsync(string nodeName, int take, CancellationToken cancellationToken = default) =>
        store.GetNodeEventsAsync(nodeName, take, cancellationToken);
}
