@echo off
REM Phase 4 Test - Start Chat Server (Registry Mode)
echo ========================================
echo Phase 4 Test: Starting Chat Server (Registry Mode)
echo ========================================
echo.

cd /d "%~dp0..\..\PinionCore.Consoles.Chat1.Server\bin\Debug\net8.0"

if not exist PinionCore.Consoles.Chat1.Server.exe (
    echo ERROR: Chat Server executable not found
    echo Please run: dotnet build PinionCore.Consoles.Chat1.Server\PinionCore.Consoles.Chat1.Server.csproj
    pause
    exit /b 1
)

echo Starting Chat Server (Registry Mode)...
echo Connection configuration:
echo   - Router Host: 127.0.0.1
echo   - Router Port: 8003
echo   - Group ID: 1
echo.
echo In this mode Chat Server will:
echo   1. Connect to Router's Registry endpoint
echo   2. Register as available service
echo   3. Accept clients routed from Router
echo.
echo Press Ctrl+C to test disconnect and reconnect
echo ========================================
echo.

PinionCore.Consoles.Chat1.Server.exe --router-host=127.0.0.1 --router-port=8003 --group=1
