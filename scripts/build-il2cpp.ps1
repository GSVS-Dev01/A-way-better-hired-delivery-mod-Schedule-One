[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $MelonLoaderRoot,

    [string] $DotNet = "dotnet"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot

& (Join-Path $PSScriptRoot "verify-game-api.ps1") -MelonLoaderRoot $MelonLoaderRoot
if ($LASTEXITCODE) { exit $LASTEXITCODE }

& $DotNet build (Join-Path $repositoryRoot "VehicleHandlers.Il2Cpp.csproj") `
    --configuration Release `
    --property:MelonLoaderRoot="$MelonLoaderRoot" `
    --nologo
if ($LASTEXITCODE) { exit $LASTEXITCODE }

$outputPath = Join-Path $repositoryRoot "bin\Il2Cpp\VehicleHandlers.dll"
if (-not (Test-Path -LiteralPath $outputPath -PathType Leaf)) {
    throw "Build completed without the expected output: $outputPath"
}

Write-Output "Built $outputPath"

