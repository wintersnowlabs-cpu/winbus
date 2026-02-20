namespace WinBus.Utility;

internal sealed record AppModule(
    string Name,
    string Description,
    string Benefits,
    string Requirements,
    Func<CancellationToken, Task<ModuleExecutionResult>> ExecuteAsync);

internal sealed record ModuleExecutionResult(bool Success, string Summary, IReadOnlyList<string> Details)
{
    public static ModuleExecutionResult Ok(string summary, params string[] details) =>
        new(true, summary, details);

    public static ModuleExecutionResult Fail(string summary, params string[] details) =>
        new(false, summary, details);
}
