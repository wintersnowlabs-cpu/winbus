# WinBus Monitoring Server (Modular)

This is a modular, extensible monitoring backend for WinBus Utility fleet status.

## Project structure

- `src/WinBus.Monitoring.Contracts` - Shared DTO contracts
- `src/WinBus.Monitoring.Core` - Service interfaces and core orchestration
- `src/WinBus.Monitoring.Infrastructure` - Storage implementation (`InMemoryStatusEventStore`)
- `src/WinBus.Monitoring.Infrastructure.SqlServer` - SQL Server storage implementation (`SqlServerStatusEventStore`)
- `src/WinBus.Monitoring.Api` - HTTP API host for IIS/Kestrel
- `src/WinBus.Monitoring.QueryApi` - read-only query API host for dashboard and reporting

This structure allows replacing storage/services without changing API contracts.

## API endpoints

Write/Ingestion API (`WinBus.Monitoring.Api`):
- `GET /api/status/health`
- `POST /api/status/events`
- `GET /api/status/summary`
- `GET /api/status/nodes`
- `GET /api/status/nodes/{nodeName}/events?take=100`

Read-only Query API (`WinBus.Monitoring.QueryApi`):
- `GET /api/query/health`
- `GET /api/query/summary`
- `GET /api/query/nodes`
- `GET /api/query/nodes/{nodeName}/events?take=100`

## Security

Set `MonitoringApi:ApiKey` in `appsettings.json`.

If set, callers must provide header:
- `X-Api-Key: <value>`

## Store provider selection

Configure in `src/WinBus.Monitoring.Api/appsettings.json`:

```json
"MonitoringApi": {
  "StoreProvider": "InMemory",
  "SqlServer": {
    "ConnectionString": "Server=localhost;Database=WinBusMonitoring;Trusted_Connection=True;TrustServerCertificate=True;",
    "TableName": "StatusEvents",
    "AutoCreateSchema": true
  }
}
```

- `InMemory`: fast local testing.
- `SqlServer`: persistent enterprise storage for production.

## Build

```powershell
dotnet build .\WinBus.Monitoring.slnx -c Release
```

## Docker deployment

From repository root:

1. Create `.env` from `.env.docker.example`.
2. Set strong `SA_PASSWORD`.
3. Run:

```powershell
docker compose up -d --build
```

Container routing:
- `monitoring-api` (write): port `5000`
- `monitoring-query-api` (read): port `5001`
- `monitoring-ui`: port `8088`

Health checks:
- `http://localhost:5000/api/status/health`
- `http://localhost:5001/api/query/health`

## Run locally

```powershell
dotnet run --project .\src\WinBus.Monitoring.Api\WinBus.Monitoring.Api.csproj
```

## IIS deployment (Windows Server)

1. Install .NET Hosting Bundle for the target runtime.
2. Publish API:
```powershell
dotnet publish .\src\WinBus.Monitoring.Api\WinBus.Monitoring.Api.csproj -c Release -o .\publish\monitoring-api
```
3. In IIS, create a new Site or Application pointing to `publish\monitoring-api`.
4. Set Application Pool to **No Managed Code**.
5. Ensure site binding + firewall are configured.
6. Set `appsettings.json` (`MonitoringApi` section), especially `ApiKey`.
7. For production persistence set `MonitoringApi:StoreProvider` to `SqlServer` and valid SQL connection string.

Split-service recommendation:
- Host `WinBus.Monitoring.Api` as write endpoint for agents (`/api/status/events`).
- Host `WinBus.Monitoring.QueryApi` as read endpoint for UI/reporting (`/api/query/*`).
- Both can point to same SQL database for shared state.

## Integrating desktop clients

On each WinBus utility machine, edit `remote-monitoring.json`:

```json
{
  "Enabled": true,
  "EndpointUrl": "https://your-server/api/status/events",
  "ApiKey": "YOUR_SHARED_KEY",
  "HeartbeatSeconds": 60,
  "Fleet": "prod-fleet",
  "NodeName": "PC-001"
}
```

## Integrating operations dashboard (Vue 3)

Dashboard project:
- `..\monitoring-ui`

Build:
```powershell
cd ..\monitoring-ui
npm run build
```

The dashboard consumes these API endpoints:
- `GET /api/query/summary`
- `GET /api/query/nodes`
- `GET /api/query/nodes/{nodeName}/events?take=100`

## Extensibility guidance

- Add new storage (SQL/Redis) by implementing `IStatusEventStore` in a separate infrastructure project.
- Add queueing/event-bus by wrapping `IEventIngestionService`.
- Split read/write APIs into separate hosts using the same `Contracts` and `Core` layers.

## SQL schema behavior

- When `AutoCreateSchema=true`, the SQL provider creates table/indexes automatically on startup.
- Retention is applied by deleting rows older than `RetentionHours` during ingestion.
