using System.Text;

namespace WinBus.Utility;

internal static class ConsoleUi
{
    public static void WriteHeader(string title)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(new string('=', 78));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', 78));
        Console.ResetColor();
    }

    public static void WriteModuleOverview(AppModule module)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n{Localization.T("module_label")}: {module.Name}");
        Console.ResetColor();

        Console.WriteLine($"{Localization.T("what_label")} : {module.Description}");
        Console.WriteLine($"{Localization.T("benefit_label")} : {module.Benefits}");
        Console.WriteLine($"{Localization.T("needs_label")}       : {module.Requirements}");
    }

    public static bool AskForConsent(string prompt)
    {
        Console.Write($"{prompt} (Y/N): ");
        var input = (Console.ReadLine() ?? string.Empty).Trim();
        return input.Equals("Y", StringComparison.OrdinalIgnoreCase) ||
               input.Equals("YES", StringComparison.OrdinalIgnoreCase);
    }

    public static void WriteResult(ModuleExecutionResult result)
    {
        Console.ForegroundColor = result.Success ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine(result.Success ? Localization.T("result_success") : Localization.T("result_failed"));
        Console.ResetColor();

        Console.WriteLine(result.Summary);
        if (result.Details.Count > 0)
        {
            foreach (var detail in result.Details.Where(detail => !string.IsNullOrWhiteSpace(detail)))
            {
                Console.WriteLine($" - {detail}");
            }
        }
    }

    public static void WriteSummary(IEnumerable<(AppModule Module, ModuleExecutionResult? Result, bool Skipped)> items)
    {
        Console.WriteLine($"\n{Localization.T("execution_summary")}");
        foreach (var item in items)
        {
            var status = item.Skipped
                ? Localization.T("status_skipped")
                : item.Result is { Success: true }
                    ? Localization.T("status_success")
                    : Localization.T("status_failed");

            Console.WriteLine($" - {item.Module.Name}: {status}");
        }

        Console.WriteLine($"\n{Localization.T("done_message")}");
    }

    public static string FormatStartupItems(IEnumerable<StartupItem> items)
    {
        var sb = new StringBuilder();
        foreach (var item in items)
        {
            sb.AppendLine($"[{item.Hive}] {item.Name}");
            sb.AppendLine($"  {Localization.T("report_command")}: {item.Command}");
            sb.AppendLine($"  {Localization.T("report_electron_likely")}: {(item.IsLikelyElectron ? Localization.T("yes") : Localization.T("no"))}");
        }

        return sb.ToString();
    }
}
