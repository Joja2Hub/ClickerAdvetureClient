@echo off
setlocal
title RPG Quest Firebase Deploy
cd /d "%~dp0"

where firebase.cmd >nul 2>&1
if errorlevel 1 (
    echo Firebase CLI was not found.
    echo Installing Firebase CLI...
    call npm.cmd install -g firebase-tools
    if errorlevel 1 (
        echo Firebase CLI installation failed.
        pause
        exit /b 1
    )
)

echo.
echo Deploying Firestore rules and indexes...
echo Firebase may ask you to sign in.
echo.
call firebase.cmd deploy --only firestore:rules,firestore:indexes

if errorlevel 1 (
    echo.
    echo Deployment failed. Review the error above.
    pause
    exit /b 1
)

echo.
echo Firebase deployment completed.
echo Analytics uses the browser fallback on the free Firebase Spark plan.
echo Cloud Functions can be deployed separately after upgrading to Blaze.
pause
