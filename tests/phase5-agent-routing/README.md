# Phase 5: Agent Routing Test Guide

## 測試目標
驗證 Router 的 Agent 路由功能：
- Agent 連接到 Router
- Router 自動將 Agent 路由到可用的 Chat Server
- Round-Robin 負載平衡

## 測試架構

```
Chat Client (Agent) → Router (8001) → Chat Server (Registry)
                         ↑
Chat Server (Registry) --+ (8003)
```

## 測試步驟

### 步驟 1: 編譯所有專案

```powershell
cd D:\develop\PinionCore.Remote
dotnet build --configuration Debug
```

### 步驟 2: 啟動測試環境

打開**三個獨立的 PowerShell 終端機**：

#### 終端機 1 - Router
```powershell
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Gateway.Router\bin\Debug\net8.0
.\PinionCore.Consoles.Gateway.Router.exe
```

**預期輸出**：
```
[Info]Router 配置: Agent TCP=8001, Agent WebSocket=8002, Registry TCP=8003
[Info]Router 啟動成功，負載平衡策略: Round-Robin
[Info]Agent TCP 監聽已啟動，端口: 8001
[Info]Registry TCP 監聽已啟動，端口: 8003
[Info]Router Console 啟動完成，所有監聽器已就緒
```

#### 終端機 2 - Chat Server (Registry 模式)
```powershell
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Server\bin\Debug\net8.0
.\PinionCore.Consoles.Chat1.Server.exe --router-host=127.0.0.1 --router-port=8003 --group=1
```

**預期輸出**：
```
[Info]Registry 狀態: 未連接
[Info]Registry 狀態: 連接中 (127.0.0.1:8003)
[Info]Agent online enter.
[Info]成功連接到 Router
[Info]Registry 狀態: 已連接
```

**Router 應該顯示**：
```
[Info]Registry 連接建立 (當前連接數: 1)
```

#### 終端機 3 - Chat Client (連接到 Router)
```powershell
cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Client\bin\Debug\net8.0
.\PinionCore.Consoles.Chat1.Client.exe localhost 8001
```

**預期行為**：
1. Client 連接到 Router (8001)
2. Router 將 Client 路由到 Chat Server
3. Client 應該能正常登入和聊天

### 步驟 3: 驗證功能

在 **Chat Client 終端機**：

1. **登入測試**：
   ```
   login TestUser TestPassword
   ```
   預期：登入成功

2. **發送訊息測試**：
   ```
   say Hello from Router!
   ```
   預期：訊息正常顯示

3. **斷線重連測試**：
   - 按 `Ctrl+C` 關閉 Client
   - 重新啟動 Client
   - 重新登入
   預期：重連成功

### 步驟 4: 負載平衡測試（可選）

1. **啟動第二個 Chat Server**（新終端機）：
   ```powershell
   cd D:\develop\PinionCore.Remote\PinionCore.Consoles.Chat1.Server\bin\Debug\net8.0
   .\PinionCore.Consoles.Chat1.Server.exe --router-host=127.0.0.1 --router-port=8003 --group=1
   ```

2. **啟動多個 Chat Client**（依次啟動 4-5 個）：
   ```powershell
   .\PinionCore.Consoles.Chat1.Client.exe localhost 8001
   ```

3. **預期行為**：
   - Router 使用 Round-Robin 策略
   - Client 應該交替分配到兩個 Chat Server

## 驗收標準

### ✅ 基本路由功能
- [  ] Router 成功啟動並監聽端口
- [  ] Chat Server 成功連接到 Router Registry
- [  ] Chat Client 成功連接到 Router Agent
- [  ] Client 能夠登入並發送訊息

### ✅ 斷線處理
- [  ] Client 斷線後 Router 正確處理
- [  ] Client 重連後可以正常工作

### ✅ 負載平衡（可選）
- [  ] 多個 Chat Server 時路由分配均勻
- [  ] Round-Robin 策略正確運作

## 已知限制

- 目前缺少 Agent 連接/斷線日誌（Phase 5 後續任務）
- 目前缺少路由分配成功/等待日誌（Phase 5 後續任務）

## 故障排除

### 問題：Client 連接後無反應
- 檢查 Router 是否已啟動
- 檢查 Chat Server 是否已連接到 Router
- 使用 `netstat -an | findstr "8001 8003"` 檢查端口狀態

### 問題：Client 無法登入
- 確認 Chat Server 已連接到 Router (Router 日誌應顯示 "Registry 連接建立")
- 確認沒有其他程序占用端口

### 問題：訊息無法發送
- 這可能是路由功能未正確工作，請回報詳細日誌
