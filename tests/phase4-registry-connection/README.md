# Phase 4 Test Scripts: Registry Connection and Reconnection

Test scripts for Phase 4 (US2) to verify Router-side Registry connection logging, disconnect detection, and Chat Server reconnection functionality.

## Test Tasks

- **T033**: Add Registry connection logging on Router side
- **T034**: Implement Registry disconnect detection and logging on Router side  
- **T035**: Test Registry reconnection logic

## Script Overview

### 1. Automated Testing (Recommended)

**test-phase4-auto.ps1** - PowerShell automated test script

```powershell
# Run automated test
.\test-phase4-auto.ps1

# Custom reconnection wait time (default 15 seconds)
.\test-phase4-auto.ps1 -ReconnectWaitSeconds 20
```

**Test Flow**:
1. Check executables exist
2. Clean old logs
3. Start Router
4. Start Chat Server (Registry mode)
5. Check connection logs
6. Stop Chat Server (simulate disconnect)
7. Check disconnect logs
8. Restart Chat Server (test reconnection)
9. Check reconnection logs
10. Display log summary

**Acceptance Criteria**:
- Router logs "Registry connection established (current connections: 1)"
- Router logs "Registry connection lost (current connections: 0)"
- Chat Server reconnects, Router logs connection again
- Reconnection completes within 10 seconds

---

### 2. Manual Testing

For step-by-step observation of logs and state changes.

#### Step 1: Start Router

```cmd
start-router.cmd
```

**Expected Output**:
```
Registry TCP listening started, port: 8003
Registry listener successfully bound to Registry endpoint
Router started successfully, load balancing strategy: Round-Robin
```

---

#### Step 2: Start Chat Server (New Terminal)

```cmd
start-chatserver.cmd
```

**Expected Output (Router side)**:
```
Registry connection established (current connections: 1)
```

**Expected Output (Chat Server side)**:
```
Successfully connected to Router
Registry state: Connected
```

---

#### Step 3: Test Disconnect and Reconnect

Press `Ctrl+C` in Chat Server terminal to close.

**Expected Output (Router side)**:
```
Registry connection lost (current connections: 0)
```

**Expected Output (Chat Server side)**:
```
Router connection lost, error code: ConnectionReset
Registry state: Reconnecting
Will retry in 1 seconds
Will retry in 2 seconds
...
Successfully connected to Router
Registry state: Connected
```

**Expected Output (Router side)**:
```
Registry connection established (current connections: 1)
```

---

#### Step 4: Check Logs

```cmd
check-logs.cmd
```

**Expected Content**:
```
Registry TCP listening started, port: 8003
Registry listener successfully bound to Registry endpoint
Registry connection established (current connections: 1)
Registry connection lost (current connections: 0)
Registry connection established (current connections: 1)  <-- Reconnection success
```

---

## Prerequisites

Before running tests, ensure the project is compiled:

```bash
cd D:\develop\PinionCore.Remote

# Build Router
dotnet build PinionCore.Consoles.Gateway.Router\PinionCore.Consoles.Gateway.Router.csproj

# Build Chat Server
dotnet build PinionCore.Consoles.Chat1.Server\PinionCore.Consoles.Chat1.Server.csproj
```

---

## Quick Start

**Option 1: One-click test (Recommended)**

```cmd
cd D:\develop\PinionCore.Remote\tests\phase4-registry-connection
run-test.cmd
```

**Option 2: PowerShell direct**

```powershell
cd D:\develop\PinionCore.Remote\tests\phase4-registry-connection
.\test-phase4-auto.ps1
```

---

## Troubleshooting

### Issue 1: PowerShell Execution Policy

**Error**: "Execution of scripts is disabled on this system..."

**Solution**:
```powershell
# Run as Administrator
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Or use Bypass mode
powershell -ExecutionPolicy Bypass -File .\test-phase4-auto.ps1
```

---

### Issue 2: Port Already in Use

**Error**: "Address already in use" or "Port already occupied"

**Solution**:
```cmd
# Check if port 8003 is occupied
netstat -ano | findstr :8003

# Stop process occupying the port
taskkill /PID <PID> /F
```

---

### Issue 3: Executable Not Found

**Error**: "Executable not found"

**Solution**:
```bash
# Rebuild project
dotnet build --configuration Debug
```

---

## Log Locations

- **Router Logs**: `PinionCore.Consoles.Gateway.Router\bin\Debug\net8.0\RouterConsole_*.log`
- **Chat Server Logs**: Console output (not written to file currently)

---

## Test Report Template

After completing tests, fill in the following checklist:

### T033: Router-side Registry Connection Logging
- [ ] Router logs "Registry connection established" event
- [ ] Log includes current connection count
- [ ] Log timestamps are correct

### T034: Router-side Registry Disconnect Detection
- [ ] Router logs "Registry connection lost" event  
- [ ] Connection count decrements correctly on disconnect
- [ ] Uses SocketErrorEvent instead of Ping polling

### T035: Registry Reconnection Logic
- [ ] Chat Server auto-reconnects
- [ ] Uses exponential backoff strategy (1s, 2s, 4s, 8s...)
- [ ] Reconnection completes within 10 seconds
- [ ] Router correctly logs reconnection event
- [ ] Functionality works normally after reconnection

---

## Related Documentation

- [Phase 4 Task List](../../specs/002-gateway-router-console/tasks.md)
- [Implementation Plan](../../specs/002-gateway-router-console/plan.md)
- [CLAUDE.md - Disconnect Detection Pattern](../../CLAUDE.md#7-network-disconnect-detection-pattern)

---

**Test Duration**: Approximately 5-10 minutes  
**Last Updated**: 2025-10-25
