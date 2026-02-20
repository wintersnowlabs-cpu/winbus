using WinBus.Monitoring.Contracts;

namespace WinBus.Monitoring.Core;

public sealed class EventIngestionService(IStatusEventStore store) : IEventIngestionService
{
    public async Task<IngestionResultDto> IngestAsync(StatusEventDto statusEvent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(statusEvent.NodeName))
        {
            return new IngestionResultDto(false, "NodeName is required.");
        }

        if (string.IsNullOrWhiteSpace(statusEvent.EventType))
        {
            return new IngestionResultDto(false, "EventType is required.");
        }

        await store.AddAsync(statusEvent, cancellationToken);
        return new IngestionResultDto(true, "Event accepted.");
    }
}
