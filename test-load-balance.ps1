# Round-Robin Load Balance Test
# Test Goal: Verify Router distributes 10 Agents evenly across 2 Registries (expected 5:5, tolerance ±1)

param(
    [int]$AgentCount = 10,
    [int]$RegistryCount = 2,
    [int]$TestDurationSeconds = 10
)

$ErrorActionPreference = "Stop"

# Path Configuration
$RouterExe = "D:\develop\PinionCore.Remote\PinionCore.Consoles.Gateway.Router\bin\Debug\net8.0\PinionCore.Consoles.Gateway.Router.exe"
$ServerExe = "D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Server\bin\Debug\net8.0\PinionCore.Consoles.Chat1.Server.exe"
$ClientExe = "D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Client\bin\Debug\net8.0\PinionCore.Consoles.Chat1.Client.exe"

# Port Configuration
$RouterAgentTcpPort = 9001
$RouterRegistryPort = 9003

# Log Directory
$LogDir = "D:\develop\PinionCore.Remote\test-logs"
if (!(Test-Path $LogDir)) {
    New-Item -ItemType Directory -Path $LogDir | Out-Null
}

# Clean old logs
Get-ChildItem $LogDir -Filter "*.log" | Remove-Item -Force

Write-Host "========================================"  -ForegroundColor Cyan
Write-Host "Round-Robin Load Balance Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Agent Count: $AgentCount" -ForegroundColor Yellow
Write-Host "Registry Count: $RegistryCount" -ForegroundColor Yellow
Write-Host "Test Duration: $TestDurationSeconds seconds" -ForegroundColor Yellow
Write-Host ""

# Process Tracking
$processes = @()

try {
    # Step 1: Start Router
    Write-Host "[1/4] Starting Router..." -ForegroundColor Green
    $routerLogFile = Join-Path $LogDir "router.log"
    $routerProcess = Start-Process -FilePath $RouterExe `
        -ArgumentList "--agent-tcp-port=$RouterAgentTcpPort", "--registry-tcp-port=$RouterRegistryPort" `
        -RedirectStandardOutput $routerLogFile `
        -RedirectStandardError (Join-Path $LogDir "router-error.log") `
        -NoNewWindow `
        -PassThru
    $processes += $routerProcess
    Write-Host "  Router PID: $($routerProcess.Id)" -ForegroundColor Gray
    Start-Sleep -Seconds 2

    # Step 2: Start 2 Chat Servers (Registry Clients)
    Write-Host "[2/4] Starting $RegistryCount Chat Servers..." -ForegroundColor Green
    for ($i = 1; $i -le $RegistryCount; $i++) {
        $serverLogFile = Join-Path $LogDir "server-$i.log"
        $serverProcess = Start-Process -FilePath $ServerExe `
            -ArgumentList "--router-host=127.0.0.1", "--router-port=$RouterRegistryPort", "--group=1" `
            -RedirectStandardOutput $serverLogFile `
            -RedirectStandardError (Join-Path $LogDir "server-$i-error.log") `
            -NoNewWindow `
            -PassThru
        $processes += $serverProcess
        Write-Host "  Chat Server $i PID: $($serverProcess.Id)" -ForegroundColor Gray
        Start-Sleep -Milliseconds 500
    }
    Start-Sleep -Seconds 2

    # Step 3: Start 10 Chat Clients (Agents)
    Write-Host "[3/4] Starting $AgentCount Chat Clients..." -ForegroundColor Green
    for ($i = 1; $i -le $AgentCount; $i++) {
        $clientLogFile = Join-Path $LogDir "client-$i.log"
        $clientProcess = Start-Process -FilePath $ClientExe `
            -ArgumentList "--router-host=127.0.0.1", "--router-port=$RouterAgentTcpPort" `
            -RedirectStandardOutput $clientLogFile `
            -RedirectStandardError (Join-Path $LogDir "client-$i-error.log") `
            -NoNewWindow `
            -PassThru
        $processes += $clientProcess
        Write-Host "  Chat Client $i PID: $($clientProcess.Id)" -ForegroundColor Gray
        Start-Sleep -Milliseconds 300
    }

    # Step 4: Wait for connections to stabilize
    Write-Host "[4/4] Waiting $TestDurationSeconds seconds for connections to stabilize..." -ForegroundColor Green
    Start-Sleep -Seconds $TestDurationSeconds

    # Analyze Results
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "Analyzing Test Results" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan

    # Extract client connection count from Server logs
    $serverStats = @()
    for ($i = 1; $i -le $RegistryCount; $i++) {
        $serverLogFile = Join-Path $LogDir "server-$i.log"
        $content = Get-Content $serverLogFile -ErrorAction SilentlyContinue

        # Find last "[Entry]" log with connection count
        $connectionLogs = $content | Select-String "\[Entry\].*\(.*: (\d+)\)"

        if ($connectionLogs) {
            $lastLog = $connectionLogs | Select-Object -Last 1
            if ($lastLog -match ": (\d+)\)") {
                $count = [int]$matches[1]
                $serverStats += @{ Server = "Server $i"; Count = $count }
                Write-Host "  Server $i`: $count client connections" -ForegroundColor Yellow
            }
        } else {
            Write-Host "  Server $i`: 0 client connections (no logs found)" -ForegroundColor Gray
            $serverStats += @{ Server = "Server $i"; Count = 0 }
        }
    }

    # Validate distribution results
    Write-Host ""
    Write-Host "Validation Results:" -ForegroundColor Cyan

    $totalConnected = ($serverStats | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum
    Write-Host "  Total Connected: $totalConnected / $AgentCount" -ForegroundColor White

    # Check if distribution is even (expected 5:5, tolerance ±1)
    $expectedPerServer = $AgentCount / $RegistryCount
    $passed = $true

    foreach ($stat in $serverStats) {
        $deviation = [Math]::Abs($stat.Count - $expectedPerServer)
        if ($deviation -le 1) {
            Write-Host "  OK $($stat.Server)`: $($stat.Count) (expected $expectedPerServer ± 1)" -ForegroundColor Green
        } else {
            Write-Host "  FAIL $($stat.Server)`: $($stat.Count) (deviation $deviation > 1)" -ForegroundColor Red
            $passed = $false
        }
    }

    Write-Host ""
    if ($passed -and $totalConnected -eq $AgentCount) {
        Write-Host "Test Result: PASSED" -ForegroundColor Green
        Write-Host "Round-Robin load balancing verified successfully!" -ForegroundColor Green
    } else {
        Write-Host "Test Result: FAILED" -ForegroundColor Red
        if ($totalConnected -ne $AgentCount) {
            Write-Host "Warning: Not all Agents connected successfully ($totalConnected / $AgentCount)" -ForegroundColor Yellow
        }
    }

} finally {
    # Cleanup all processes
    Write-Host ""
    Write-Host "Cleaning up test processes..." -ForegroundColor Yellow
    foreach ($proc in $processes) {
        if (!$proc.HasExited) {
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        }
    }
    Write-Host "Cleanup complete." -ForegroundColor Gray
    Write-Host ""
    Write-Host "Log files location: $LogDir" -ForegroundColor Cyan
}
