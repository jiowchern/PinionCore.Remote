@echo off
REM Phase 5 Test - Start Chat Server (Registry Mode)
echo ========================================
echo Phase 5 Test: Starting Chat Server (Registry Mode)
echo ========================================
echo.

cd /d "%~dp0..\..\PinionCore.Consoles.Chat1.Server\bin\Debug\net8.0"

if not exist PinionCore.Consoles.Chat1.Server.exe (
    echo ERROR: Chat Server executable not found
    echo Please run: dotnet build --configuration Debug
    pause
    exit /b 1
)

echo Starting Chat Server in Registry Mode...
echo Connection configuration:
echo   - Router Host: 127.0.0.1
echo   - Router Port: 8003
echo   - Group ID: 1
echo.
echo This server will register with Router and accept routed clients
echo Press Ctrl+C to stop
echo ========================================
echo.

PinionCore.Consoles.Chat1.Server.exe --router-host=127.0.0.1 --router-port=8003 --group=1
