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
if ([string]::IsNullOrWhiteSpace($env:MICROSOFT_TENANT_ID)) { throw "Set MICROSOFT_TENANT_ID in this PowerShell session." }
if ([string]::IsNullOrWhiteSpace($env:MICROSOFT_CLIENT_ID)) { throw "Set MICROSOFT_CLIENT_ID in this PowerShell session." }
if ([string]::IsNullOrWhiteSpace($env:MICROSOFT_CLIENT_SECRET)) { throw "Set MICROSOFT_CLIENT_SECRET in this PowerShell session." }
if (-not (Test-Path -LiteralPath $PrivateKeyPath)) { throw "Tesla private key not found. Run scripts/New-TeslaKeyPair.ps1 first." }

az extension add --name containerapp --upgrade --only-show-errors
az provider register --namespace Microsoft.App --wait
az provider register --namespace Microsoft.OperationalInsights --wait
az group create --name $ResourceGroup --location $Location --output none

$environmentExists = az containerapp env list --resource-group $ResourceGroup --query "[?name=='$EnvironmentName'].name | [0]" --output tsv --only-show-errors
if (-not $environmentExists) {
    az containerapp env create --name $EnvironmentName --resource-group $ResourceGroup --location $Location --output none --only-show-errors
}

$identityName = "$AppName-pull"
$identityExists = az identity list --resource-group $ResourceGroup --query "[?name=='$identityName'].id | [0]" --output tsv --only-show-errors
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
$appExists = az containerapp list --resource-group $ResourceGroup --query "[?name=='$AppName'].name | [0]" --output tsv --only-show-errors
if (-not $appExists) {
    az containerapp create --name $AppName --resource-group $ResourceGroup --environment $EnvironmentName --image $image --target-port 8080 --ingress external --min-replicas 1 --max-replicas 1 --user-assigned $identityId --registry-server $registryServer --registry-identity $identityId --secrets "tesla-client-secret=$env:TESLA_CLIENT_SECRET" "tesla-private-key=$privateKeyBase64" "microsoft-client-secret=$env:MICROSOFT_CLIENT_SECRET" --env-vars "Tesla__ClientId=$env:TESLA_CLIENT_ID" "Tesla__ClientSecret=secretref:tesla-client-secret" "Tesla__PrivateKeyBase64=secretref:tesla-private-key" "Authentication__Microsoft__TenantId=$env:MICROSOFT_TENANT_ID" "Authentication__Microsoft__ClientId=$env:MICROSOFT_CLIENT_ID" "Authentication__Microsoft__ClientSecret=secretref:microsoft-client-secret" "ASPNETCORE_ENVIRONMENT=Production" --output none --only-show-errors
}
else {
    az containerapp secret set --name $AppName --resource-group $ResourceGroup --secrets "tesla-client-secret=$env:TESLA_CLIENT_SECRET" "tesla-private-key=$privateKeyBase64" "microsoft-client-secret=$env:MICROSOFT_CLIENT_SECRET" --output none --only-show-errors
    az containerapp update --name $AppName --resource-group $ResourceGroup --image $image --set-env-vars "Tesla__ClientId=$env:TESLA_CLIENT_ID" "Tesla__ClientSecret=secretref:tesla-client-secret" "Tesla__PrivateKeyBase64=secretref:tesla-private-key" "Authentication__Microsoft__TenantId=$env:MICROSOFT_TENANT_ID" "Authentication__Microsoft__ClientId=$env:MICROSOFT_CLIENT_ID" "Authentication__Microsoft__ClientSecret=secretref:microsoft-client-secret" --output none --only-show-errors
}

$fqdn = az containerapp show --name $AppName --resource-group $ResourceGroup --query properties.configuration.ingress.fqdn --output tsv --only-show-errors
$redirectUri = "https://$fqdn/Auth/Callback"
$oidcSignInUri = "https://$fqdn/signin-oidc"
$oidcSignOutUri = "https://$fqdn/signout-callback-oidc"
az containerapp update --name $AppName --resource-group $ResourceGroup --set-env-vars "Tesla__RedirectUri=$redirectUri" --output none --only-show-errors

$microsoftClientIdSetting = az containerapp show --name $AppName --resource-group $ResourceGroup --query "properties.template.containers[0].env[?name=='Authentication__Microsoft__ClientId'].value | [0]" --output tsv --only-show-errors
if ([string]::IsNullOrWhiteSpace($microsoftClientIdSetting)) {
    throw "Deployment verification failed: Authentication__Microsoft__ClientId is missing from the Container App revision."
}

Write-Host "Deployment complete."
Write-Host "Origin:       https://$fqdn"
Write-Host "Redirect URI: $redirectUri"
Write-Host "Public key:   https://$fqdn/.well-known/appspecific/com.tesla.3p.public-key.pem"
Write-Host "OIDC sign-in: $oidcSignInUri"
Write-Host "OIDC sign-out:$oidcSignOutUri"
Write-Host "Update these URLs in the Tesla Developer Portal before connecting."
Write-Host "Add the OIDC URLs as Web redirect URIs in the Microsoft Entra app registration."
