@echo off
REM Phase 6 Test 1 - Maximum Compatibility Mode
echo ========================================
echo Phase 6 Test 1: Maximum Compatibility Mode
echo ========================================
echo.

cd /d "%~dp0..\..\PinionCore.Consoles.Chat1.Server\bin\Debug\net8.0"

if not exist PinionCore.Consoles.Chat1.Server.exe (
    echo ERROR: Chat Server executable not found
    echo Please run: dotnet build --configuration Debug
    pause
    exit /b 1
)

echo Starting Chat Server in Maximum Compatibility Mode...
echo Connection sources:
echo   - TCP Direct: 23916
echo   - WebSocket Direct: 23917
echo   - Gateway Router: 127.0.0.1:8003 (Group 1)
echo.
echo Expected: Server accepts connections from all 3 sources
echo.
echo ========================================
echo.

PinionCore.Consoles.Chat1.Server.exe --tcp-port=23916 --web-port=23917 --router-host=127.0.0.1 --router-port=8003 --group=1
