using Microsoft.Win32;

namespace WinBus.Utility;

internal sealed record StartupItem(string Hive, string Name, string Command, bool IsLikelyElectron);

internal static class StartupAuditModule
{
    public static ModuleExecutionResult Execute()
    {
        var items = new List<StartupItem>();

        ReadRunKey(Registry.CurrentUser, "HKCU", items);
        ReadRunKey(Registry.LocalMachine, "HKLM", items);

        if (items.Count == 0)
        {
            return ModuleExecutionResult.Ok(
                Localization.T("startup_none_summary"),
                Localization.T("startup_checked_detail"));
        }

        var electronCount = items.Count(item => item.IsLikelyElectron);
        AppPaths.EnsureDirectories();
        var reportPath = AppPaths.StartupAuditReportPath;
        File.WriteAllText(reportPath, ConsoleUi.FormatStartupItems(items));

        return ModuleExecutionResult.Ok(
            string.Format(Localization.T("startup_found_summary"), items.Count, electronCount),
            string.Format(Localization.T("startup_report_saved"), reportPath),
            Localization.T("startup_review_tip"));
    }

    private static void ReadRunKey(RegistryKey baseKey, string hiveName, ICollection<StartupItem> items)
    {
        using var runKey = baseKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
        if (runKey is null)
        {
            return;
        }

        foreach (var valueName in runKey.GetValueNames())
        {
            var raw = runKey.GetValue(valueName)?.ToString() ?? string.Empty;
            items.Add(new StartupItem(hiveName, valueName, raw, IsLikelyElectron(raw, valueName)));
        }
    }

    private static bool IsLikelyElectron(string command, string name)
    {
        var text = $"{name} {command}".ToLowerInvariant();
        return text.Contains("discord") ||
               text.Contains("teams") ||
               text.Contains("slack") ||
               text.Contains("electron") ||
               text.Contains("update.exe --processstart");
    }
}
