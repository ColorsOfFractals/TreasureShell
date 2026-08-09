$ErrorActionPreference = "Stop"

$ProjectRoot = $PSScriptRoot

$UpgradeScript =
    Join-Path $ProjectRoot "Upgrade-TreasureShell.ps1"

Write-Host ""
Write-Host "Dr. Recursion - TreasureShell Upgrade Patcher" -ForegroundColor Magenta
Write-Host "============================================" -ForegroundColor Magenta
Write-Host ""

# ============================================================
# VERIFY TARGET SCRIPT
# ============================================================

if (-not (Test-Path $UpgradeScript)) {
    throw "Upgrade-TreasureShell.ps1 was not found."
}

# ============================================================
# BACK UP THE UPGRADE SCRIPT ITSELF
# ============================================================

$Timestamp =
    Get-Date -Format "yyyyMMdd-HHmmss"

$Backup =
    "$UpgradeScript.$Timestamp.bak"

Copy-Item `
    -Path $UpgradeScript `
    -Destination $Backup

Write-Host "Upgrade script backup created:" -ForegroundColor Green
Write-Host $Backup
Write-Host ""

# ============================================================
# LOAD THE UPGRADE SCRIPT
# ============================================================

$Code =
    Get-Content `
        -Path $UpgradeScript `
        -Raw

# ============================================================
# PATCH THE HTMLDOCUMENT COLLISION
# ============================================================

$Old =
    "new HtmlDocument();"

$New =
    "new HtmlAgilityPack.HtmlDocument();"

if ($Code.Contains($New)) {

    Write-Host "HtmlDocument fix already present." -ForegroundColor Yellow

}
elseif ($Code.Contains($Old)) {

    $Code =
        $Code.Replace(
            $Old,
            $New
        )

    Set-Content `
        -Path $UpgradeScript `
        -Value $Code `
        -Encoding utf8

    Write-Host "Patched HtmlDocument namespace collision." -ForegroundColor Green

}
else {

    throw "Could not find the HtmlDocument line to patch."
}

# ============================================================
# RUN THE NEWLY PATCHED UPGRADE SCRIPT
# ============================================================

Write-Host ""
Write-Host "Launching patched TreasureShell upgrader..." -ForegroundColor Cyan
Write-Host ""

$PowerShellExe =
    (Get-Process -Id $PID).Path

& $PowerShellExe `
    -NoProfile `
    -ExecutionPolicy Bypass `
    -File $UpgradeScript

$UpgradeExitCode =
    $LASTEXITCODE

Write-Host ""

if ($UpgradeExitCode -ne 0) {

    Write-Host "Recursive upgrade failed." -ForegroundColor Red
    Write-Host "Upgrade script backup:" -ForegroundColor Yellow
    Write-Host $Backup

    exit $UpgradeExitCode
}

Write-Host "============================================" -ForegroundColor Green
Write-Host " DR. RECURSION REPORTS SUCCESS" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Green
Write-Host ""
Write-Host "The patcher patched the upgrader." -ForegroundColor Yellow
Write-Host "The upgrader patched TreasureShell." -ForegroundColor Yellow
Write-Host "TreasureShell built successfully." -ForegroundColor Yellow
Write-Host ""
Write-Host "Next:" -ForegroundColor Cyan
Write-Host "  dotnet run"
Write-Host ""
Write-Host "Then try:"
Write-Host "  web github.com"
Write-Host ""