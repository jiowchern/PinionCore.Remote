@echo off
REM Quick run Phase 4 test

echo ========================================
echo Phase 4 Quick Test
echo ========================================
echo.

REM Check if executables need compilation
echo [1/3] Checking executables...
if not exist "..\..\PinionCore.Consoles.Gateway.Router\bin\Debug\net8.0\PinionCore.Consoles.Gateway.Router.exe" (
    echo Router executable not found, compiling...
    pushd "%~dp0..\.."
    dotnet build PinionCore.Consoles.Gateway.Router\PinionCore.Consoles.Gateway.Router.csproj --no-restore
    popd
)

if not exist "..\..\PinionCore.Consoles.Chat1.Server\bin\Debug\net8.0\PinionCore.Consoles.Chat1.Server.exe" (
    echo Chat Server executable not found, compiling...
    pushd "%~dp0..\.."
    dotnet build PinionCore.Consoles.Chat1.Server\PinionCore.Consoles.Chat1.Server.csproj --no-restore
    popd
)

echo.
echo [2/3] Running automated test...
echo.

REM Run PowerShell test script
powershell -ExecutionPolicy Bypass -File "%~dp0test-phase4-auto.ps1"

echo.
echo [3/3] Test completed
echo.
pause
