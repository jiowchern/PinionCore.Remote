@echo off
REM Phase 5 Test - Start Router
echo ========================================
echo Phase 5 Test: Starting Router
echo ========================================
echo.

cd /d "%~dp0..\..\PinionCore.Consoles.Gateway.Router\bin\Debug\net8.0"

if not exist PinionCore.Consoles.Gateway.Router.exe (
    echo ERROR: Router executable not found
    echo Please run: dotnet build --configuration Debug
    pause
    exit /b 1
)

echo Starting Router...
echo Listening ports:
echo   - Agent TCP: 8001
echo   - Agent WebSocket: 8002
echo   - Registry TCP: 8003
echo.
echo Press Ctrl+C to stop
echo ========================================
echo.

PinionCore.Consoles.Gateway.Router.exe
