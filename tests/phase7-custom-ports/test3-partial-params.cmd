@echo off
REM Phase 7 Test 3 - Partial Parameter Specification (T061)
echo ========================================
echo Phase 7 Test 3: Partial Parameters
echo ========================================
echo.
echo This test verifies Router uses default values for unspecified parameters.
echo.

cd /d "%~dp0..\..\PinionCore.Consoles.Gateway.Router\bin\Debug\net8.0"

if not exist PinionCore.Consoles.Gateway.Router.exe (
    echo ERROR: Router executable not found
    echo Please run: dotnet build --configuration Debug
    pause
    exit /b 1
)

echo ========================================
echo Test 3A: Only Agent TCP Port Specified
echo ========================================
echo Testing: --agent-tcp-port=9001
echo Expected: Agent TCP=9001, Agent WebSocket=8002 (default), Registry TCP=8003 (default)
echo.
PinionCore.Consoles.Gateway.Router.exe --agent-tcp-port=9001
echo.
echo Press Ctrl+C to stop, then any key to continue...
pause > nul

echo.
echo ========================================
echo Test 3B: Only Agent WebSocket Port Specified
echo ========================================
echo Testing: --agent-web-port=9002
echo Expected: Agent TCP=8001 (default), Agent WebSocket=9002, Registry TCP=8003 (default)
echo.
PinionCore.Consoles.Gateway.Router.exe --agent-web-port=9002
echo.
echo Press Ctrl+C to stop, then any key to continue...
pause > nul

echo.
echo ========================================
echo Test 3C: Only Registry TCP Port Specified
echo ========================================
echo Testing: --registry-tcp-port=9003
echo Expected: Agent TCP=8001 (default), Agent WebSocket=8002 (default), Registry TCP=9003
echo.
PinionCore.Consoles.Gateway.Router.exe --registry-tcp-port=9003
echo.
echo Press Ctrl+C to stop, then any key to continue...
pause > nul

echo.
echo ========================================
echo Test 3D: Two Parameters Specified
echo ========================================
echo Testing: --agent-tcp-port=9001 --registry-tcp-port=9003
echo Expected: Agent TCP=9001, Agent WebSocket=8002 (default), Registry TCP=9003
echo.
PinionCore.Consoles.Gateway.Router.exe --agent-tcp-port=9001 --registry-tcp-port=9003
echo.
echo.
echo ========================================
echo All partial parameter tests completed!
echo ========================================
pause
