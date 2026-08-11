@echo off
title TreasureShell Prerequisite Checkpoint
cd /d "%~dp0"

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Prerequisite-Checkpoint.ps1"

echo.
if errorlevel 1 (
    echo ==============================================
    echo  TreasureShell encountered a problem.
    echo ==============================================
    echo.
    echo Keep this window open and send us a picture
    echo of the error above.
) else (
    echo ==============================================
    echo  TreasureShell finished successfully.
    echo ==============================================
)

echo.
pause
