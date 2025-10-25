@echo off
REM Phase 4 Test - Check Logs
echo ========================================
echo Phase 4 Test: Checking Router Logs
echo ========================================
echo.

cd /d "%~dp0..\..\PinionCore.Consoles.Gateway.Router\bin\Debug\net8.0"

echo Searching for Registry related logs...
echo.
echo ======== Router Logs (Registry Events) ========
findstr /i "Registry" RouterConsole_*.log

if errorlevel 1 (
    echo.
    echo WARNING: No log files found or no Registry records
    echo Please verify:
    echo   1. Router is started
    echo   2. Chat Server is connected
    echo   3. Log files exist
) else (
    echo.
    echo ========================================
    echo.
    echo Expected events:
    echo   - Registry TCP listening started
    echo   - Registry listener successfully bound
    echo   - Registry connection established (current connections: X)
    echo   - Registry connection lost (current connections: X)  [if tested disconnect]
)

echo.
echo View full log:
echo   type RouterConsole_*.log
echo.
pause
