namespace WinBus.Monitoring.QueryApi;

public sealed class MonitoringQueryApiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string StoreProvider { get; set; } = "InMemory";
    public int RetentionHours { get; set; } = 24;
    public int ActiveWindowMinutes { get; set; } = 10;
    public int MaxEventsPerNode { get; set; } = 5000;
    public SqlServerOptions SqlServer { get; set; } = new();
}

public sealed class SqlServerOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string TableName { get; set; } = "StatusEvents";
    public bool AutoCreateSchema { get; set; } = true;
}
