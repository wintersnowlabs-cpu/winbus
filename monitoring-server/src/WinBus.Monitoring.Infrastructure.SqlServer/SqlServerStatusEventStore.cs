using Microsoft.Data.SqlClient;
using WinBus.Monitoring.Contracts;
using WinBus.Monitoring.Core;

namespace WinBus.Monitoring.Infrastructure.SqlServer;

public sealed class SqlServerStatusEventStore : IStatusEventStore
{
    private readonly SqlServerStoreOptions _options;
    private readonly string _safeTableName;

    public SqlServerStatusEventStore(SqlServerStoreOptions options)
    {
        _options = options;
        _safeTableName = SanitizeTableName(string.IsNullOrWhiteSpace(options.TableName) ? "StatusEvents" : options.TableName);

        if (_options.AutoCreateSchema)
        {
            EnsureSchema();
        }
    }

    public async Task AddAsync(StatusEventDto statusEvent, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_options.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = $@"
INSERT INTO [dbo].[{_safeTableName}] ([Timestamp], [Machine], [UserName], [Fleet], [NodeName], [EventType], [Module], [Status], [Message])
VALUES (@Timestamp, @Machine, @UserName, @Fleet, @NodeName, @EventType, @Module, @Status, @Message);";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Timestamp", statusEvent.Timestamp);
        cmd.Parameters.AddWithValue("@Machine", statusEvent.Machine);
        cmd.Parameters.AddWithValue("@UserName", statusEvent.User);
        cmd.Parameters.AddWithValue("@Fleet", statusEvent.Fleet);
        cmd.Parameters.AddWithValue("@NodeName", statusEvent.NodeName);
        cmd.Parameters.AddWithValue("@EventType", statusEvent.EventType);
        cmd.Parameters.AddWithValue("@Module", statusEvent.Module);
        cmd.Parameters.AddWithValue("@Status", statusEvent.Status);
        cmd.Parameters.AddWithValue("@Message", statusEvent.Message);
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        await PruneExpiredAsync(conn, cancellationToken);
    }

    public async Task<IReadOnlyList<NodeStatusDto>> GetNodeStatusesAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_options.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = $@"
WITH Ranked AS (
    SELECT
        [NodeName], [Machine], [Fleet], [Status], [EventType], [Module], [Timestamp], [Message],
        ROW_NUMBER() OVER (PARTITION BY [NodeName] ORDER BY [Timestamp] DESC, [Id] DESC) AS rn
    FROM [dbo].[{_safeTableName}]
)
SELECT [NodeName], [Machine], [Fleet], [Status], [EventType], [Module], [Timestamp], [Message]
FROM Ranked
WHERE rn = 1
ORDER BY [Timestamp] DESC;";

        var results = new List<NodeStatusDto>();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new NodeStatusDto(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetDateTimeOffset(6),
                reader.GetString(7)));
        }

        return results;
    }

    public async Task<IReadOnlyList<StatusEventDto>> GetNodeEventsAsync(string nodeName, int take, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_options.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        var sql = $@"
SELECT TOP (@Take) [Timestamp], [Machine], [UserName], [Fleet], [NodeName], [EventType], [Module], [Status], [Message]
FROM [dbo].[{_safeTableName}]
WHERE [NodeName] = @NodeName
ORDER BY [Timestamp] DESC, [Id] DESC;";

        var results = new List<StatusEventDto>();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Take", Math.Clamp(take, 1, 1000));
        cmd.Parameters.AddWithValue("@NodeName", nodeName);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new StatusEventDto(
                reader.GetDateTimeOffset(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetString(6),
                reader.GetString(7),
                reader.GetString(8)));
        }

        return results;
    }

    public async Task<MonitoringSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new SqlConnection(_options.ConnectionString);
        await conn.OpenAsync(cancellationToken);

        var activeCutoff = DateTimeOffset.UtcNow.AddMinutes(-Math.Abs(_options.ActiveWindowMinutes));
        var hourCutoff = DateTimeOffset.UtcNow.AddHours(-1);

        var sql = $@"
WITH Latest AS (
    SELECT [NodeName], [Timestamp],
           ROW_NUMBER() OVER (PARTITION BY [NodeName] ORDER BY [Timestamp] DESC, [Id] DESC) AS rn
    FROM [dbo].[{_safeTableName}]
)
SELECT
    (SELECT COUNT(*) FROM Latest WHERE rn = 1) AS TotalNodes,
    (SELECT COUNT(*) FROM Latest WHERE rn = 1 AND [Timestamp] >= @ActiveCutoff) AS ActiveNodes,
    (SELECT COUNT(*) FROM [dbo].[{_safeTableName}] WHERE [Timestamp] >= @HourCutoff AND [Status] = 'alert') AS AlertsLastHour,
    (SELECT COUNT(*) FROM [dbo].[{_safeTableName}] WHERE [Timestamp] >= @HourCutoff AND [Status] = 'failed') AS FailuresLastHour;";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@ActiveCutoff", activeCutoff);
        cmd.Parameters.AddWithValue("@HourCutoff", hourCutoff);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new MonitoringSummaryDto(0, 0, 0, 0, DateTimeOffset.UtcNow);
        }

        return new MonitoringSummaryDto(
            reader.GetInt32(0),
            reader.GetInt32(1),
            reader.GetInt32(2),
            reader.GetInt32(3),
            DateTimeOffset.UtcNow);
    }

    private void EnsureSchema()
    {
        using var conn = new SqlConnection(_options.ConnectionString);
        conn.Open();

        var sql = $@"
IF OBJECT_ID(N'[dbo].[{_safeTableName}]', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[{_safeTableName}] (
        [Id] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Timestamp] DATETIMEOFFSET(7) NOT NULL,
        [Machine] NVARCHAR(128) NOT NULL,
        [UserName] NVARCHAR(128) NOT NULL,
        [Fleet] NVARCHAR(128) NOT NULL,
        [NodeName] NVARCHAR(128) NOT NULL,
        [EventType] NVARCHAR(128) NOT NULL,
        [Module] NVARCHAR(256) NOT NULL,
        [Status] NVARCHAR(64) NOT NULL,
        [Message] NVARCHAR(2000) NOT NULL
    );

    CREATE INDEX [IX_{_safeTableName}_NodeName_Timestamp] ON [dbo].[{_safeTableName}] ([NodeName], [Timestamp] DESC);
    CREATE INDEX [IX_{_safeTableName}_Timestamp] ON [dbo].[{_safeTableName}] ([Timestamp] DESC);
END";

        using var cmd = new SqlCommand(sql, conn);
        cmd.ExecuteNonQuery();
    }

    private async Task PruneExpiredAsync(SqlConnection conn, CancellationToken cancellationToken)
    {
        var cutoff = DateTimeOffset.UtcNow.AddHours(-Math.Abs(_options.RetentionHours));
        var sql = $@"DELETE FROM [dbo].[{_safeTableName}] WHERE [Timestamp] < @Cutoff;";

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@Cutoff", cutoff);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string SanitizeTableName(string tableName)
    {
        var filtered = new string(tableName.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        return string.IsNullOrWhiteSpace(filtered) ? "StatusEvents" : filtered;
    }
}
