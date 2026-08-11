

$ErrorActionPreference = "Stop"

Clear-Host

Write-Host ""
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host " TreasureShell Prerequisite Checkpoint" -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Windows may warn that this PowerShell script is unsigned." -ForegroundColor Yellow
Write-Host "That's expected for this GitHub build." -ForegroundColor Yellow
Write-Host ""

$Root = Split-Path $PSScriptRoot -Parent

Write-Host "Installer folder:" -ForegroundColor DarkGray
Write-Host "  $PSScriptRoot"
Write-Host ""

Write-Host "TreasureShell actually starts one level higher:" -ForegroundColor Cyan
Write-Host "  $Root"
Write-Host ""

Write-Host "But I got you, no worries my dude." -ForegroundColor Green
Write-Host "Press Enter and watch it spiral. 🌀" -ForegroundColor Magenta
Write-Host ""

Read-Host | Out-Null

Set-Location $Root

$Bootstrap = Join-Path $Root "Bootstrap-TreasureShell.ps1"

if (-not (Test-Path $Bootstrap)) {
    throw "Bootstrap-TreasureShell.ps1 was not found in the repository root."
}

powershell.exe `
    -NoProfile `
    -ExecutionPolicy Bypass `
    -File $Bootstrap

if ($LASTEXITCODE -ne 0) {
    throw "TreasureShell bootstrap failed."
}


