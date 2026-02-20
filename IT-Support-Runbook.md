# WinBus Utility - IT Support Runbook

## Purpose

This runbook provides step-by-step operational guidance for IT support engineers to maintain, deploy, and troubleshoot WinBus Utility in Windows 11 Pro environments.

Related quick-reference:
- [L1-Helpdesk-Quick-Card.md](L1-Helpdesk-Quick-Card.md)

## 1) Prerequisites

- Windows 11 Pro endpoint or VM
- .NET SDK 8.0+
- Local Administrator rights for full module execution
- PowerShell execution policy that allows signed/internal scripts

## 2) Build and release steps

### Build utility EXE
```powershell
.\scripts\Build-Utility-Exe.ps1
```

Expected output:
- `bin\Release\net8.0-windows\win-x64\publish\WinBus.Utility.exe`

### Build MSI
```powershell
dotnet build .\installer\WinBus.Installer.wixproj -c Release
```

Expected output:
- `installer\bin\x64\Release\WinBus.Utility.Installer.msi`

### Build + sign pipeline
```powershell
.\scripts\Create-And-Sign-Installer.ps1
```

Optional self-signed test certificate:
```powershell
.\scripts\Create-And-Sign-Installer.ps1 -CreateSelfSignedCert
```

## 3) Safe validation run (post-build)

1. Launch utility:
```powershell
.\scripts\Run-Utility.ps1
```
2. Confirm language menu appears.
3. Perform smoke test by selecting `N` for all modules.
4. Verify end-of-run summary is printed.

## 4) Production operation guidance

Recommended module order:
1. Safety Checkpoint
2. Telemetry Module
3. Cache Purge
4. Startup Audit
5. Tray Watchdog (optional)

Support policy:
- Always keep user consent on each module.
- For managed enterprise devices, follow change request approval before execution.

## 5) Troubleshooting matrix

### A) Module failures due to permissions
- Symptom: service/registry errors
- Action: rerun terminal as Administrator

### B) Temp files still present after cleanup
- Symptom: data remains in temp folders
- Cause: locked/recreated files
- Action: close user apps, rerun cleanup, optionally reboot then rerun

### C) Telemetry module issues
- Symptom: `DiagTrack` cannot be stopped
- Action: verify service policy and endpoint hardening controls

### D) MSI install/signing issues
- Symptom: install blocked or signature warnings
- Action: validate certificate chain, timestamp access, endpoint trust policies

## 6) Maintenance cadence

- Weekly: one test run on validation endpoint
- Monthly: rebuild EXE/MSI and confirm script health
- Quarterly: verify module behavior against latest Windows updates
- As needed: update translations and support docs

## 7) Logs and support artifacts

- Startup scan report: `logs\startup-audit-report.txt`
- Watchdog threshold log: `logs\watchdog-alerts.log`
- Remote monitor transport log: `logs\remote-monitor-events.log`
- Build artifacts: `bin\Release\...` and `installer\bin\x64\Release\...`
- Keep ticket references for each production optimization session

## 8) Remote monitoring operations

Configuration file:
- `remote-monitoring.json` next to the EXE

Required support actions:
1. Set `Enabled=true`.
2. Set central `EndpointUrl` for your monitoring receiver.
3. Set `Fleet` to your support group or customer group.
4. Optionally set `ApiKey` and `NodeName`.

Validation:
- Run utility once and confirm `logs\remote-monitor-events.log` shows `SENT` entries.
- Verify central dashboard receives module status and heartbeat events from the device.

Backend persistence recommendation:
- For production monitoring, configure API `MonitoringApi:StoreProvider=SqlServer`.
- Set valid SQL connection string under `MonitoringApi:SqlServer:ConnectionString`.

## 9) Monitoring dashboard (Vue 3)

Project:
- `monitoring-ui`

Run steps:
1. `cd monitoring-ui`
2. `npm install`
3. `npm run dev`

Production build:
1. `cd monitoring-ui`
2. `npm run build`
3. Host `monitoring-ui\dist` using IIS static files, Nginx, or CDN

Dashboard API requirements:
- Query API (`WinBus.Monitoring.QueryApi`) must be reachable from browser.
- If API key is enabled, provide same key in dashboard settings.

Scale recommendation:
- Keep write ingestion on `WinBus.Monitoring.Api`.
- Keep dashboard reads on `WinBus.Monitoring.QueryApi`.

## 10) Docker operations (optional)

For quick full-stack bring-up:
1. Create `.env` from `.env.docker.example`.
2. Set secure `SA_PASSWORD`.
3. Run:
```powershell
docker compose up -d --build
```

Verify:
- UI: `http://localhost:8088`
- Write API: `http://localhost:5000/api/status/health`
- Query API: `http://localhost:5001/api/query/health`

Shutdown:
```powershell
docker compose down
```
