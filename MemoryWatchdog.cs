using System.Diagnostics;
using System.Windows.Forms;

namespace WinBus.Utility;

internal static class MemoryWatchdog
{
    public static ModuleExecutionResult ExecuteWithConsent()
    {
        Console.WriteLine(Localization.T("watchdog_intro1"));
        Console.WriteLine(Localization.T("watchdog_intro2"));
        Console.Write($"{Localization.T("watchdog_threshold_prompt")}: ");

        var input = Console.ReadLine();
        var thresholdMb = int.TryParse(input, out var value) && value > 0 ? value : 1200;

        Console.WriteLine(Localization.T("watchdog_starting"));

        using var context = new WatchdogContext(thresholdMb);
        Application.Run(context);

        return ModuleExecutionResult.Ok(
            Localization.T("watchdog_ended"),
            string.Format(Localization.T("watchdog_threshold_used"), thresholdMb),
            Localization.T("watchdog_interval_detail"),
            string.Format(Localization.T("watchdog_log_path"), context.LogFilePath));
    }

    private sealed class WatchdogContext : ApplicationContext
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly System.Windows.Forms.Timer _timer;
        private readonly long _thresholdBytes;
        private readonly int _thresholdMb;
        private readonly object _logLock = new();

        public string LogFilePath { get; }

        public WatchdogContext(int thresholdMb)
        {
            _thresholdMb = thresholdMb;
            _thresholdBytes = thresholdMb * 1024L * 1024L;
            AppPaths.EnsureDirectories();
            LogFilePath = AppPaths.WatchdogAlertLogPath;

            _notifyIcon = new NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Information,
                Text = "WinBus Tray Watchdog",
                Visible = true,
                BalloonTipTitle = "WinBus Watchdog"
            };

            _notifyIcon.BalloonTipText = $"Monitoring high-memory processes (>{thresholdMb} MB).";
            _notifyIcon.ShowBalloonTip(3000);

            AppendLog($"Session started | Threshold={thresholdMb} MB");

            _timer = new System.Windows.Forms.Timer { Interval = 30_000 };
            _timer.Tick += OnTimerTick;
            _timer.Start();

            Application.ApplicationExit += (_, _) => _notifyIcon.Visible = false;
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            try
            {
                var offenders = Process.GetProcesses()
                    .Where(p =>
                    {
                        try
                        {
                            return p.PrivateMemorySize64 > _thresholdBytes;
                        }
                        catch
                        {
                            return false;
                        }
                    })
                    .Select(p =>
                    {
                        try
                        {
                            var memoryMb = p.PrivateMemorySize64 / (1024 * 1024);
                            return new ProcessAlert(p.ProcessName, p.Id, memoryMb);
                        }
                        catch
                        {
                            return null;
                        }
                    })
                    .Where(alert => alert is not null)
                    .Select(alert => alert!)
                    .Take(3)
                    .ToList();

                if (offenders.Count > 0)
                {
                    _notifyIcon.BalloonTipText = $"High memory: {string.Join(", ", offenders.Select(o => $"{o.Name} ({o.MemoryMb} MB)"))}";
                    _notifyIcon.ShowBalloonTip(3000);

                    AppendLog($"Threshold exceeded | Threshold={_thresholdMb} MB | Offenders={string.Join("; ", offenders.Select(o => $"{o.Name}[PID:{o.Pid}]={o.MemoryMb}MB"))}");
                    RemoteStatusPublisher.Publish(
                        "watchdog_threshold",
                        "Tray Watchdog",
                        "alert",
                        $"Threshold {_thresholdMb} MB exceeded by: {string.Join(", ", offenders.Select(o => $"{o.Name}[{o.Pid}]={o.MemoryMb}MB"))}");
                }
            }
            catch
            {
            }
        }

        private void AppendLog(string message)
        {
            try
            {
                lock (_logLock)
                {
                    AppPaths.RotateIfTooLarge(LogFilePath);
                    File.AppendAllText(LogFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
                }
            }
            catch
            {
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                AppendLog("Session ended");
                _timer.Stop();
                _timer.Tick -= OnTimerTick;
                _timer.Dispose();
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }

            base.Dispose(disposing);
        }

        private sealed record ProcessAlert(string Name, int Pid, long MemoryMb);
    }
}
