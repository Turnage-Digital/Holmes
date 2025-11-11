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
