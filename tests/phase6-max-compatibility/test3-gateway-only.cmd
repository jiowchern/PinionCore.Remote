@echo off
REM Phase 6 Test 3 - Gateway Only Mode
echo ========================================
echo Phase 6 Test 3: Gateway Only Mode
echo ========================================
echo.

cd /d "%~dp0..\..\PinionCore.Consoles.Chat1.Server\bin\Debug\net8.0"

if not exist PinionCore.Consoles.Chat1.Server.exe (
    echo ERROR: Chat Server executable not found
    echo Please run: dotnet build --configuration Debug
    pause
    exit /b 1
)

echo Starting Chat Server in Gateway Only Mode...
echo Connection source:
echo   - Gateway Router: 127.0.0.1:8003 (Group 1)
echo.
echo Expected: Only Gateway mode enabled, no direct TCP/WebSocket
echo.
echo ========================================
echo.

PinionCore.Consoles.Chat1.Server.exe --router-host=127.0.0.1 --router-port=8003 --group=1
