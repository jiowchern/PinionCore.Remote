@echo off
REM Phase 7 Test 1 - Custom Port Configuration (T059)
echo ========================================
echo Phase 7 Test 1: Custom Port Configuration
echo ========================================
echo.
echo This test verifies Router can start with custom ports:
echo   - Agent TCP Port: 9001
echo   - Agent WebSocket Port: 9002
echo   - Registry TCP Port: 9003
echo.

cd /d "%~dp0..\..\PinionCore.Consoles.Gateway.Router\bin\Debug\net8.0"

if not exist PinionCore.Consoles.Gateway.Router.exe (
    echo ERROR: Router executable not found
    echo Please run: dotnet build --configuration Debug
    pause
    exit /b 1
)

echo Starting Router with custom ports...
echo.
echo Expected behavior:
echo   1. Router should display: "Router 配置: Agent TCP=9001, Agent WebSocket=9002, Registry TCP=9003"
echo   2. Router should display: "Agent TCP 監聽已啟動，端口: 9001"
echo   3. Router should display: "Agent WebSocket 監聽已啟動，端口: 9002"
echo   4. Router should display: "Router Console 啟動完成，所有監聽器已就緒"
echo.
echo After Router starts, run "netstat -an | findstr "9001 9002 9003"" in another terminal
echo to verify ports are in LISTENING state.
echo.
echo Press Ctrl+C to stop Router when done testing.
echo ========================================
echo.

PinionCore.Consoles.Gateway.Router.exe --agent-tcp-port=9001 --agent-web-port=9002 --registry-tcp-port=9003
