using Microsoft.Extensions.DependencyInjection;
using WinBus.Monitoring.Core;

namespace WinBus.Monitoring.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMonitoringInfrastructure(this IServiceCollection services, Action<MonitoringStoreOptions>? configure = null)
    {
        var options = new MonitoringStoreOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IStatusEventStore, InMemoryStatusEventStore>();
        services.AddSingleton<IEventIngestionService, EventIngestionService>();
        services.AddSingleton<IMonitoringQueryService, MonitoringQueryService>();

        return services;
    }
}
