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

## Deploy to Azure Container Apps

The deployment script uses an existing Azure Container Registry, creates a Container Apps environment and pull identity, builds the image with ACR Tasks, injects Tesla credentials as Container Apps secrets, and prints the public URLs required by Tesla.

```powershell
az login
./scripts/New-TeslaKeyPair.ps1

$env:TESLA_CLIENT_ID = "YOUR_CLIENT_ID"
$env:TESLA_CLIENT_SECRET = Read-Host "Tesla client secret"

./scripts/Deploy-AzureContainerApp.ps1 `
  -ResourceGroup "YOUR_RESOURCE_GROUP" `
  -AcrName "YOUR_ACR_NAME" `
  -Location "eastus" `
  -AppName "tesladash"
```

The script prints the Allowed Origin, Redirect URI, and well-known public-key URL. Put the first two into the Tesla Developer Portal and verify the public-key URL returns a PEM key. The local private-key file is Git-ignored; back it up securely and never commit it.

The initial deployment runs one replica but still stores OAuth tokens and ASP.NET Data Protection keys on container-local storage. A restart or new revision can require reconnecting Tesla. Add durable Azure storage before treating this as production.

## Roadmap

- Live state without unnecessary vehicle wake-ups
- Charge sessions, kWh, estimated cost, and completion alerts
- FSD miles and percentage from Fleet Telemetry on supported HW4 vehicles
- Software/FSD version history and update notifications
- Opt-in household alerts for departures, arrivals, motion, charging, and lock changes
- Quiet hours, location redaction, cooldowns, consent, and an alert audit log

The UI depends on `IVehicleDashboardService`. Replace its demo implementation with an OAuth-backed Fleet API service; ingest Fleet Telemetry through a public TLS endpoint and persist raw events plus derived drives and charge sessions. Keep all tokens, keys, VINs, and precise location history out of source control. Start read-only and add vehicle commands only after authentication, audit, and confirmation controls are mature.
