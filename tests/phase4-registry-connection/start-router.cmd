@echo off
REM Phase 4 Test - Start Router
echo ========================================
echo Phase 4 Test: Starting Gateway Router
echo ========================================
echo.

cd /d "%~dp0..\..\PinionCore.Consoles.Gateway.Router\bin\Debug\net8.0"

if not exist PinionCore.Consoles.Gateway.Router.exe (
    echo ERROR: Router executable not found
    echo Please run: dotnet build PinionCore.Consoles.Gateway.Router\PinionCore.Consoles.Gateway.Router.csproj
    pause
    exit /b 1
)

echo Starting Router...
echo Port configuration:
echo   - Agent TCP: 8001
echo   - Agent WebSocket: 8002
echo   - Registry TCP: 8003
echo.
echo Press Ctrl+C to stop Router
echo ========================================
echo.

PinionCore.Consoles.Gateway.Router.exe
