# Fix Double Connection Issue

## Problem
Chat Server creates TWO TCP connections to Router because of duplicate connection logic in `Program.cs`.

## Root Cause
1. **Line 72-84**: Manual connection code (DUPLICATE)
2. **Line 118**: `registryConnectionManager.Start()` (ALSO connects)

`RegistryConnectionStates/ConnectingState.cs` already handles connection, so manual connection is redundant.

## Solution: Remove Duplicate Connection Code

### File to Modify
`D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Server\Program.cs`

### Changes Required

**DELETE Lines 71-86**:
```csharp
// DELETE THIS BLOCK (Lines 71-86)
// T031: 建立 Registry Agent 連接邏輯 (使用 Tcp.Connector)
var connector = new PinionCore.Network.Tcp.Connector();
var endpoint = new IPEndPoint(IPAddress.Parse(options.RouterHost), options.RouterPort.Value);

// 嘗試連接
try
{
    var peer = connector.Connect(endpoint).Result; // 同步等待連接
    
    registry.Agent.VersionCodeErrorEvent += (expected, actual) =>
    {
        System.Console.WriteLine($"Version code mismatch: expected {expected}, got {actual}. ");                      
    };
    registry.Agent.Enable(peer);

    System.Console.WriteLine($"Successfully connected to Router at {options.RouterHost}:{options.RouterPort}");
```

**REMOVE the try-catch wrapper**:
- Delete Line 76: `try {`
- Delete Lines 126-131: `} catch (Exception ex) { ... }`

**KEEP and MOVE OUT of try-catch**:
- Lines 88-104: AgentWorker code
- Line 107: `listeners.Add(registry.Listener);`
- Lines 109-118: RegistryConnectionManager creation and Start()
- Lines 120-125: shutdownTasks

### Final Code Should Look Like

```csharp
if (options.HasGatewayMode)
{
    System.Console.WriteLine($"Gateway mode enabled: connecting to Router {options.RouterHost}:{options.RouterPort} (Group: {options.Group})");

    // 建立 Registry Client
    registry = new PinionCore.Remote.Gateway.Registry(protocol, options.Group);

    // T032: 啟動 AgentWorker (持續處理 registry.Agent.HandlePackets/HandleMessage)
    var agentWorkerRunning = true;
    var agentWorkerTask = Task.Run(() =>
    {
        while (agentWorkerRunning)
        {
            registry.Agent.HandlePackets();
            registry.Agent.HandleMessage();
            System.Threading.Thread.Sleep(1); // 短暫休眠避免忙等待
        }
    });

    registryWorkerDispose = () =>
    {
        agentWorkerRunning = false;
        agentWorkerTask.Wait(TimeSpan.FromSeconds(2));
    };

    // 添加 Registry.Listener 到 CompositeListenable
    listeners.Add(registry.Listener);

    // T031: 建立 RegistryConnectionManager (負責連接、斷線偵測與重連)
    var log = PinionCore.Utility.Log.Instance;
    registryConnectionManager = new RegistryConnectionManager(
        registry,
        options.RouterHost,
        options.RouterPort.Value,
        log
    );

    // 啟動連接管理器 (會自動建立連接)
    registryConnectionManager.Start();

    shutdownTasks.Add(() =>
    {
        registryWorkerDispose?.Invoke();
        registryConnectionManager?.Dispose();
        registry?.Dispose();
    });
}
```

## Verification

After modification, rebuild and test:

```cmd
cd D:\develop\PinionCore.Remote
dotnet build PinionCore.Consoles.Chat1.Server\PinionCore.Consoles.Chat1.Server.csproj

cd tests\phase4-registry-connection
.\start-router.cmd        # Terminal 1
.\start-chatserver.cmd    # Terminal 2
```

**Expected Result**:
- Router console shows **ONLY ONE** "Registry 連接建立 (當前連接數: 1)"
- `netstat -an | findstr :8003` shows **ONLY TWO** ESTABLISHED connections (one inbound, one outbound)

## Rollback

If something goes wrong:
```cmd
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Server
copy Program.cs.backup Program.cs
```
