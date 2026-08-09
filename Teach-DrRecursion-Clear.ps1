$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "==============================================" -ForegroundColor Magenta
Write-Host "      DR. RECURSION: CLEAR LESSON v2" -ForegroundColor Magenta
Write-Host "==============================================" -ForegroundColor Magenta
Write-Host ""
Write-Host "Teacher -> Fixer -> Upgrader -> C# -> Build -> Git" -ForegroundColor DarkMagenta
Write-Host ""

$fixer = Join-Path $PSScriptRoot "Fix-UpgradeTreasureShell.ps1"

if (!(Test-Path $fixer)) {
    throw "Fixer not found: $fixer"
}

$stamp  = Get-Date -Format "yyyyMMdd-HHmmss"
$backup = "$fixer.$stamp.bak"
Copy-Item $fixer $backup

Write-Host "Fixer backup created:" -ForegroundColor Green
Write-Host $backup

$content = Get-Content $fixer -Raw

# Repair the bad lesson from the previous recursion.
$content = $content -replace '\$ProjectRoot', '$PSScriptRoot'

Set-Content -Path $fixer -Value $content -Encoding UTF8

Write-Host ""
Write-Host "Teacher repaired recursive path scope." -ForegroundColor Green
Write-Host 'Fixer now resolves paths from $PSScriptRoot.' -ForegroundColor Green
Write-Host ""
Write-Host "Launching newly educated fixer..." -ForegroundColor Cyan
Write-Host ""

& powershell.exe -NoProfile -ExecutionPolicy Bypass -File $fixer

$exitCode = $LASTEXITCODE

if ($exitCode -ne 0) {
    Write-Host ""
    Write-Host "Recursive chain stopped." -ForegroundColor Red
    Write-Host "Fixer exit code: $exitCode" -ForegroundColor Red
    exit $exitCode
}

Write-Host ""
Write-Host "==============================================" -ForegroundColor Green
Write-Host "        DR. RECURSION SURVIVED v2" -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Green
