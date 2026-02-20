namespace WinBus.Monitoring.Infrastructure.SqlServer;

public sealed class SqlServerStoreOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string TableName { get; set; } = "StatusEvents";
    public bool AutoCreateSchema { get; set; } = true;
    public int RetentionHours { get; set; } = 24;
    public int ActiveWindowMinutes { get; set; } = 10;
}
