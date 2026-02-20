using System.Security.Principal;

namespace WinBus.Utility;

internal static class Program
{
    [STAThread]
    private static async Task Main()
    {
        AppPaths.EnsureDirectories();
        SelectLanguage();

        var (monitorSettings, createdTemplate) = RemoteMonitoringSettingsStore.LoadOrCreate(AppPaths.RemoteMonitoringConfigPath);
        using var remoteMonitor = new RemoteMonitorClient(monitorSettings);
        RemoteStatusPublisher.Configure(remoteMonitor);
        remoteMonitor.Start();

        ConsoleUi.WriteHeader(Localization.T("header_title"));
        Console.WriteLine(Localization.T("intro_line1"));
        Console.WriteLine($"{Localization.T("intro_line2")}\n");
        Console.WriteLine(Localization.T("log_locations_header"));
        Console.WriteLine(string.Format(Localization.T("log_startup_report"), AppPaths.StartupAuditReportPath));
        Console.WriteLine(string.Format(Localization.T("log_watchdog_alerts"), AppPaths.WatchdogAlertLogPath));
        Console.WriteLine(string.Format(Localization.T("log_remote_config"), AppPaths.RemoteMonitoringConfigPath));
        Console.WriteLine(string.Format(Localization.T("log_remote_events"), AppPaths.RemoteMonitoringEventLogPath));

        if (createdTemplate)
        {
            Console.WriteLine(Localization.T("remote_template_created"));
        }

        Console.WriteLine(remoteMonitor.IsEnabled
            ? string.Format(Localization.T("remote_monitoring_enabled"), monitorSettings.EndpointUrl, monitorSettings.HeartbeatSeconds)
            : Localization.T("remote_monitoring_disabled"));
        Console.WriteLine();

        RemoteStatusPublisher.Publish("session_start", "system", "started", "Utility session started.");

        if (!IsAdministrator())
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(Localization.T("warning_not_admin"));
            Console.ResetColor();
        }

        var cancellationToken = CancellationToken.None;

        var modules = new List<AppModule>
        {
            new(
                Localization.T("m1_name"),
                Localization.T("m1_desc"),
                Localization.T("m1_benefits"),
                Localization.T("m1_req"),
                ct => SystemTuningModules.CreateSystemRestorePointAsync(ct)),

            new(
                Localization.T("m2_name"),
                Localization.T("m2_desc"),
                Localization.T("m2_benefits"),
                Localization.T("m2_req"),
                ct => SystemTuningModules.DisableTelemetryAsync(ct)),

            new(
                Localization.T("m3_name"),
                Localization.T("m3_desc"),
                Localization.T("m3_benefits"),
                Localization.T("m3_req"),
                ct => SystemTuningModules.PurgeCachesAsync(ct)),

            new(
                Localization.T("m4_name"),
                Localization.T("m4_desc"),
                Localization.T("m4_benefits"),
                Localization.T("m4_req"),
                ct => Task.FromResult(StartupAuditModule.Execute())),

            new(
                Localization.T("m5_name"),
                Localization.T("m5_desc"),
                Localization.T("m5_benefits"),
                Localization.T("m5_req"),
                ct => Task.FromResult(MemoryWatchdog.ExecuteWithConsent()))
        };

        var summary = new List<(AppModule Module, ModuleExecutionResult? Result, bool Skipped)>();

        foreach (var module in modules)
        {
            ConsoleUi.WriteModuleOverview(module);
            RemoteStatusPublisher.Publish("module_prompt", module.Name, "pending", "Module awaiting user consent.");
            var run = ConsoleUi.AskForConsent(Localization.T("consent_prompt"));
            if (!run)
            {
                Console.WriteLine($"{Localization.T("skipped_by_user")}\n");
                summary.Add((module, null, true));
                RemoteStatusPublisher.Publish("module_result", module.Name, "skipped", "Skipped by user consent decision.");
                continue;
            }

            Console.WriteLine($"{Localization.T("executing_module")}\n");
            RemoteStatusPublisher.Publish("module_start", module.Name, "running", "Module execution started.");
            var result = await module.ExecuteAsync(cancellationToken);
            ConsoleUi.WriteResult(result);
            Console.WriteLine();
            summary.Add((module, result, false));
            RemoteStatusPublisher.Publish(
                "module_result",
                module.Name,
                result.Success ? "success" : "failed",
                result.Summary);
        }

        ConsoleUi.WriteSummary(summary);
        RemoteStatusPublisher.Publish("session_end", "system", "completed", "Utility session completed.");
    }

    private static bool IsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static void SelectLanguage()
    {
        Console.WriteLine(Localization.T("language_select"));
        for (var i = 0; i < Localization.SupportedLanguages.Count; i++)
        {
            var language = Localization.SupportedLanguages[i];
            Console.WriteLine($" {i + 1}. {language.Name}");
        }

        Console.Write($"{Localization.T("language_prompt")}: ");
        var input = Console.ReadLine();

        if (!int.TryParse(input, out var index) || index < 1 || index > Localization.SupportedLanguages.Count)
        {
            Console.WriteLine($"{Localization.T("invalid_choice_default")}\n");
            Localization.SetLanguage("en");
            return;
        }

        Localization.SetLanguage(Localization.SupportedLanguages[index - 1].Code);
        Console.WriteLine();
    }
}
