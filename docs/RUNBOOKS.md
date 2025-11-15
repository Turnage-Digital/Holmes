# Holmes Runbooks

Authoritative playbooks for common operational tasks during development and Phase 1.9 hardening.

---

## Database Reset & Reseed

1. `docker compose up -d` — ensures the MySQL container is online.
2. `pwsh ./ef-reset.ps1` — drops Core/Users/Customers/Subjects schemas, deletes old migrations, scaffolds the Initial
   migrations (plus `AddCustomerProfiles`), and reapplies them. The script is idempotent thanks to guarded folder
   deletes, so you can run it repeatedly.
3. `dotnet run --project src/Holmes.Identity.Server` — starts the dev IdP if you want to log into the app afterwards.
4. `dotnet run --project src/Holmes.App.Server` — boot the host; the `DevelopmentDataSeeder` will recreate the Admin
   user
   and demo customer automatically once migrations are complete.

**Verification**: connect with `mysql` or TablePlus and confirm the four schemas exist with fresh
`__EFMigrationsHistory`
rows dated from the current run.

---

## Projection Verification / Replay

Holmes v1 stores read models directly in the module DbContexts. To verify or rebuild the projections:

1. Reset databases (previous runbook).
2. Launch the host and execute the core flows:
    - Invite + activate a user; grant/revoke a role.
    - Create a customer; add/update contacts.
    - Register two subjects and merge one into the other.
3. Inspect the following tables to confirm projections match expectations:
    - `users.user_directory` — every activated user appears with issuer/subject + roles.
    - `customers.customer_profiles` / `customers.customer_contacts` — profile + contact counts match created data.
    - `subjects.subject_directory` — merged subjects mark `merged_into_subject_id`.
4. For automated coverage, run `dotnet test Holmes.sln`; the Subjects test suite now asserts registration/alias/merge
   invariants, and Customers/Users suites guard their aggregate logic.

When dedicated projection runners arrive, add their replay commands here (e.g.,
`dotnet run --project Holmes.Projections.Runner --projection user_directory`).

## Intake Session Projection & Order Timeline Verification

1. After running any intake flow (invite → submit → accept), query the projection tables to ensure the read models are
   synchronized:

   ```sql
   SELECT intake_session_id, status, submitted_at, accepted_at
   FROM intake.intake_sessions_projection
   ORDER BY last_touched_at DESC
   LIMIT 5;
   ```

   ```sql
   SELECT order_id, event_type, description, occurred_at
   FROM workflow.order_timeline_events
   ORDER BY occurred_at DESC
   LIMIT 10;
   ```

2. If projections drift, run the application again and replay the events via MediatR (`dotnet test Holmes.sln` exercises
   the handlers) or truncate the tables and re-run the happy path; the handlers will rebuild the projections on the next
   event dispatch.
3. Watch the metrics exposed at `/metrics` — new counters `holmes.intake_sessions.projection_updates` and
   `holmes.timeline.events_written` confirm the handlers are executing. Wire them into Grafana alongside the existing
   UnitOfWork histograms.

---

## Observability Hooks

1. Metrics (Prometheus):
    - Ensure Holmes.App.Server is running.
    - Configure your Prometheus (or Grafana Agent) scrape job:
      ```yaml
      scrape_configs:
        - job_name: holmes-app
          metrics_path: /metrics
          scheme: https
          static_configs:
            - targets: ['localhost:5001']
      ```
    - Dashboards can now chart runtime, ASP.NET Core, HttpClient, and `holmes.unit_of_work.*` histograms/counters.
2. Traces (OTLP):
    - Set the environment variable `OpenTelemetry__Exporter__Endpoint=http://localhost:4317` (or any OTLP gRPC endpoint)
      before launching Holmes.App.Server.
    - Start your collector (e.g., OpenTelemetry Collector, Grafana Agent) with an OTLP receiver on that endpoint.
    - Use Grafana Tempo, Jaeger, or another backend to view traces; look for `UnitOfWork.SaveChanges` spans with tags
      describing DbContext, provider, and duration.

Keep screenshots/links to the canonical dashboards in the project wiki once they exist so the Phase 1.9 readiness review
can reference them directly.
