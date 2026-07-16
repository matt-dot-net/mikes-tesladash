param(
    [string]$OutputPath = (Join-Path $PSScriptRoot "..\TeslaDash\data\private-key.pem")
)

$ErrorActionPreference = "Stop"
$projectPath = Join-Path $PSScriptRoot "TeslaKeygen\TeslaKeygen.csproj"
$absoluteOutputPath = [System.IO.Path]::GetFullPath($OutputPath)

& dotnet run --project $projectPath --configuration Release -- $absoluteOutputPath
if ($LASTEXITCODE -ne 0) {
    throw "Tesla key generation failed with exit code $LASTEXITCODE."
}
