using WinBus.Monitoring.Contracts;
using WinBus.Monitoring.Core;
using WinBus.Monitoring.Infrastructure;
using WinBus.Monitoring.Infrastructure.SqlServer;
using WinBus.Monitoring.QueryApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var queryOptions = builder.Configuration.GetSection("MonitoringApi").Get<MonitoringQueryApiOptions>() ?? new MonitoringQueryApiOptions();
builder.Services.AddSingleton(queryOptions);

var provider = (queryOptions.StoreProvider ?? "InMemory").Trim();
if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddMonitoringSqlServerInfrastructure(options =>
    {
        options.ConnectionString = queryOptions.SqlServer.ConnectionString;
        options.TableName = queryOptions.SqlServer.TableName;
        options.AutoCreateSchema = queryOptions.SqlServer.AutoCreateSchema;
        options.RetentionHours = queryOptions.RetentionHours;
        options.ActiveWindowMinutes = queryOptions.ActiveWindowMinutes;
    });
}
else
{
    builder.Services.AddMonitoringInfrastructure(options =>
    {
        options.RetentionHours = queryOptions.RetentionHours;
        options.ActiveWindowMinutes = queryOptions.ActiveWindowMinutes;
        options.MaxEventsPerNode = queryOptions.MaxEventsPerNode;
    });
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/query/health", () => Results.Ok(new
{
    service = "WinBus Monitoring Query API",
    status = "ok",
    provider,
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/api/query/summary", async (
    HttpContext context,
    IMonitoringQueryService queryService,
    MonitoringQueryApiOptions options,
    CancellationToken cancellationToken) =>
{
    if (!IsAuthorized(context, options))
    {
        return Results.Unauthorized();
    }

    var summary = await queryService.GetSummaryAsync(cancellationToken);
    return Results.Ok(summary);
});

app.MapGet("/api/query/nodes", async (
    HttpContext context,
    IMonitoringQueryService queryService,
    MonitoringQueryApiOptions options,
    CancellationToken cancellationToken) =>
{
    if (!IsAuthorized(context, options))
    {
        return Results.Unauthorized();
    }

    var nodes = await queryService.GetNodesAsync(cancellationToken);
    return Results.Ok(nodes);
});

app.MapGet("/api/query/nodes/{nodeName}/events", async (
    HttpContext context,
    string nodeName,
    int? take,
    IMonitoringQueryService queryService,
    MonitoringQueryApiOptions options,
    CancellationToken cancellationToken) =>
{
    if (!IsAuthorized(context, options))
    {
        return Results.Unauthorized();
    }

    var events = await queryService.GetNodeEventsAsync(nodeName, take ?? 100, cancellationToken);
    return Results.Ok(events);
});

app.Run();

static bool IsAuthorized(HttpContext context, MonitoringQueryApiOptions options)
{
    if (string.IsNullOrWhiteSpace(options.ApiKey))
    {
        return true;
    }

    if (!context.Request.Headers.TryGetValue("X-Api-Key", out var providedKey))
    {
        return false;
    }

    return string.Equals(providedKey.ToString(), options.ApiKey, StringComparison.Ordinal);
}
