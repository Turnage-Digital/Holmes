# Development Environment Quickstart

1. **Start supporting services**
    - `docker compose up -d` (MySQL, Seq, etc.)
    - `dotnet run --project src/Holmes.Identity.Server` (IdentityServer + ASP.NET Identity on https://localhost:5000)

2. **Reset databases (if needed)**
    - Run `pwsh ./ef-reset.ps1`
    - Drops + recreates the Core/Users/Customers/Subjects/Workflow/Intake **and** Identity/IdentityServer schemas and
      reapplies the single “Initial” migration for each context.

3. **Run the app**
    - `dotnet run --project src/Holmes.App.Server` (APIs/SSE)
    - `dotnet run --project src/Holmes.Internal.Server` (SPA host at https://localhost:5003; proxies to Vite in development)
    - `npm run dev --prefix src/Holmes.Internal` (if you want live SPA reloads)

4. **Login**
    - Visit https://localhost:5001 and follow the “Continue with Holmes Identity” flow
    - Credentials: `admin@holmes.dev` / `ChangeMe123!` (reset immediately via Identity UI)

The `DevelopmentDataSeeder` hosted service ensures an Admin user and demo customer exist whenever you run in
Development.***

5. **Observability**
    - Metrics: Prometheus-compatible scrape endpoint lives at `https://localhost:5001/metrics`. Point Grafana Agent (or
      another collector) at that URL to power the dashboards.
    - Traces: set `OpenTelemetry__Exporter__Endpoint` (for example `http://localhost:4317`) to stream traces to your
      OTLP collector. Leave it unset to run purely locally.

6. **Runbooks & Verification**
    - See `docs/RUNBOOKS.md` for database reset, projection verification, and observability hookup procedures you can
      follow (or share with teammates) without spelunking through source.

**Port map (dev)**
- IdentityServer: https://localhost:5000
- App API/SSE: https://localhost:5001
- Intake SPA host: https://localhost:5002
- Internal SPA BFF host: https://localhost:5003
- Vite dev: Internal https://localhost:3000, Intake https://localhost:3001
