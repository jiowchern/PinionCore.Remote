# Phase 4 Manual Test Guide

## Issue with Automated Test

The automated test cannot read logs immediately because **LogFileRecorder buffers logs in memory** and only writes to disk when the program exits (on Dispose).

Therefore, **manual testing is required** to observe console output in real-time.

---

## Manual Test Steps

### Step 1: Start Router (Terminal 1)

```cmd
cd D:\develop\PinionCore.Remote\tests\phase4-registry-connection
.\start-router.cmd
```

**Expected Console Output:**
```
[2025/10/25_XX:XX:XX][Info]Router configuration: Agent TCP=8001, Agent WebSocket=8002, Registry TCP=8003
[2025/10/25_XX:XX:XX][Info]Router started successfully, load balancing strategy: Round-Robin
[2025/10/25_XX:XX:XX][Info]Agent TCP listening started, port: 8001
[2025/10/25_XX:XX:XX][Info]Agent WebSocket listening started, port: 8002
[2025/10/25_XX:XX:XX][Info]Agent listener successfully bound to Session endpoint
[2025/10/25_XX:XX:XX][Info]Registry TCP listening started, port: 8003
[2025/10/25_XX:XX:XX][Info]Registry listener successfully bound to Registry endpoint
[2025/10/25_XX:XX:XX][Info]Router Console startup complete, all listeners ready
```

---

### Step 2: Start Chat Server (Terminal 2)

```cmd
cd D:\develop\PinionCore.Remote\tests\phase4-registry-connection
.\start-chatserver.cmd
```

**Watch Terminal 1 (Router Console) - Expected:**
```
[2025/10/25_XX:XX:XX][Info]Registry connection established (current connections: 1)
```

✅ **T033 PASS** - Router logs Registry connection event with connection count

**Watch Terminal 2 (Chat Server Console) - Expected:**
```
[2025/10/25_XX:XX:XX][Info]Successfully connected to Router
[2025/10/25_XX:XX:XX][Info]Registry state: Connected
```

---

### Step 3: Test Disconnect Detection

**In Terminal 2**, press `Ctrl+C` to stop Chat Server.

**Watch Terminal 1 (Router Console) - Expected:**
```
[2025/10/25_XX:XX:XX][Info]Registry connection lost (current connections: 0)
```

✅ **T034 PASS** - Router detects and logs Registry disconnect with updated connection count

**Watch Terminal 2 (Chat Server Console) - Expected:**
```
[2025/10/25_XX:XX:XX][Info]Router connection lost, error code: ConnectionReset
[2025/10/25_XX:XX:XX][Info]Registry state: Reconnecting
[2025/10/25_XX:XX:XX][Info]Will retry in 1 seconds
```

---

### Step 4: Test Reconnection Logic

**In Terminal 2**, restart Chat Server:

```cmd
.\start-chatserver.cmd
```

**Watch Chat Server Console - Expected:**
```
[2025/10/25_XX:XX:XX][Info]Registry state: Connecting
[2025/10/25_XX:XX:XX][Info]Successfully connected to Router
[2025/10/25_XX:XX:XX][Info]Registry state: Connected
```

**Watch Router Console - Expected:**
```
[2025/10/25_XX:XX:XX][Info]Registry connection established (current connections: 1)
```

✅ **T035 PASS** - Chat Server reconnects successfully, Router logs the reconnection

**Verify Reconnection Time:**
- Reconnection should complete within 10 seconds
- First retry after 1 second (exponential backoff)

---

### Step 5: Check Log File (After Shutdown)

**In Terminal 1**, press `Ctrl+C` to stop Router gracefully.

**Expected Shutdown Sequence:**
```
[2025/10/25_XX:XX:XX][Info]Received shutdown signal, starting graceful shutdown...
[2025/10/25_XX:XX:XX][Info]Closing listeners...
[2025/10/25_XX:XX:XX][Info]Closing Agent listener...
[2025/10/25_XX:XX:XX][Info]Closing Registry listener...
[2025/10/25_XX:XX:XX][Info]Closing 0 Agent connections...
[2025/10/25_XX:XX:XX][Info]Closing Router service...
[2025/10/25_XX:XX:XX][Info]Writing log file...
[2025/10/25_XX:XX:XX][Info]Graceful shutdown complete
```

**Now check log file:**

```cmd
cd D:\develop\PinionCore.Remote\tests\phase4-registry-connection
.\check-logs.cmd
```

**Expected Log File Content:**
```
[2025/10/25_XX:XX:XX][Info]Registry TCP listening started, port: 8003
[2025/10/25_XX:XX:XX][Info]Registry listener successfully bound to Registry endpoint
[2025/10/25_XX:XX:XX][Info]Registry connection established (current connections: 1)
[2025/10/25_XX:XX:XX][Info]Registry connection lost (current connections: 0)
[2025/10/25_XX:XX:XX][Info]Registry connection established (current connections: 1)
```

---

## Acceptance Checklist

### T033: Router-side Registry Connection Logging
- [ ] Router console shows "Registry connection established (current connections: 1)"
- [ ] Message appears immediately when Chat Server connects
- [ ] Connection count is correct

### T034: Router-side Registry Disconnect Detection
- [ ] Router console shows "Registry connection lost (current connections: 0)"
- [ ] Message appears immediately when Chat Server disconnects
- [ ] Connection count decrements correctly
- [ ] Uses SocketErrorEvent (not Ping polling) - verified in code

### T035: Registry Reconnection Logic
- [ ] Chat Server auto-reconnects after disconnect
- [ ] Uses exponential backoff (1s, 2s, 4s, 8s...)
- [ ] Reconnection completes within 10 seconds
- [ ] Router logs reconnection event
- [ ] Functionality works normally after reconnection

---

## Test Report Template

**Test Date:** ___________  
**Tester:** ___________

**Results:**

| Task | Description | Status | Notes |
|------|-------------|--------|-------|
| T033 | Registry connection logging | ☐ PASS ☐ FAIL | |
| T034 | Registry disconnect detection | ☐ PASS ☐ FAIL | |
| T035 | Registry reconnection logic | ☐ PASS ☐ FAIL | |

**Observations:**
- Connection count accuracy: ___________
- Reconnection time: ___________ seconds
- Any errors or issues: ___________

**Conclusion:**
☐ All tests passed - Phase 4 complete  
☐ Issues found - requires fixes

---

**Important:** You MUST observe console output in real-time. Log files are only written when the program exits.
