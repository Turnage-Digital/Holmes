# Development Environment Quickstart

1. **Start supporting services**
    - `docker compose up -d` (MySQL, Seq, etc.)
    - `dotnet run --project src/Holmes.Identity.Server` (dev IdP on https://localhost:6001)

2. **Reset databases (if needed)**
    - Run `pwsh ./ef-reset.ps1`
    - Drops + recreates the Core/Users/Customers/Subjects schemas and reapplies migrations.

3. **Run the app**
    - `dotnet run --project src/Holmes.App.Server`
    - `npm run dev --prefix src/Holmes.Client` (if you want live SPA reloads)

4. **Login**
    - Visit https://localhost:5001 and follow the “Continue with Holmes Identity” flow
    - Credentials: `admin` / `password`

The `DevelopmentDataSeeder` hosted service ensures an Admin user and demo customer exist whenever you run in Development.***

5. **Observability**
    - Metrics: Prometheus-compatible scrape endpoint lives at `https://localhost:5001/metrics`. Point Grafana Agent (or
      another collector) at that URL to power the dashboards.
    - Traces: set `OpenTelemetry__Exporter__Endpoint` (for example `http://localhost:4317`) to stream traces to your
      OTLP collector. Leave it unset to run purely locally.

6. **Runbooks & Verification**
    - See `docs/RUNBOOKS.md` for database reset, projection verification, and observability hookup procedures you can
      follow (or share with teammates) without spelunking through source.
