[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$')]
    [string] $Version = '0.1.0',
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'src\WinWidget\WinWidget.csproj'
$publishDir = Join-Path $repoRoot 'artifacts\publish\win-x64'
$installerDir = Join-Path $repoRoot 'artifacts\installer'
$issFile = Join-Path $repoRoot 'installer\WinWidget.iss'

Remove-Item $publishDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item $publishDir -ItemType Directory -Force | Out-Null
New-Item $installerDir -ItemType Directory -Force | Out-Null

dotnet publish $project `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained true `
    --output $publishDir `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -p:Version=$Version

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish завершился с кодом $LASTEXITCODE."
}

$isccCandidates = @(
    (Join-Path ${env:ProgramFiles(x86)} 'Inno Setup 6\ISCC.exe'),
    (Join-Path $env:ProgramFiles 'Inno Setup 6\ISCC.exe')
) | Where-Object { $_ -and (Test-Path $_) }

$iscc = $isccCandidates | Select-Object -First 1
if (-not $iscc) {
    $isccCommand = Get-Command ISCC.exe -ErrorAction SilentlyContinue
    if ($isccCommand) { $iscc = $isccCommand.Source }
}

if (-not $iscc) {
    throw 'Inno Setup 6 не найден. Установите его: winget install --id JRSoftware.InnoSetup -e'
}

& $iscc "/DAppVersion=$Version" "/DSourceDir=$publishDir" $issFile
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup завершился с кодом $LASTEXITCODE."
}

Write-Host "Установщик готов: $installerDir\WinWidget-Setup.exe"
