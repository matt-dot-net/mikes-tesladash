param(
    [Parameter(Mandatory)]
    [string]$Domain,

    [string]$FleetApiBaseUrl = "https://fleet-api.prd.na.vn.cloud.tesla.com",
    [string]$FleetAuthBaseUrl = "https://fleet-auth.prd.vn.cloud.tesla.com"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($env:TESLA_CLIENT_ID)) {
    throw "Set TESLA_CLIENT_ID in this PowerShell session."
}
if ([string]::IsNullOrWhiteSpace($env:TESLA_CLIENT_SECRET)) {
    throw "Set TESLA_CLIENT_SECRET in this PowerShell session."
}

$normalizedDomain = $Domain.Trim().TrimEnd('/')
if ($normalizedDomain -match '^https?://') {
    $normalizedDomain = ([Uri]$normalizedDomain).Host
}

Write-Host "Requesting a Tesla partner token..."
$tokenResponse = Invoke-RestMethod `
    -Method Post `
    -Uri "$($FleetAuthBaseUrl.TrimEnd('/'))/oauth2/v3/token" `
    -ContentType "application/x-www-form-urlencoded" `
    -Body @{
        grant_type    = "client_credentials"
        client_id     = $env:TESLA_CLIENT_ID
        client_secret = $env:TESLA_CLIENT_SECRET
        audience      = $FleetApiBaseUrl.TrimEnd('/')
        scope         = "openid vehicle_device_data"
    }

if ([string]::IsNullOrWhiteSpace($tokenResponse.access_token)) {
    throw "Tesla did not return a partner access token."
}

Write-Host "Registering $normalizedDomain in $FleetApiBaseUrl..."
$response = Invoke-RestMethod `
    -Method Post `
    -Uri "$($FleetApiBaseUrl.TrimEnd('/'))/api/1/partner_accounts" `
    -Headers @{ Authorization = "Bearer $($tokenResponse.access_token)" } `
    -ContentType "application/json" `
    -Body (@{ domain = $normalizedDomain } | ConvertTo-Json -Compress)

Write-Host "Tesla partner account registration succeeded."
$response | ConvertTo-Json -Depth 10
