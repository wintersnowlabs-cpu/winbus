# WinBus Utility (Windows 11 Pro Tuning Suite)

A consent-driven C# utility that runs step-by-step Windows tuning modules with clear explanations before execution.

## IT Support handover (Operations)

This repository is ready for IT support teams to operate and maintain.

Primary runbook:
- [IT-Support-Runbook.md](IT-Support-Runbook.md)
- [L1-Helpdesk-Quick-Card.md](L1-Helpdesk-Quick-Card.md)

Primary support responsibilities:
- Build and publish EXE/MSI artifacts.
- Run optimization sessions with user consent.
- Validate outcome (space reclaimed, startup report, watchdog alerts).
- Troubleshoot permission/service issues.
- Maintain release cadence and signed installer process.

## Language support

At startup, the utility asks the user to select a language.

Supported languages:
- English
- Welsh (Cymraeg)
- Bengali (বাংলা)
- Hindi (हिन्दी)
- Tamil (தமிழ்)
- Telugu (తెలుగు)
- Spanish (Español)
- French (Français)
- German (Deutsch)
- Arabic (العربية)

## What this utility includes

1. **Safety Checkpoint**
   - Creates a restore point via WMI before deep changes.
2. **Telemetry Engine Kill-Switch**
   - Stops `DiagTrack` and sets policy `AllowTelemetry=0`.
3. **Cache & Memory Purge**
   - Cleans user temp, system temp, and `SoftwareDistribution` contents.
4. **Startup Audit Agent**
   - Scans startup registry keys and exports a report.
5. **Tray Watchdog**
   - Optional memory watcher with tray notifications (30s interval).

Every module asks for explicit **Y/N** consent before running.

## Build and generate EXE

### Prerequisites
- Windows 11 Pro
- .NET SDK 8.0+
- Run terminal as **Administrator** for full module support

### Build
```powershell
dotnet build -c Release
```

### Publish single EXE
```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

Output EXE:
- `bin\Release\net8.0-windows\win-x64\publish\WinBus.Utility.exe`

Recommended IT support check after publish:
- Verify EXE exists and launch test once.
- Confirm language menu appears correctly.
- Use non-destructive smoke run (`N` for all modules) before production rollout.

## Build MSI installer

### Quick build MSI
```powershell
dotnet build .\installer\WinBus.Installer.wixproj -c Release
```

Output MSI:
- `installer\bin\x64\Release\WinBus.Utility.Installer.msi`

### Build + optional signing in one step
```powershell
.\scripts\Create-And-Sign-Installer.ps1
```

### Build + create self-signed certificate + sign artifacts
```powershell
.\scripts\Create-And-Sign-Installer.ps1 -CreateSelfSignedCert
```

### Build + sign with an existing certificate thumbprint
```powershell
.\scripts\Create-And-Sign-Installer.ps1 -CertThumbprint "YOUR_CERT_THUMBPRINT"
```

This script publishes the EXE, builds the MSI, then signs both EXE and MSI when a certificate is provided.

Installer behavior:
- Uses a feature-selection UI during setup.
- "Desktop Shortcut" is optional and default-off.
- Users can enable Desktop Shortcut during MSI installation.

IT support release checklist:
- Build EXE (`Build-Utility-Exe.ps1`).
- Build MSI (`WinBus.Installer.wixproj`).
- Sign artifacts for enterprise distribution.
- Validate install/uninstall on a clean Windows 11 Pro test VM.
- Archive release artifacts and build date in ticket/change record.

## Run
```powershell
.\bin\Release\net8.0-windows\win-x64\publish\WinBus.Utility.exe
```

## Manual jobs (scripts)

Generate EXE manually:
```powershell
.\scripts\Build-Utility-Exe.ps1
```

Run utility manually:
```powershell
.\scripts\Run-Utility.ps1
```

Operational automation script:
```powershell
.\scripts\Create-And-Sign-Installer.ps1
```

## VS Code jobs (Tasks)

From VS Code, run `Terminal > Run Task` and use:
- `WinBus: Build EXE`
- `WinBus: Run Utility`
- `WinBus: Build MSI`
- `WinBus: Build+Sign Installer`

## Notes
- If not run elevated, some modules can fail due to permissions.
- Startup audit report is generated at `logs\startup-audit-report.txt`.
- Watchdog alert records are written to `logs\watchdog-alerts.log`.
- Remote monitor delivery results are written to `logs\remote-monitor-events.log`.
- For safety, create and keep a restore point before major system changes.
- MSI includes a Start Menu shortcut (`WinBus Utility`) on install.
- MSI sets Add/Remove Programs icon metadata using the app executable icon.

## Remote monitoring across multiple PCs

The utility now supports centralized status publishing for remote monitoring.

Monitoring backend project:
- [monitoring-server/README.md](monitoring-server/README.md)
- [monitoring-server/WinBus.Monitoring.slnx](monitoring-server/WinBus.Monitoring.slnx)

Monitoring dashboard UI project:
- [monitoring-ui/README.md](monitoring-ui/README.md)

Architecture blueprint:
- [ARCHITECTURE.md](ARCHITECTURE.md)

Configuration file:
- `remote-monitoring.json` (auto-created beside the EXE on first run)

Set these values:
- `Enabled`: `true` to turn on remote publishing
- `EndpointUrl`: your central API endpoint that accepts JSON POST
- `ApiKey`: optional API key sent as `X-Api-Key`
- `HeartbeatSeconds`: periodic status interval (minimum 15)
- `Fleet`: logical group name (for dashboard filtering)
- `NodeName`: device name override (default machine name)

Published status includes:
- Session start/end
- Module prompt/start/result (success/failed/skipped)
- Watchdog threshold alerts
- Periodic heartbeat events

This allows a central monitoring service to show what is happening on all enrolled PCs in near real time.

## IT support maintenance schedule

- Daily (if actively deployed): monitor support tickets for failed module runs.
- Weekly: run one validation session on a test endpoint.
- Monthly: rebuild artifacts with current .NET SDK and verify signing certificate validity.
- Quarterly: review module scope against Windows 11 updates and enterprise policy changes.

## Troubleshooting guide (IT support)

- Utility says not elevated:
   - Relaunch PowerShell/Terminal as Administrator.
- Temp cleanup leaves files behind:
   - Close heavy apps and rerun Module 3; some files are locked/recreated by Windows.
- Telemetry module fails:
   - Check `DiagTrack` service permissions and local policy restrictions.
- MSI install blocked:
   - Verify signature trust chain and endpoint execution policy.
- Non-English labels look garbled in old terminal fonts:
   - Use Windows Terminal and UTF-8 capable font.

## End-to-end validation (recommended)

1. Build utility EXE:
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Build-Utility-Exe.ps1
```
2. Build monitoring backend:
```powershell
dotnet build .\monitoring-server\WinBus.Monitoring.slnx -c Release
```
3. Start monitoring API:
```powershell
dotnet run --project .\monitoring-server\src\WinBus.Monitoring.Api\WinBus.Monitoring.Api.csproj
```
4. Start query API:
```powershell
dotnet run --project .\monitoring-server\src\WinBus.Monitoring.QueryApi\WinBus.Monitoring.QueryApi.csproj
```
5. Build monitoring UI:
```powershell
cd .\monitoring-ui
npm run build
```
6. Configure endpoint utility `remote-monitoring.json` and run utility.
7. Open dashboard and verify node status/events are visible through `/api/query/*`.

## Docker Compose stack

The repository now includes a full container stack:
- `docker-compose.yml`
- `monitoring-server/src/WinBus.Monitoring.Api/Dockerfile`
- `monitoring-server/src/WinBus.Monitoring.QueryApi/Dockerfile`
- `monitoring-ui/Dockerfile`

Setup:
1. Copy `.env.docker.example` to `.env`.
2. Update `SA_PASSWORD` and optional `MONITORING_API_KEY`.

Run:
```powershell
docker compose up -d --build
```

Endpoints:
- Monitoring UI: `http://localhost:8088`
- Write API health: `http://localhost:5000/api/status/health`
- Query API health: `http://localhost:5001/api/query/health`

Stop:
```powershell
docker compose down
```
