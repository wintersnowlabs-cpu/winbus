using System.Collections.Concurrent;
using WinBus.Monitoring.Contracts;
using WinBus.Monitoring.Core;

namespace WinBus.Monitoring.Infrastructure;

public sealed class InMemoryStatusEventStore(MonitoringStoreOptions options) : IStatusEventStore
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<StatusEventDto>> _nodeEvents = new(StringComparer.OrdinalIgnoreCase);

    public Task AddAsync(StatusEventDto statusEvent, CancellationToken cancellationToken = default)
    {
        var queue = _nodeEvents.GetOrAdd(statusEvent.NodeName, _ => new ConcurrentQueue<StatusEventDto>());
        queue.Enqueue(statusEvent);

        while (queue.Count > options.MaxEventsPerNode && queue.TryDequeue(out _))
        {
        }

        PruneExpired();
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<NodeStatusDto>> GetNodeStatusesAsync(CancellationToken cancellationToken = default)
    {
        PruneExpired();

        var statuses = new List<NodeStatusDto>();
        foreach (var (nodeName, queue) in _nodeEvents)
        {
            var last = queue.LastOrDefault();
            if (last is null)
            {
                continue;
            }

            statuses.Add(new NodeStatusDto(
                nodeName,
                last.Machine,
                last.Fleet,
                last.Status,
                last.EventType,
                last.Module,
                last.Timestamp,
                last.Message));
        }

        return Task.FromResult<IReadOnlyList<NodeStatusDto>>(statuses.OrderByDescending(s => s.LastSeen).ToList());
    }

    public Task<IReadOnlyList<StatusEventDto>> GetNodeEventsAsync(string nodeName, int take, CancellationToken cancellationToken = default)
    {
        PruneExpired();

        if (!_nodeEvents.TryGetValue(nodeName, out var queue))
        {
            return Task.FromResult<IReadOnlyList<StatusEventDto>>([]);
        }

        var events = queue
            .Reverse()
            .Take(Math.Clamp(take, 1, 1000))
            .ToList();

        return Task.FromResult<IReadOnlyList<StatusEventDto>>(events);
    }

    public async Task<MonitoringSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var nodes = await GetNodeStatusesAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var activeCutoff = now.AddMinutes(-Math.Abs(options.ActiveWindowMinutes));
        var hourCutoff = now.AddHours(-1);

        var active = nodes.Count(node => node.LastSeen >= activeCutoff);

        var allRecentEvents = _nodeEvents.Values
            .SelectMany(queue => queue)
            .Where(e => e.Timestamp >= hourCutoff)
            .ToList();

        var alertsLastHour = allRecentEvents.Count(e => e.Status.Equals("alert", StringComparison.OrdinalIgnoreCase));
        var failuresLastHour = allRecentEvents.Count(e => e.Status.Equals("failed", StringComparison.OrdinalIgnoreCase));

        return new MonitoringSummaryDto(
            nodes.Count,
            active,
            alertsLastHour,
            failuresLastHour,
            DateTimeOffset.UtcNow);
    }

    private void PruneExpired()
    {
        var cutoff = DateTimeOffset.UtcNow.AddHours(-Math.Abs(options.RetentionHours));

        foreach (var (nodeName, queue) in _nodeEvents)
        {
            while (queue.TryPeek(out var head) && head.Timestamp < cutoff)
            {
                queue.TryDequeue(out _);
            }

            if (queue.IsEmpty)
            {
                _nodeEvents.TryRemove(nodeName, out _);
            }
        }
    }
}
