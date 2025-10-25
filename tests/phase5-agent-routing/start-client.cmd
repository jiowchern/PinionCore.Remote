@echo off
REM Phase 5 Test - Start Chat Client (Connect to Router)
echo ========================================
echo Phase 5 Test: Starting Chat Client
echo ========================================
echo.

cd /d "%~dp0..\..\PinionCore.Consoles.Chat1.Client\bin\Debug\net8.0"

if not exist PinionCore.Consoles.Chat1.Client.exe (
    echo ERROR: Chat Client executable not found
    echo Please run: dotnet build --configuration Debug
    pause
    exit /b 1
)

echo Starting Chat Client...
echo Connection target:
echo   - Router Host: localhost
echo   - Router Port: 8001 (Agent TCP)
echo.
echo After connection:
echo   1. Type: login YourName YourPassword
echo   2. Type: say Hello World
echo   3. Type: quit to exit
echo.
echo ========================================
echo.

PinionCore.Consoles.Chat1.Client.exe --router-host=127.0.0.1 --router-port=8001
