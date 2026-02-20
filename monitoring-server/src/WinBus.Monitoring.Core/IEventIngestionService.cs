using WinBus.Monitoring.Contracts;

namespace WinBus.Monitoring.Core;

public interface IEventIngestionService
{
    Task<IngestionResultDto> IngestAsync(StatusEventDto statusEvent, CancellationToken cancellationToken = default);
}
