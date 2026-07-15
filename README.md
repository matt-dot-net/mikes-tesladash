# TeslaDash

A privacy-conscious ASP.NET Core dashboard for a household Tesla. It currently runs with demo data; Fleet API connectivity and persistent telemetry are the next milestone.

## Run

```powershell
dotnet run --project TeslaDash
```

Or run `docker compose up --build` and open http://localhost:8080.

## Connect Tesla locally

Create a Tesla Developer application using authorization-code grant and enable Vehicle Information. Register the exact redirect URI `http://localhost:5187/Auth/Callback`, unless you change `Tesla:RedirectUri`.

Store credentials outside configuration files:

```powershell
dotnet user-secrets set "Tesla:ClientId" "YOUR_CLIENT_ID" --project TeslaDash
dotnet user-secrets set "Tesla:ClientSecret" "YOUR_CLIENT_SECRET" --project TeslaDash
dotnet run --project TeslaDash --urls http://localhost:5187
```

Open `/Connect`, choose **Continue with Tesla**, and approve the read-only scopes. Tokens are encrypted with ASP.NET Core Data Protection; the protected token and encryption keys live under `TeslaDash/data`, which is ignored by Git. In production, use HTTPS, secure secret injection, persistent `/data`, and protected backups.

## Roadmap

- Live state without unnecessary vehicle wake-ups
- Charge sessions, kWh, estimated cost, and completion alerts
- FSD miles and percentage from Fleet Telemetry on supported HW4 vehicles
- Software/FSD version history and update notifications
- Opt-in household alerts for departures, arrivals, motion, charging, and lock changes
- Quiet hours, location redaction, cooldowns, consent, and an alert audit log

The UI depends on `IVehicleDashboardService`. Replace its demo implementation with an OAuth-backed Fleet API service; ingest Fleet Telemetry through a public TLS endpoint and persist raw events plus derived drives and charge sessions. Keep all tokens, keys, VINs, and precise location history out of source control. Start read-only and add vehicle commands only after authentication, audit, and confirmation controls are mature.
