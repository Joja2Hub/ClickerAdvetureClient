@echo off
setlocal
title RPG Quest Parent Web

cd /d "%~dp0"

where npm.cmd >nul 2>&1
if errorlevel 1 (
    echo.
    echo Node.js was not found.
    echo Install Node.js from https://nodejs.org/ and run this file again.
    echo.
    pause
    exit /b 1
)

powershell -NoProfile -Command "if (Get-NetTCPConnection -LocalPort 3000 -State Listen -ErrorAction SilentlyContinue) { exit 0 } else { exit 1 }"
if not errorlevel 1 (
    echo Web application is already running.
    start "" "http://localhost:3000"
    exit /b 0
)

if not exist "node_modules\" (
    echo.
    echo First launch: installing dependencies...
    call npm.cmd install
    if errorlevel 1 (
        echo.
        echo Dependency installation failed.
        pause
        exit /b 1
    )
)

echo.
echo Starting the parent Web application...
echo Keep this window open while using the application.
echo URL: http://localhost:3000
echo.

start "" powershell -NoProfile -WindowStyle Hidden -Command "for ($i = 0; $i -lt 60; $i++) { try { $response = Invoke-WebRequest -UseBasicParsing 'http://localhost:3000' -TimeoutSec 1; if ($response.StatusCode -ge 200) { Start-Process 'http://localhost:3000'; break } } catch {}; Start-Sleep -Milliseconds 500 }"

call npm.cmd run dev

echo.
echo Web application stopped.
pause
