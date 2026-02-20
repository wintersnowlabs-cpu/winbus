namespace WinBus.Utility;

internal static class AppPaths
{
    public static string BaseDirectory => AppContext.BaseDirectory;
    public static string LogsDirectory => Path.Combine(BaseDirectory, "logs");
    public static string StartupAuditReportPath => Path.Combine(LogsDirectory, "startup-audit-report.txt");
    public static string WatchdogAlertLogPath => Path.Combine(LogsDirectory, "watchdog-alerts.log");
    public static string RemoteMonitoringConfigPath => Path.Combine(BaseDirectory, "remote-monitoring.json");
    public static string RemoteMonitoringEventLogPath => Path.Combine(LogsDirectory, "remote-monitor-events.log");

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(LogsDirectory);
    }

    public static void RotateIfTooLarge(string filePath, long maxBytes = 5 * 1024 * 1024, int maxArchives = 5)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            var info = new FileInfo(filePath);
            if (info.Length < maxBytes)
            {
                return;
            }

            var directory = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return;
            }

            var name = Path.GetFileNameWithoutExtension(filePath);
            var extension = Path.GetExtension(filePath);
            var archivePath = Path.Combine(directory, $"{name}.{DateTime.Now:yyyyMMdd-HHmmss}{extension}");
            File.Move(filePath, archivePath, true);

            var archives = Directory.GetFiles(directory, $"{name}.*{extension}")
                .OrderByDescending(File.GetCreationTimeUtc)
                .Skip(maxArchives)
                .ToList();

            foreach (var archive in archives)
            {
                try
                {
                    File.Delete(archive);
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }
}
