@echo off
REM Phase 7 Test 2 - Invalid Port Number Handling (T060)
echo ========================================
echo Phase 7 Test 2: Invalid Port Numbers
echo ========================================
echo.
echo This test verifies Router properly handles invalid port numbers.
echo.

cd /d "%~dp0..\..\PinionCore.Consoles.Gateway.Router\bin\Debug\net8.0"

if not exist PinionCore.Consoles.Gateway.Router.exe (
    echo ERROR: Router executable not found
    echo Please run: dotnet build --configuration Debug
    pause
    exit /b 1
)

echo ========================================
echo Test 2A: Negative Port Number
echo ========================================
echo Testing: --agent-tcp-port=-1
echo Expected: Error message showing invalid port range
echo.
PinionCore.Consoles.Gateway.Router.exe --agent-tcp-port=-1
echo.
echo Press any key to continue to next test...
pause > nul

echo.
echo ========================================
echo Test 2B: Port Number Exceeding 65535
echo ========================================
echo Testing: --agent-tcp-port=99999
echo Expected: Error message showing port exceeds 65535
echo.
PinionCore.Consoles.Gateway.Router.exe --agent-tcp-port=99999
echo.
echo Press any key to continue to next test...
pause > nul

echo.
echo ========================================
echo Test 2C: Non-Numeric Port
echo ========================================
echo Testing: --agent-tcp-port=abc
echo Expected: Parameter ignored (defaults to 8001) OR parse error
echo.
PinionCore.Consoles.Gateway.Router.exe --agent-tcp-port=abc
echo.
echo Press any key to continue to next test...
pause > nul

echo.
echo ========================================
echo Test 2D: Port Conflict (Same Port for Multiple Services)
echo ========================================
echo Testing: --agent-tcp-port=8000 --agent-web-port=8000
echo Expected: Error message showing port conflict
echo.
PinionCore.Consoles.Gateway.Router.exe --agent-tcp-port=8000 --agent-web-port=8000
echo.
echo.
echo ========================================
echo All invalid port tests completed!
echo ========================================
pause
