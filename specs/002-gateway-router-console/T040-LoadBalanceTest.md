# T040 - Round-Robin Load Balance Test

**Task**: Verify Router distributes 10 Agents evenly across 2 Registries (expected 5:5, tolerance ±1)

## Test Setup

### Prerequisites
1. Ensure all applications are built in Debug or Release mode
2. Close any existing Router/Server/Client processes
3. Open multiple command prompts (at least 13 windows)

### File Locations
- **Router**: `D:\develop\PinionCore.Remote\PinionCore.Consoles.Gateway.Router\bin\Debug\net8.0\`
- **Chat Server**: `D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Server\bin\Debug\net8.0\`
- **Chat Client**: `D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Client\bin\Debug\net8.0\`

## Manual Test Procedure

### Step 1: Start Router
```bash
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Gateway.Router\bin\Debug\net8.0
PinionCore.Consoles.Gateway.Router.exe --agent-tcp-port=10001 --registry-tcp-port=10003
```

Expected output:
```
[Info]Router settings: Agent TCP=10001, Agent WebSocket=8002, Registry TCP=10003
[Info]Router startup successful
...
```

### Step 2: Start 2 Chat Servers
**Terminal 2 - Server 1**:
```bash
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Server\bin\Debug\net8.0
PinionCore.Consoles.Chat1.Server.exe --router-host=127.0.0.1 --router-port=10003 --group=1
```

**Terminal 3 - Server 2**:
```bash
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Server\bin\Debug\net8.0
PinionCore.Consoles.Chat1.Server.exe --router-host=127.0.0.1 --router-port=10003 --group=1
```

Expected output from each server:
```
Initializing Gateway mode: Router 127.0.0.1:10003 (Group: 1)
[OK] Gateway mode initialized
...
[Entry] client connection established (current connections: X)
```

### Step 3: Start 10 Chat Clients
**Terminals 4-13**: Open 10 separate terminals and run:
```bash
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Client\bin\Debug\net8.0
PinionCore.Consoles.Chat1.Client.exe --router-host=127.0.0.1 --router-port=10001
```

Expected output from each client:
```
[TCP] Connecting to Router at 127.0.0.1:10001...
[TCP] Connected to Router. Waiting for routing allocation...
...
```

### Step 4: Verify Load Distribution

**Check Server 1 logs**:
Look for the last `[Entry]` log line and note the client connection count.

**Check Server 2 logs**:
Look for the last `[Entry]` log line and note the client connection count.

## Expected Results

### Passing Criteria
- ✓ Total connected clients = 10
- ✓ Server 1 connections: 4-6 (5 ± 1)
- ✓ Server 2 connections: 4-6 (5 ± 1)
- ✓ No connection errors in Router logs
- ✓ No connection errors in Server logs

### Example Passing Result
```
Server 1: [Entry] client connection established (current connections: 5)
Server 2: [Entry] client connection established (current connections: 5)
Total: 10/10 clients connected
Distribution: 5:5 (Perfect Round-Robin)
```

## Troubleshooting

### Issue: "Port already in use"
**Solution**: Kill existing processes
```bash
taskkill /F /IM PinionCore.Consoles.Gateway.Router.exe
taskkill /F /IM PinionCore.Consoles.Chat1.Server.exe
taskkill /F /IM PinionCore.Consoles.Chat1.Client.exe
```

### Issue: Client cannot connect
**Checklist**:
1. Router is running and listening on port 10001
2. No firewall blocking local connections
3. Correct host/port parameters

### Issue: Server cannot register
**Checklist**:
1. Router is running and listening on port 10003
2. Correct router-host and router-port parameters
3. Check Server logs for connection errors

## Automated Test Script (Optional)

Due to console input redirect limitations, the automated PowerShell script may fail.
If you want to use it anyway:

```powershell
# Fix required in Program.cs first:
# Replace Console.ReadKey() with Console.Read() when Environment.UserInteractive == false
# OR check if (Console.IsInputRedirected) before calling ReadKey()

.\test-load-balance.ps1
```

## Enhanced Entry.cs Logging

To enable client connection counting in Chat Servers, the following enhancement was added to `PinionCore.Consoles.Chat1\Entry.cs`:

```csharp
private readonly PinionCore.Utility.Log _log;

public Entry()
{
    _log = PinionCore.Utility.Log.Instance;
    // ...
}

public void RegisterClientBinder(PinionCore.Remote.IBinder binder)
{
    // ...
    _log.WriteInfo(() => $"[Entry] client connection established (current connections: {currentCount})");
}

public void UnregisterClientBinder(PinionCore.Remote.IBinder binder)
{
    // ...
    _log.WriteInfo(() => $"[Entry] client connection terminated (current connections: {currentCount})");
}
```

This logging allows us to track how many clients each Registry (Chat Server) is serving, which is essential for verifying Round-Robin load balancing.

## Test Result

**Status**: ⏳ Pending Manual Verification

**Next Steps**:
1. Execute manual test procedure
2. Record actual distribution results
3. Update this document with PASSED/FAILED status
4. If failed, analyze Router's RoundRobinSelector implementation
