@echo off
echo ========================================
echo Checking Router Log Content
echo ========================================
echo.

cd /d "%~dp0..\..\PinionCore.Consoles.Gateway.Router\bin\Debug\net8.0"

echo Finding latest log file...
for /f "delims=" %%i in ('dir /b /od RouterConsole_*.log 2^>nul') do set LOGFILE=%%i

if not defined LOGFILE (
    echo ERROR: No log file found
    pause
    exit /b 1
)

echo Latest log: %LOGFILE%
echo.
echo ========================================
echo Full Log Content:
echo ========================================
type "%LOGFILE%"
echo.
echo ========================================
echo Registry Related Lines:
echo ========================================
findstr /i "Registry" "%LOGFILE%"
echo.
pause
