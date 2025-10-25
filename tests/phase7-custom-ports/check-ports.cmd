@echo off
REM Helper script to check Router port status
echo ========================================
echo Checking Router Port Status
echo ========================================
echo.

echo Checking default ports (8001, 8002, 8003):
netstat -an | findstr "8001 8002 8003"
echo.

echo Checking custom ports (9001, 9002, 9003):
netstat -an | findstr "9001 9002 9003"
echo.

echo ========================================
echo Note: LISTENING state means port is ready to accept connections
echo ========================================
pause
