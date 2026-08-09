$ErrorActionPreference = "Stop"

$PSScriptRoot = $PSScriptRoot

$UpgradeScript =
    Join-Path $PSScriptRoot "Upgrade-TreasureShell.ps1"

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

# ============================================================
# DR RECURSION LESSON: TEACH UPGRADE CLS
# ============================================================

$UpgradeScript = Join-Path $PSScriptRoot "Upgrade-TreasureShell.ps1"

if (-not (Test-Path $UpgradeScript)) {
    throw "Upgrade-TreasureShell.ps1 was not found."
}

$UpgradeCode =
    Get-Content $UpgradeScript -Raw

$UpgradeLessonMarker =
    "# DR RECURSION LESSON: CLS / CLEAR"

if ($UpgradeCode -notmatch [regex]::Escape($UpgradeLessonMarker)) {

    # This payload is the readable PowerShell lesson that will
    # be injected into Upgrade-TreasureShell.ps1.
    #
    # It teaches the upgrader how to inject the cls / clear
    # native command into MainWindow.xaml.cs.

    $UpgradePatchB64 =
"IyA9PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT0KIyBEUiBSRUNVUlNJT04gTEVTU09OOiBDTFMgLyBDTEVBUgojID09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PT09PQoKaWYgKCRDb2RlIC1ub3RtYXRjaCAiQ0xFQVIgVEVSTUlOQUwgT1VUUFVUIikgewoKJENsZWFyQ29tbWFuZCA9IEAnCiAgICAgICAgICAgIC8vIENMRUFSIFRFUk1JTkFMIE9VVFBVVAoKICAgICAgICAgICAgaWYgKAogICAgICAgICAgICAgICAgY29tbWFuZC5FcXVhbHMoCiAgICAgICAgICAgICAgICAgICAgImNscyIsCiAgICAgICAgICAgICAgICAgICAgU3RyaW5nQ29tcGFyaXNvbi5PcmRpbmFsSWdub3JlQ2FzZSkKCiAgICAgICAgICAgICAgICB8fAoKICAgICAgICAgICAgICAgIGNvbW1hbmQuRXF1YWxzKAogICAgICAgICAgICAgICAgICAgICJjbGVhciIsCiAgICAgICAgICAgICAgICAgICAgU3RyaW5nQ29tcGFyaXNvbi5PcmRpbmFsSWdub3JlQ2FzZSkpCiAgICAgICAgICAgIHsKICAgICAgICAgICAgICAgIFRlcm1pbmFsT3V0cHV0LkNsZWFyKCk7CgogICAgICAgICAgICAgICAgQ29tbWFuZElucHV0LkZvY3VzKCk7CgogICAgICAgICAgICAgICAgcmV0dXJuOwogICAgICAgICAgICB9CgonQAoKJENsZWFyQW5jaG9yID0gQCcKICAgICAgICAgICAgLy8gV0VCIE9GRgonQAoKICAgIGlmICgtbm90ICRDb2RlLkNvbnRhaW5zKCRDbGVhckFuY2hvcikpIHsKICAgICAgICB0aHJvdyAiQ291bGQgbm90IGZpbmQgdGhlIFdFQiBPRkYgYW5jaG9yIGluIE1haW5XaW5kb3cueGFtbC5jcy4iCiAgICB9CgogICAgJENvZGUgPQogICAgICAgICRDb2RlLlJlcGxhY2UoCiAgICAgICAgICAgICRDbGVhckFuY2hvciwKICAgICAgICAgICAgJENsZWFyQ29tbWFuZCArICRDbGVhckFuY2hvcgogICAgICAgICkKCiAgICBXcml0ZS1Ib3N0ICJVcGdyYWRlIGxlYXJuZWQgaG93IHRvIGFkZCBjbHMgLyBjbGVhci4iIC1Gb3JlZ3JvdW5kQ29sb3IgR3JlZW4KfQplbHNlIHsKICAgIFdyaXRlLUhvc3QgIlRyZWFzdXJlU2hlbGwgYWxyZWFkeSBrbm93cyBjbHMgLyBjbGVhci4iIC1Gb3JlZ3JvdW5kQ29sb3IgWWVsbG93Cn0KCg=="

    $UpgradeLesson =
        [System.Text.Encoding]::UTF8.GetString(
            [System.Convert]::FromBase64String(
                $UpgradePatchB64
            )
        )

    $UpgradeAnchor =
        "# NEW WEB BROWSER BLOCK"

    if (-not $UpgradeCode.Contains($UpgradeAnchor)) {
        throw "Could not find NEW WEB BROWSER BLOCK in upgrader."
    }

    $UpgradeCode =
        $UpgradeCode.Replace(
            $UpgradeAnchor,
            $UpgradeLesson +
            "
" +
            $UpgradeAnchor
        )

    Set-Content -Path $UpgradeScript -Value $UpgradeCode -Encoding utf8

    Write-Host ""
    Write-Host "Fixer taught the upgrader about cls / clear." -ForegroundColor Green
}
else {
    Write-Host ""
    Write-Host "Upgrader already contains the cls / clear lesson." -ForegroundColor Yellow
}

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








