# WinBus Utility - L1 Helpdesk Quick Card

## Goal

Use this quick checklist for first-line support when assisting users with WinBus Utility.

## 1) Fast pre-checks (30 seconds)

- Confirm user is on Windows 11 Pro.
- Confirm utility launch path exists:
  - `bin\Release\net8.0-windows\win-x64\publish\WinBus.Utility.exe`
- Ask user to run terminal as Administrator for full module support.

## 2) Safe first run

1. Launch utility.
2. Select preferred language.
3. Validate consent prompts appear for each module.
4. If only smoke test needed, answer `N` for all modules.

Expected result:
- Final execution summary appears with module status.

## 3) Common user questions

### “Will watchdog run forever?”
- No. It runs only for the current session.
- User can stop it with `Ctrl+C`.

### “Why are temp files still there?”
- Some files are locked or recreated by Windows.
- Ask user to close apps and rerun Module 3.
- If needed, reboot and rerun Module 3.

### “Is disk cleanup included?”
- Yes, Module 3 cleans `%TEMP%`, `C:\Windows\Temp`, and update download cache path.

## 4) When to escalate to L2/L3

Escalate if any of these happen:
- Repeated service stop/start failures (`DiagTrack`, `wuauserv`, `bits`).
- MSI install blocked by policy/signature trust.
- Utility crashes consistently on launch.
- Cleanup reports persistent high failures after reboot + admin run.

## 5) L1 evidence to collect before escalation

- Screenshot of error message.
- Whether run was elevated (Admin or not).
- Selected language and module where failure occurred.
- `startup-audit-report.txt` if startup module was used.
- Exact EXE/MSI path and build date.

## 6) L1 standard responses

- “Please rerun as Administrator and retry only the failed module.”
- “For temp cleanup, close running apps first and rerun Module 3.”
- “Watchdog is session-based and not permanent unless restarted manually.”

## 7) Quick commands for support

Build EXE:
```powershell
.\scripts\Build-Utility-Exe.ps1
```

Run utility:
```powershell
.\scripts\Run-Utility.ps1
```

Build MSI:
```powershell
dotnet build .\installer\WinBus.Installer.wixproj -c Release
```
