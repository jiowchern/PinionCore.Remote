# Fix Double Connection Issue - Automated Script

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Fix Double Connection Issue" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$programCsPath = "..\..\PinionCore.Consoles.Chat1.Server\Program.cs"

if (!(Test-Path $programCsPath)) {
    Write-Host "[ERROR] Program.cs not found at: $programCsPath" -ForegroundColor Red
    exit 1
}

Write-Host "[1/4] Backing up Program.cs..." -ForegroundColor Yellow
Copy-Item $programCsPath "$programCsPath.backup" -Force
Write-Host "[OK] Backup created: Program.cs.backup" -ForegroundColor Green
Write-Host ""

Write-Host "[2/4] Reading file..." -ForegroundColor Yellow
$content = Get-Content $programCsPath -Raw -Encoding UTF8

Write-Host "[3/4] Removing duplicate connection code..." -ForegroundColor Yellow

# Define the OLD block to remove (Lines 71-86 + try/catch wrapper)
$oldBlock = @'
                // T031: 建立 Registry Agent 連接邏輯 \(使用 Tcp\.Connector\)
                var connector = new PinionCore\.Network\.Tcp\.Connector\(\);
                var endpoint = new IPEndPoint\(IPAddress\.Parse\(options\.RouterHost\), options\.RouterPort\.Value\);

                // 嘗試連接
                try
                \{
                    var peer = connector\.Connect\(endpoint\)\.Result; // 同步等待連接
                    
                    registry\.Agent\.VersionCodeErrorEvent \+= \(expected, actual\) =>
                    \{
                        System\.Console\.WriteLine\(\$"Version code mismatch: expected \{expected\}, got \{actual\}\. "\);                      
                    \};
                    registry\.Agent\.Enable\(peer\);

                    System\.Console\.WriteLine\(\$"Successfully connected to Router at \{options\.RouterHost\}:\{options\.RouterPort\}"\);

                    // T032: 啟動 AgentWorker
'@

# Define the NEW block (keep AgentWorker and rest, remove manual connection)
$newBlock = @'
                // T032: 啟動 AgentWorker (持續處理 registry.Agent.HandlePackets/HandleMessage)
'@

# Replace
$content = $content -replace $oldBlock, $newBlock

# Remove the catch block at the end
$content = $content -replace '\s+\}\s+catch \(Exception ex\)\s+\{\s+System\.Console\.WriteLine\(\$"Failed to connect to Router: \{ex\.Message\}"\);\s+System\.Console\.WriteLine\("Chat Server will only use direct connection modes\."\);\s+\}', ''

Write-Host "[4/4] Writing fixed file..." -ForegroundColor Yellow
$content | Out-File -FilePath $programCsPath -Encoding UTF8 -NoNewline
Write-Host "[OK] File fixed successfully" -ForegroundColor Green
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Fix Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Rebuild the project:"
Write-Host "   dotnet build ..\..\PinionCore.Consoles.Chat1.Server\PinionCore.Consoles.Chat1.Server.csproj"
Write-Host ""
Write-Host "2. Test:"
Write-Host "   .\start-router.cmd        (Terminal 1)"
Write-Host "   .\start-chatserver.cmd    (Terminal 2)"
Write-Host ""
Write-Host "3. Verify: Should see ONLY ONE 'Registry connection established' log"
Write-Host ""
Write-Host "If something goes wrong, restore backup:"
Write-Host "   copy Program.cs.backup Program.cs"
Write-Host ""
