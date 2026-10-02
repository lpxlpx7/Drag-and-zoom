param(
    [Parameter(Mandatory = $true)]
    [string]$VatSysExe,

    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $VatSysExe -PathType Leaf)) {
    throw "vatSys.exe was not found: $VatSysExe"
}

$project = Join-Path $PSScriptRoot 'PersistentPan\PersistentPan.csproj'
$output = Join-Path $PSScriptRoot 'PersistentPan\bin\Release\vatSys.PersistentPan.dll'

dotnet msbuild $project /p:Configuration=$Configuration /p:Platform=x86 /p:VatSysPath=$VatSysExe /v:minimal

Write-Host "Built plugin for: $VatSysExe"
Write-Host "Output: $output"
