# WinBus Monitoring UI (Vue 3)

Vue 3 dashboard for remote fleet monitoring of WinBus utility endpoints.

## Features

- API endpoint and API key configuration in UI.
- Summary cards (`totalNodes`, `activeNodes`, `alertsLastHour`, `failuresLastHour`).
- Node table with latest status.
- Node event stream (latest 100 events).
- Auto-refresh controls (pause/resume).

## Setup

Install dependencies:

```powershell
npm install
```

Optional environment file:

```powershell
copy .env.example .env
```

Example `.env` values:

```ini
VITE_MONITORING_API_BASE_URL=http://localhost:5000
VITE_MONITORING_API_KEY=
```

## Run dev server

```powershell
npm run dev
```

## Build production

```powershell
npm run build
```

## API dependencies

The dashboard expects the monitoring API from:
- `monitoring-server/src/WinBus.Monitoring.Api`
- `monitoring-server/src/WinBus.Monitoring.QueryApi` (recommended for reads)

Endpoints used:
- `GET /api/query/summary`
- `GET /api/query/nodes`
- `GET /api/query/nodes/{nodeName}/events?take=100`
