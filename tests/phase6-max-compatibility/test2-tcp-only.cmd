@echo off
REM Phase 6 Test 2 - TCP Only Mode
echo ========================================
echo Phase 6 Test 2: TCP Only Mode
echo ========================================
echo.

cd /d "%~dp0..\..\PinionCore.Consoles.Chat1.Server\bin\Debug\net8.0"

if not exist PinionCore.Consoles.Chat1.Server.exe (
    echo ERROR: Chat Server executable not found
    echo Please run: dotnet build --configuration Debug
    pause
    exit /b 1
)

echo Starting Chat Server in TCP Only Mode...
echo Connection source:
echo   - TCP Direct: 23916
echo.
echo Expected: Only TCP listener enabled
echo.
echo ========================================
echo.

PinionCore.Consoles.Chat1.Server.exe --tcp-port=23916
