using Microsoft.Extensions.DependencyInjection;
using WinBus.Monitoring.Core;

namespace WinBus.Monitoring.Infrastructure.SqlServer;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMonitoringSqlServerInfrastructure(this IServiceCollection services, Action<SqlServerStoreOptions>? configure = null)
    {
        var options = new SqlServerStoreOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<IStatusEventStore, SqlServerStatusEventStore>();
        services.AddSingleton<IEventIngestionService, EventIngestionService>();
        services.AddSingleton<IMonitoringQueryService, MonitoringQueryService>();

        return services;
    }
}
