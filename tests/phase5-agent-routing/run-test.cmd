@echo off
REM Phase 5 Quick Test - Launch all components
echo ========================================
echo Phase 5 Quick Test Launcher
echo ========================================
echo.
echo This script will open 3 separate windows:
echo   1. Router (port 8001, 8002, 8003)
echo   2. Chat Server (Registry mode)
echo   3. Chat Client (connecting to Router)
echo.
echo Please wait for each component to start before the next one launches...
echo.
pause

echo Starting Router...
start "Router" cmd /k "%~dp0start-router.cmd"
timeout /t 3 /nobreak >nul

echo Starting Chat Server...
start "Chat Server" cmd /k "%~dp0start-chatserver.cmd"
timeout /t 3 /nobreak >nul

echo Starting Chat Client...
start "Chat Client" cmd /k "%~dp0start-client.cmd"

echo.
echo All components launched!
echo.
echo Test Instructions:
echo   1. In Chat Client window, type: login TestUser TestPassword
echo   2. Type: say Hello from Router!
echo   3. Observe if messages work through Router
echo.
echo Press any key to exit this launcher (components will continue running)
pause >nul
