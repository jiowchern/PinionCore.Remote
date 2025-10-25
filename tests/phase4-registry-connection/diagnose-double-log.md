# Diagnose Double Connection Log Issue

## Problem
When Chat Server connects to Router, Router console shows TWO "Registry 連接建立" logs instead of ONE.

## Diagnosis Steps

### 1. Check Router Console Output

When you see double logs, please record:
- [ ] First log shows: `Registry 連接建立 (當前連接數: ?)` 
- [ ] Second log shows: `Registry 連接建立 (當前連接數: ?)`
- [ ] What are the connection counts? Both 1? Or 1 then 2?

### 2. Possible Causes

#### Cause A: Event Subscribed Twice
**Check**: In `RegistryListenerService.cs` line 53
```csharp
_listener.StreamableEnterEvent += _OnRegistryConnected;
```
**Question**: Is this line executed twice somehow?

#### Cause B: Gateway Internal Double Connection  
**Check**: Chat Server's `registry.Agent.Enable(peer)` might internally create two connections or trigger the event twice.

#### Cause C: `_registryEndpoint.Join()` Logs Again
**Check**: In line 82:
```csharp
_registryEndpoint?.Join(streamable);
```
**Question**: Does `registryEndpoint.Join()` internally log something that looks similar?

### 3. Quick Test

**Modify `_OnRegistryConnected` temporarily** to add more debugging:

```csharp
private void _OnRegistryConnected(IStreamable streamable)
{
    _registryCount++;
    _log.WriteInfo(() => $"[DEBUG] _OnRegistryConnected called, count BEFORE log: {_registryCount}");
    _log.WriteInfo(() => $"Registry 連接建立 (當前連接數: {_registryCount})");
    _log.WriteInfo(() => $"[DEBUG] About to call _registryEndpoint.Join()");
    
    // 將連接傳遞給 Registry 端點處理
    _registryEndpoint?.Join(streamable);
    
    _log.WriteInfo(() => $"[DEBUG] _registryEndpoint.Join() completed");
}
```

**Expected**: If method is called twice, you'll see TWO sets of DEBUG messages.

### 4. Alternative: Check if Two Physical Connections

**In Router terminal**, after seeing double logs, check port connections:

```cmd
netstat -an | findstr :8003
```

**Expected**: Only ONE ESTABLISHED connection from 127.0.0.1

---

## Please Provide

1. **Full Router Console Output** when Chat Server connects
2. **Connection count numbers** in both logs
3. **Result of netstat command**

This will help determine the root cause.
