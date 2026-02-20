using WinBus.Monitoring.Contracts;

namespace WinBus.Monitoring.Core;

public interface IStatusEventStore
{
    Task AddAsync(StatusEventDto statusEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NodeStatusDto>> GetNodeStatusesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StatusEventDto>> GetNodeEventsAsync(string nodeName, int take, CancellationToken cancellationToken = default);
    Task<MonitoringSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
