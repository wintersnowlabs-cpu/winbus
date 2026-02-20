using WinBus.Monitoring.Api;
using WinBus.Monitoring.Contracts;
using WinBus.Monitoring.Core;
using WinBus.Monitoring.Infrastructure;
using WinBus.Monitoring.Infrastructure.SqlServer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var apiOptions = builder.Configuration.GetSection("MonitoringApi").Get<MonitoringApiOptions>() ?? new MonitoringApiOptions();

builder.Services.AddSingleton(apiOptions);

var provider = (apiOptions.StoreProvider ?? "InMemory").Trim();
if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddMonitoringSqlServerInfrastructure(options =>
    {
        options.ConnectionString = apiOptions.SqlServer.ConnectionString;
        options.TableName = apiOptions.SqlServer.TableName;
        options.AutoCreateSchema = apiOptions.SqlServer.AutoCreateSchema;
        options.RetentionHours = apiOptions.RetentionHours;
        options.ActiveWindowMinutes = apiOptions.ActiveWindowMinutes;
    });
}
else
{
    builder.Services.AddMonitoringInfrastructure(options =>
    {
        options.RetentionHours = apiOptions.RetentionHours;
        options.ActiveWindowMinutes = apiOptions.ActiveWindowMinutes;
        options.MaxEventsPerNode = apiOptions.MaxEventsPerNode;
    });
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/status/health", () => Results.Ok(new
{
    service = "WinBus Monitoring API",
    status = "ok",
    provider,
    utc = DateTimeOffset.UtcNow
}));

app.MapPost("/api/status/events", async (
    HttpContext context,
    StatusEventDto statusEvent,
    IEventIngestionService ingestionService,
    MonitoringApiOptions options,
    CancellationToken cancellationToken) =>
{
    if (!IsAuthorized(context, options))
    {
        return Results.Unauthorized();
    }

    var result = await ingestionService.IngestAsync(statusEvent, cancellationToken);
    return result.Accepted ? Results.Accepted(value: result) : Results.BadRequest(result);
});

app.MapGet("/api/status/summary", async (
    HttpContext context,
    IMonitoringQueryService queryService,
    MonitoringApiOptions options,
    CancellationToken cancellationToken) =>
{
    if (!IsAuthorized(context, options))
    {
        return Results.Unauthorized();
    }

    var summary = await queryService.GetSummaryAsync(cancellationToken);
    return Results.Ok(summary);
});

app.MapGet("/api/status/nodes", async (
    HttpContext context,
    IMonitoringQueryService queryService,
    MonitoringApiOptions options,
    CancellationToken cancellationToken) =>
{
    if (!IsAuthorized(context, options))
    {
        return Results.Unauthorized();
    }

    var nodes = await queryService.GetNodesAsync(cancellationToken);
    return Results.Ok(nodes);
});

app.MapGet("/api/status/nodes/{nodeName}/events", async (
    HttpContext context,
    string nodeName,
    int? take,
    IMonitoringQueryService queryService,
    MonitoringApiOptions options,
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

static bool IsAuthorized(HttpContext context, MonitoringApiOptions options)
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
