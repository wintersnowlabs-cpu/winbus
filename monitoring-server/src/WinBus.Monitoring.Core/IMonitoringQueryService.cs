using WinBus.Monitoring.Contracts;

namespace WinBus.Monitoring.Core;

public interface IMonitoringQueryService
{
    Task<MonitoringSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NodeStatusDto>> GetNodesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StatusEventDto>> GetNodeEventsAsync(string nodeName, int take, CancellationToken cancellationToken = default);
}
