@echo off
REM Phase 8 Test - Start Chat Client (WebSocket to Router)
echo ========================================
echo Phase 8 Test: WebSocket Gateway Client
echo ========================================
echo.

cd /d "%~dp0..\..\PinionCore.Consoles.Chat1.Client\bin\Debug\net8.0"

if not exist PinionCore.Consoles.Chat1.Client.exe (
    echo ERROR: Chat Client executable not found
    echo Please run: dotnet build --configuration Debug
    pause
    exit /b 1
)

echo Starting Chat Client with WebSocket...
echo Connection target:
echo   - Router Host: localhost
echo   - Router Port: 8002 (Agent WebSocket)
echo   - Protocol: WebSocket
echo.
echo After connection:
echo   1. Type: login YourName YourPassword
echo   2. Type: say Hello from WebSocket!
echo   3. Type: quit to exit
echo.
echo ========================================
echo.

PinionCore.Consoles.Chat1.Client.exe --router-host=localhost --router-port=8002 --websocket
