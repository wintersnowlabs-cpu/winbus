using System.Management;
using Microsoft.Win32;

namespace WinBus.Utility;

internal static class SystemTuningModules
{
    public static async Task<ModuleExecutionResult> CreateSystemRestorePointAsync(CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var systemRestoreReg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore", false);
                var disableSr = Convert.ToInt32(systemRestoreReg?.GetValue("DisableSR", 0) ?? 0);
                if (disableSr == 1)
                {
                    return ModuleExecutionResult.Fail(
                        "Unable to create restore point.",
                        "System Protection is disabled by policy/registry (DisableSR=1). Enable System Protection and retry.");
                }

                using var mc = new ManagementClass("\\\\localhost\\root\\default", "SystemRestore", null);
                using var inParams = mc.GetMethodParameters("CreateRestorePoint");
                inParams["Description"] = $"WinBus Utility Restore Point {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                inParams["RestorePointType"] = 0;
                inParams["EventType"] = 100;

                using var outParams = mc.InvokeMethod("CreateRestorePoint", inParams, null);
                var result = Convert.ToInt32(outParams?["ReturnValue"] ?? -1);

                return result == 0
                    ? ModuleExecutionResult.Ok(
                        "System restore point created successfully.",
                        "Rollback safety checkpoint is available if a later module needs reversal.")
                    : ModuleExecutionResult.Fail(
                        $"CreateRestorePoint returned code {result}.",
                        "Ensure System Protection is enabled and utility is running elevated as Administrator.");
            }
            catch (Exception ex)
            {
                var message = string.IsNullOrWhiteSpace(ex.Message)
                    ? $"{ex.GetType().Name} (HRESULT: 0x{ex.HResult:X8})"
                    : ex.Message;

                return ModuleExecutionResult.Fail(
                    "Unable to create restore point.",
                    message,
                    "Check: run utility as Administrator, enable System Protection on system drive, and ensure Volume Shadow Copy service is available.");
            }
        }, cancellationToken);
    }

    public static async Task<ModuleExecutionResult> DisableTelemetryAsync(CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var details = new List<string>();

            try
            {
                using var serviceManager = new System.ServiceProcess.ServiceController("DiagTrack");
                try
                {
                    if (serviceManager.Status != System.ServiceProcess.ServiceControllerStatus.Stopped &&
                        serviceManager.Status != System.ServiceProcess.ServiceControllerStatus.StopPending)
                    {
                        serviceManager.Stop();
                        serviceManager.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(20));
                    }

                    details.Add("DiagTrack service stopped.");
                }
                catch (Exception ex)
                {
                    details.Add($"DiagTrack service stop warning: {ex.Message}");
                }

                using var dataCollection = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows\DataCollection", true);
                dataCollection?.SetValue("AllowTelemetry", 0, RegistryValueKind.DWord);
                details.Add("Registry set: HKLM\\SOFTWARE\\Policies\\Microsoft\\Windows\\DataCollection\\AllowTelemetry = 0");

                return ModuleExecutionResult.Ok(
                    "Telemetry reduction module completed.",
                    details.ToArray());
            }
            catch (Exception ex)
            {
                details.Add(ex.Message);
                return ModuleExecutionResult.Fail(
                    "Telemetry module failed.",
                    details.ToArray());
            }
        }, cancellationToken);
    }

    public static async Task<ModuleExecutionResult> PurgeCachesAsync(CancellationToken cancellationToken)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var totalDeletedFiles = 0;
            var totalDeletedBytes = 0L;
            var details = new List<string>();
            var windowsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            var tempTargets = new[]
            {
                Path.GetTempPath(),
                Path.Combine(windowsFolder, "Temp")
            };

            foreach (var target in tempTargets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!Directory.Exists(target))
                {
                    details.Add($"Skipped missing path: {target}");
                    continue;
                }

                var stats = DeleteDirectoryContents(target);
                totalDeletedFiles += stats.FilesDeleted;
                totalDeletedBytes += stats.BytesDeleted;

                details.Add($"Cleaned: {target} | Files removed: {stats.FilesDeleted} | Failures: {stats.Failures} | Remaining files: {stats.RemainingFiles}");
            }

            var softwareDistributionDownload = Path.Combine(windowsFolder, "SoftwareDistribution", "Download");
            var updateServiceDetails = CleanSoftwareDistributionDownload(softwareDistributionDownload);
            details.AddRange(updateServiceDetails.Details);
            totalDeletedFiles += updateServiceDetails.FilesDeleted;
            totalDeletedBytes += updateServiceDetails.BytesDeleted;

            var reclaimedMb = totalDeletedBytes / (1024d * 1024d);
            return ModuleExecutionResult.Ok(
                $"Cache purge completed. Files removed: {totalDeletedFiles}, approx space reclaimed: {reclaimedMb:F2} MB.",
                details.ToArray());
        }, cancellationToken);
    }

    private static CleanupStats CleanSoftwareDistributionDownload(string downloadPath)
    {
        var details = new List<string>();
        var filesDeleted = 0;
        var bytesDeleted = 0L;

        if (!Directory.Exists(downloadPath))
        {
            details.Add($"Skipped missing path: {downloadPath}");
            return new CleanupStats(0, 0, 0, 0, details);
        }

        var services = new[] { "wuauserv", "bits" };
        var stoppedServices = new List<System.ServiceProcess.ServiceController>();

        try
        {
            foreach (var serviceName in services)
            {
                try
                {
                    var service = new System.ServiceProcess.ServiceController(serviceName);
                    if (service.Status != System.ServiceProcess.ServiceControllerStatus.Stopped &&
                        service.Status != System.ServiceProcess.ServiceControllerStatus.StopPending)
                    {
                        service.Stop();
                        service.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(20));
                        details.Add($"Service stopped: {serviceName}");
                    }

                    stoppedServices.Add(service);
                }
                catch (Exception ex)
                {
                    details.Add($"Service stop warning ({serviceName}): {ex.Message}");
                }
            }

            var stats = DeleteDirectoryContents(downloadPath);
            filesDeleted += stats.FilesDeleted;
            bytesDeleted += stats.BytesDeleted;
            details.Add($"Cleaned: {downloadPath} | Files removed: {stats.FilesDeleted} | Failures: {stats.Failures} | Remaining files: {stats.RemainingFiles}");
        }
        finally
        {
            foreach (var service in stoppedServices)
            {
                try
                {
                    service.Start();
                    service.WaitForStatus(System.ServiceProcess.ServiceControllerStatus.Running, TimeSpan.FromSeconds(20));
                    details.Add($"Service restarted: {service.ServiceName}");
                }
                catch (Exception ex)
                {
                    details.Add($"Service restart warning ({service.ServiceName}): {ex.Message}");
                }
                finally
                {
                    service.Dispose();
                }
            }
        }

        return new CleanupStats(filesDeleted, bytesDeleted, 0, 0, details);
    }

    private static CleanupStats DeleteDirectoryContents(string root)
    {
        var filesDeleted = 0;
        var bytesDeleted = 0L;
        var failures = 0;

        try
        {
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var info = new FileInfo(file);
                    bytesDeleted += info.Exists ? info.Length : 0;
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                    filesDeleted++;
                }
                catch
                {
                    failures++;
                }
            }

            foreach (var dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories)
                         .OrderByDescending(s => s.Length))
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch
                {
                    failures++;
                }
            }
        }
        catch
        {
            failures++;
        }

        var remainingFiles = CountFilesSafe(root);
        return new CleanupStats(filesDeleted, bytesDeleted, failures, remainingFiles, []);
    }

    private static int CountFilesSafe(string root)
    {
        try
        {
            if (!Directory.Exists(root))
            {
                return 0;
            }

            return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Count();
        }
        catch
        {
            return -1;
        }
    }

    private sealed record CleanupStats(int FilesDeleted, long BytesDeleted, int Failures, int RemainingFiles, IReadOnlyList<string> Details);
}
