$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot

Write-Host ""
Write-Host "==============================================" -ForegroundColor Magenta
Write-Host "   DR. RECURSION: BOOTSTRAP LESSON v2" -ForegroundColor Magenta
Write-Host "==============================================" -ForegroundColor Magenta
Write-Host ""

$SetupPath = Join-Path $Root "Setup-TreasureShell.ps1"
$ReadmePath = Join-Path $Root "README.md"

$Setup = @'
$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot

Write-Host ""
Write-Host "TreasureShell Development Setup" -ForegroundColor Yellow
Write-Host "===============================" -ForegroundColor Yellow
Write-Host ""

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    Write-Host "Installing Git..." -ForegroundColor Cyan
    winget install --id Git.Git -e --accept-source-agreements --accept-package-agreements
}
else {
    Write-Host "Git found." -ForegroundColor Green
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "Installing .NET 10 SDK..." -ForegroundColor Cyan
    winget install --id Microsoft.DotNet.SDK.10 -e --accept-source-agreements --accept-package-agreements
}
else {
    Write-Host ".NET SDK found." -ForegroundColor Green
    dotnet --version
}

Write-Host ""
Write-Host "Restoring..." -ForegroundColor Cyan
dotnet restore
if ($LASTEXITCODE -ne 0) { throw "dotnet restore failed." }

Write-Host ""
Write-Host "Building..." -ForegroundColor Cyan
dotnet build
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed." }

Write-Host ""
Write-Host "Publishing..." -ForegroundColor Cyan
dotnet publish -c Release
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

Write-Host ""
Write-Host "TreasureShell ready." -ForegroundColor Green
Write-Host "$Root\bin\Release\net10.0-windows\win-x64\publish\TreasureShell.exe"
'@

Set-Content -Path $SetupPath -Value $Setup -Encoding UTF8
Write-Host "Created Setup-TreasureShell.ps1" -ForegroundColor Green

$Lesson = @'

---

## Running TreasureShell

TreasureShell is published as a self-contained, single-file Windows x64 application.

The published application includes the required .NET runtime, so a separate .NET runtime installation is not required to run TreasureShell.

## Development Setup

Development uses:

- Windows x64
- PowerShell
- Git
- .NET 10 SDK
- HtmlAgilityPack via NuGet

Run:

    .\Setup-TreasureShell.ps1

The setup path is:

    dependency check
          |
          v
       restore
          |
          v
        build
          |
          v
       publish

Published application:

    bin\Release\net10.0-windows\win-x64\publish\TreasureShell.exe

### Dr. Recursion

    Teacher
       |
       v
    README + Setup
       |
       v
    Restore
       |
       v
    Build
       |
       v
    Publish

Don't just draw the fractal. Ride the spiral.
'@

$Readme = Get-Content -Path $ReadmePath -Raw

if ($Readme -notmatch "## Development Setup") {
    Add-Content -Path $ReadmePath -Value $Lesson -Encoding UTF8
    Write-Host "README learned the bootstrap lesson." -ForegroundColor Green
}
else {
    Write-Host "README already contains bootstrap documentation." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Launching Setup-TreasureShell.ps1..." -ForegroundColor Cyan
Write-Host ""

& $SetupPath

if ($LASTEXITCODE -ne 0) {
    throw "Setup-TreasureShell.ps1 failed."
}

Write-Host ""
Write-Host "==============================================" -ForegroundColor Green
Write-Host "   DR. RECURSION BOOTSTRAP COMPLETE" -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Green
