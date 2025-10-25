# Phase 4 Automated Test Script
# Test Registry connection, disconnect detection and reconnection

param(
    [int]$ReconnectWaitSeconds = 15
)

$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Phase 4 Automated Test: Registry Connection" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if executables exist
$routerPath = "..\..\PinionCore.Consoles.Gateway.Router\bin\Debug\net8.0"
$chatServerPath = "..\..\PinionCore.Consoles.Chat1.Server\bin\Debug\net8.0"
$routerExe = Join-Path $routerPath "PinionCore.Consoles.Gateway.Router.exe"
$chatServerExe = Join-Path $chatServerPath "PinionCore.Consoles.Chat1.Server.exe"

if (!(Test-Path $routerExe)) {
    Write-Host "[ERROR] Router executable not found" -ForegroundColor Red
    Write-Host "Please run: dotnet build" -ForegroundColor Yellow
    exit 1
}

if (!(Test-Path $chatServerExe)) {
    Write-Host "[ERROR] Chat Server executable not found" -ForegroundColor Red
    Write-Host "Please run: dotnet build" -ForegroundColor Yellow
    exit 1
}

Write-Host "[OK] Executables check passed" -ForegroundColor Green
Write-Host ""

# Clean old logs
Write-Host "[Setup] Cleaning old logs..." -ForegroundColor Yellow
Get-ChildItem $routerPath -Filter "RouterConsole_*.log" | Remove-Item -Force -ErrorAction SilentlyContinue
Write-Host "[OK] Old logs cleaned" -ForegroundColor Green
Write-Host ""

# Step 1: Start Router
Write-Host "[Step 1/6] Starting Router..." -ForegroundColor Yellow
$routerProcess = Start-Process -FilePath $routerExe -WorkingDirectory $routerPath -PassThru -WindowStyle Normal
Start-Sleep -Seconds 3

if ($routerProcess.HasExited) {
    Write-Host "[ERROR] Router failed to start" -ForegroundColor Red
    exit 1
}

Write-Host "[OK] Router started (PID: $($routerProcess.Id))" -ForegroundColor Green
Write-Host ""

# Step 2: Start Chat Server
Write-Host "[Step 2/6] Starting Chat Server (Registry mode)..." -ForegroundColor Yellow
$chatServerArgs = "--router-host=127.0.0.1 --router-port=8003 --group=1"
$chatServerProcess = Start-Process -FilePath $chatServerExe -ArgumentList $chatServerArgs -WorkingDirectory $chatServerPath -PassThru -WindowStyle Normal
Start-Sleep -Seconds 5

if ($chatServerProcess.HasExited) {
    Write-Host "[ERROR] Chat Server failed to start" -ForegroundColor Red
    Stop-Process -Id $routerProcess.Id -Force
    exit 1
}

Write-Host "[OK] Chat Server started (PID: $($chatServerProcess.Id))" -ForegroundColor Green
Write-Host ""

# Step 3: Check connection logs
Write-Host "[Step 3/6] Checking connection logs..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

$logFile = Get-ChildItem $routerPath -Filter "RouterConsole_*.log" | Select-Object -First 1
if ($logFile) {
    Write-Host "    Log file: $($logFile.Name)" -ForegroundColor Gray
    $logContent = Get-Content $logFile.FullName -Encoding UTF8
    $connectLogs = $logContent | Select-String -Pattern "Registry.*建立"
    
    if ($connectLogs) {
        Write-Host "[OK] Found connection log" -ForegroundColor Green
        Write-Host "    $($connectLogs[0])" -ForegroundColor Gray
    } else {
        Write-Host "[WARN] Connection log not found" -ForegroundColor Yellow
        Write-Host "    Showing all Registry logs:" -ForegroundColor Gray
        $logContent | Select-String -Pattern "Registry" | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
    }
} else {
    Write-Host "[WARN] Log file not found" -ForegroundColor Yellow
}
Write-Host ""

# Step 4: Simulate disconnect
Write-Host "[Step 4/6] Stopping Chat Server (simulate disconnect)..." -ForegroundColor Yellow
Stop-Process -Id $chatServerProcess.Id -Force
Start-Sleep -Seconds 3

$logContent = Get-Content $logFile.FullName -Encoding UTF8
$disconnectLogs = $logContent | Select-String -Pattern "Registry.*中斷"

if ($disconnectLogs) {
    Write-Host "[OK] Found disconnect log" -ForegroundColor Green
    Write-Host "    $($disconnectLogs[-1])" -ForegroundColor Gray
} else {
    Write-Host "[WARN] Disconnect log not found" -ForegroundColor Yellow
}
Write-Host ""

# Step 5: Restart Chat Server (test reconnection)
Write-Host "[Step 5/6] Restarting Chat Server (test reconnection)..." -ForegroundColor Yellow
Write-Host "    Waiting $ReconnectWaitSeconds seconds..." -ForegroundColor Cyan

$chatServerProcess = Start-Process -FilePath $chatServerExe -ArgumentList $chatServerArgs -WorkingDirectory $chatServerPath -PassThru -WindowStyle Normal
Start-Sleep -Seconds $ReconnectWaitSeconds

if ($chatServerProcess.HasExited) {
    Write-Host "[WARN] Chat Server has exited" -ForegroundColor Yellow
} else {
    Write-Host "[OK] Chat Server running (PID: $($chatServerProcess.Id))" -ForegroundColor Green
}
Write-Host ""

# Step 6: Check reconnection logs
Write-Host "[Step 6/6] Checking reconnection logs..." -ForegroundColor Yellow

$logContent = Get-Content $logFile.FullName -Encoding UTF8
$connectEvents = $logContent | Select-String -Pattern "Registry.*建立"
$connectCount = ($connectEvents | Measure-Object).Count

if ($connectCount -ge 2) {
    Write-Host "[OK] Found $connectCount connection events (reconnection success)" -ForegroundColor Green
    Write-Host ""
    Write-Host "Connection events:" -ForegroundColor Cyan
    $connectEvents | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
} else {
    Write-Host "[WARN] Only found $connectCount connection events" -ForegroundColor Yellow
}
Write-Host ""

# Display log summary
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Log Summary (Registry events)" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
$logContent | Select-String -Pattern "Registry" | ForEach-Object {
    Write-Host $_
}
Write-Host ""

# Cleanup
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Completed" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Processes still running:" -ForegroundColor Yellow
Write-Host "  Router PID: $($routerProcess.Id)"
Write-Host "  Chat Server PID: $($chatServerProcess.Id)"
Write-Host ""
Write-Host "View full log:" -ForegroundColor Cyan
Write-Host "  type $($logFile.FullName)"
Write-Host ""
Write-Host "Stop processes:" -ForegroundColor Cyan
Write-Host "  taskkill /PID $($routerProcess.Id) /F"
Write-Host "  taskkill /PID $($chatServerProcess.Id) /F"
Write-Host ""

Read-Host "Press Enter to stop all test processes"

Stop-Process -Id $routerProcess.Id -Force -ErrorAction SilentlyContinue
Stop-Process -Id $chatServerProcess.Id -Force -ErrorAction SilentlyContinue

Write-Host "[OK] All test processes stopped" -ForegroundColor Green
