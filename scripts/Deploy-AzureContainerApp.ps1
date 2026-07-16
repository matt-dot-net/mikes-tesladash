param(
    [Parameter(Mandatory)] [string]$ResourceGroup,
    [Parameter(Mandatory)] [string]$AcrName,
    [string]$Location = "eastus",
    [string]$AppName = "tesladash",
    [string]$EnvironmentName = "tesladash-env",
    [string]$ImageTag = "latest",
    [string]$PrivateKeyPath = (Join-Path $PSScriptRoot "..\TeslaDash\data\private-key.pem")
)

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($env:TESLA_CLIENT_ID)) { throw "Set TESLA_CLIENT_ID in this PowerShell session." }
if ([string]::IsNullOrWhiteSpace($env:TESLA_CLIENT_SECRET)) { throw "Set TESLA_CLIENT_SECRET in this PowerShell session." }
if (-not (Test-Path -LiteralPath $PrivateKeyPath)) { throw "Tesla private key not found. Run scripts/New-TeslaKeyPair.ps1 first." }

az extension add --name containerapp --upgrade --only-show-errors
az provider register --namespace Microsoft.App --wait
az provider register --namespace Microsoft.OperationalInsights --wait
az group create --name $ResourceGroup --location $Location --output none

$environmentExists = az containerapp env show --name $EnvironmentName --resource-group $ResourceGroup --query name --output tsv 2>$null
if (-not $environmentExists) {
    az containerapp env create --name $EnvironmentName --resource-group $ResourceGroup --location $Location --output none
}

$identityName = "$AppName-pull"
$identityExists = az identity show --name $identityName --resource-group $ResourceGroup --query id --output tsv 2>$null
if (-not $identityExists) {
    az identity create --name $identityName --resource-group $ResourceGroup --location $Location --output none
}
$identityId = az identity show --name $identityName --resource-group $ResourceGroup --query id --output tsv
$principalId = az identity show --name $identityName --resource-group $ResourceGroup --query principalId --output tsv
$acrId = az acr show --name $AcrName --query id --output tsv
$registryServer = az acr show --name $AcrName --query loginServer --output tsv

$roleExists = az role assignment list --assignee $principalId --scope $acrId --role AcrPull --query "[0].id" --output tsv
if (-not $roleExists) {
    az role assignment create --assignee-object-id $principalId --assignee-principal-type ServicePrincipal --role AcrPull --scope $acrId --output none
}

$image = "$registryServer/tesladash:$ImageTag"
az acr build --registry $AcrName --image "tesladash:$ImageTag" --file Dockerfile .

$privateKeyBase64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes([IO.File]::ReadAllText((Resolve-Path $PrivateKeyPath))))
$appExists = az containerapp show --name $AppName --resource-group $ResourceGroup --query name --output tsv 2>$null
if (-not $appExists) {
    az containerapp create --name $AppName --resource-group $ResourceGroup --environment $EnvironmentName --image $image --target-port 8080 --ingress external --min-replicas 1 --max-replicas 1 --user-assigned $identityId --registry-server $registryServer --registry-identity $identityId --secrets "tesla-client-secret=$env:TESLA_CLIENT_SECRET" "tesla-private-key=$privateKeyBase64" --env-vars "Tesla__ClientId=$env:TESLA_CLIENT_ID" "Tesla__ClientSecret=secretref:tesla-client-secret" "Tesla__PrivateKeyBase64=secretref:tesla-private-key" "ASPNETCORE_ENVIRONMENT=Production" --output none
}
else {
    az containerapp secret set --name $AppName --resource-group $ResourceGroup --secrets "tesla-client-secret=$env:TESLA_CLIENT_SECRET" "tesla-private-key=$privateKeyBase64" --output none
    az containerapp update --name $AppName --resource-group $ResourceGroup --image $image --set-env-vars "Tesla__ClientId=$env:TESLA_CLIENT_ID" "Tesla__ClientSecret=secretref:tesla-client-secret" "Tesla__PrivateKeyBase64=secretref:tesla-private-key" --output none
}

$fqdn = az containerapp show --name $AppName --resource-group $ResourceGroup --query properties.configuration.ingress.fqdn --output tsv
$redirectUri = "https://$fqdn/Auth/Callback"
az containerapp update --name $AppName --resource-group $ResourceGroup --set-env-vars "Tesla__RedirectUri=$redirectUri" --output none

Write-Host "Deployment complete."
Write-Host "Origin:       https://$fqdn"
Write-Host "Redirect URI: $redirectUri"
Write-Host "Public key:   https://$fqdn/.well-known/appspecific/com.tesla.3p.public-key.pem"
Write-Host "Update these URLs in the Tesla Developer Portal before connecting."
