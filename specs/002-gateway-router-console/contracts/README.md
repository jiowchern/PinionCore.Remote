# Contracts - API 合約與介面定義

**日期**: 2025-10-23
**階段**: Phase 1 - Design
**目的**: 定義所有服務介面與配置資料結構

---

## 目錄結構

```
contracts/
├── README.md                       # 本檔案
├── IRouterService.cs               # Router 服務介面
├── IAgentListenerService.cs        # Agent 監聽服務介面
├── IRegistryListenerService.cs     # Registry 監聽服務介面
├── IRegistryClientService.cs       # Registry Client 服務介面
├── ILoggingService.cs              # 日誌服務介面
├── RouterOptions.cs                # Router Console 命令列參數
├── ChatServerOptions.cs            # Chat Server 命令列參數
└── ChatClientOptions.cs            # Chat Client 命令列參數
```

---

## 介面設計原則

### 1. 避免 Static Class

所有服務介面都是實例化類別,支援:
- 依賴注入 (Dependency Injection)
- 單元測試 (Unit Testing)
- 生命週期管理 (IDisposable)

### 2. 統一異步模式

所有啟動/停止方法使用:
```csharp
Task StartAsync(CancellationToken cancellationToken);
Task StopAsync(CancellationToken cancellationToken);
```

### 3. 配置驗證

所有 Options 類別提供:
- `Validate(out string error)` 方法驗證配置有效性
- `GetUsageString()` 靜態方法產生使用說明

---

## 核心介面說明

### IRouterService

**職責**: 封裝 `PinionCore.Remote.Gateway.Router` 核心功能

**關鍵屬性**:
- `RegistryEndpoint`: Registry Client 連接端點
- `SessionEndpoint`: Agent 連接端點
- `Strategy`: 負載平衡策略 (預設 Round-Robin)

**實作專案**: `PinionCore.Consoles.Gateway.Router`

---

### IAgentListenerService

**職責**: 管理 Agent TCP 與 WebSocket 監聽器

**關鍵屬性**:
- `TcpPort`: Agent TCP 監聽端口
- `WebPort`: Agent WebSocket 監聽端口
- `IsRunning`: 是否已啟動

**關鍵方法**:
- `StartAsync(IRouterService, CancellationToken)`: 啟動監聽,綁定到 Router
- `GetActiveConnectionCount()`: 取得當前活躍連線數

**實作專案**: `PinionCore.Consoles.Gateway.Router`

---

### IRegistryListenerService

**職責**: 管理 Registry TCP 監聽器

**關鍵屬性**:
- `Port`: Registry TCP 監聽端口
- `IsRunning`: 是否已啟動

**關鍵方法**:
- `StartAsync(IRouterService, CancellationToken)`: 啟動監聽,綁定到 Router
- `GetActiveRegistryCount()`: 取得當前已註冊的 Registry 數

**實作專案**: `PinionCore.Consoles.Gateway.Router`

---

### IRegistryClientService

**職責**: 管理 Chat Server 作為 Registry 連接到 Router

**關鍵屬性**:
- `Registry`: Registry 實例 (暴露 Listener 給 Service 綁定)
- `RouterHost`, `RouterPort`: Router 連接資訊
- `Group`: 服務群組 ID
- `State`: 當前連線狀態 (Disconnected, Connecting, Connected, WaitingRetry)

**關鍵方法**:
- `StartAsync(CancellationToken)`: 啟動服務並連接到 Router
- `ReconnectAsync()`: 手動觸發重連

**實作專案**: `PinionCore.Consoles.Chat1.Server`

---

### ILoggingService

**職責**: 封裝 `PinionCore.Utility.Log` 與 `LogFileRecorder`

**關鍵方法**:
- `WriteInfo(string)` / `WriteInfo(Func<string>)`: 資訊級別日誌
- `WriteWarning(string)`: 警告級別日誌
- `WriteError(string)`: 錯誤級別日誌
- `WriteDebug(string)`: 除錯級別日誌
- `Shutdown()`: 儲存並關閉日誌檔案

**使用範例**:
```csharp
var loggingService = new LoggingService("RouterConsole");
loggingService.WriteInfo("Router started successfully");
loggingService.WriteInfo(() => $"Listening on port {port}");

// 優雅關閉時呼叫
loggingService.Shutdown();
loggingService.Dispose();
```

**實作專案**: 共用於所有專案

---

## 配置類別說明

### RouterOptions

**用途**: Router Console 命令列參數配置

**屬性**:
- `AgentTcpPort` (預設 8001)
- `AgentWebPort` (預設 8002)
- `RegistryTcpPort` (預設 8003)

**驗證規則**:
- 端口範圍: 1-65535
- 三個端口不可重複

**使用範例**:
```bash
PinionCore.Consoles.Gateway.Router --agent-tcp-port=9001 --agent-web-port=9002 --registry-tcp-port=9003
```

---

### ChatServerOptions

**用途**: Enhanced Chat Server 命令列參數配置

**屬性**:
- **直連模式**: `TcpPort`, `WebPort` (可選)
- **Gateway 模式**: `RouterHost`, `RouterPort`, `Group` (可選)

**模式判斷**:
- `HasDirectMode`: 啟用 TCP 或 WebSocket 直連
- `HasGatewayMode`: 啟用 Gateway 路由
- `IsMaxCompatibilityMode`: 同時啟用兩種模式

**驗證規則**:
- 至少啟用一種模式
- TCP 與 WebSocket 端口不可重複
- Gateway 模式需同時提供 Host 與 Port

**使用範例**:
```bash
# 最大相容性模式
PinionCore.Consoles.Chat1.Server \
  --tcp-port=23916 \
  --web-port=23917 \
  --router-host=127.0.0.1 \
  --router-port=8003 \
  --group=1
```

---

### ChatClientOptions

**用途**: Enhanced Chat Client 命令列參數配置

**屬性**:
- **Router 模式**: `RouterHost`, `RouterPort` (可選)
- **直連模式**: 無命令列參數,透過互動輸入

**模式判斷**:
- `HasRouterMode`: 啟用 Router 模式

**驗證規則**:
- Router 模式需同時提供 Host 與 Port

**使用範例**:
```bash
# Router 模式
PinionCore.Consoles.Chat1.Client --router-host=127.0.0.1 --router-port=8001

# 直連模式 (不提供參數)
PinionCore.Consoles.Chat1.Client
```

---

## 實作指引

### 依賴注入模式

```csharp
// Program.cs
var loggingService = new LoggingService("RouterConsole");
var routerService = new RouterService(loggingService);
var agentListenerService = new AgentListenerService(options.AgentTcpPort, options.AgentWebPort, loggingService);
var registryListenerService = new RegistryListenerService(options.RegistryTcpPort, loggingService);

await routerService.StartAsync(cancellationToken);
await agentListenerService.StartAsync(routerService, cancellationToken);
await registryListenerService.StartAsync(routerService, cancellationToken);
```

---

### 錯誤處理模式

```csharp
if (!options.Validate(out string error))
{
    loggingService.WriteError($"配置驗證失敗: {error}");
    Console.WriteLine(RouterOptions.GetUsageString());
    return 1;
}

try
{
    await agentListenerService.StartAsync(routerService, cancellationToken);
}
catch (InvalidOperationException ex)
{
    loggingService.WriteError($"Agent 監聽器啟動失敗: {ex.Message}");
    return 1;
}
```

---

### 優雅關閉模式

```csharp
// 捕捉 SIGTERM/SIGINT
_shutdownCts.Cancel();

// 依序關閉服務
await agentListenerService.StopAsync(shutdownCts.Token);
await registryListenerService.StopAsync(shutdownCts.Token);
await routerService.StopAsync(shutdownCts.Token);

// 最後關閉日誌
loggingService.Shutdown();
loggingService.Dispose();
```

---

## 設計決策

### 為何使用介面而非具體類別?

- **可測試性**: 介面可用 Mock 物件替代,方便單元測試
- **鬆耦合**: 降低元件間耦合,方便未來重構
- **擴展性**: 可輕易替換實作 (如替換負載平衡策略)

### 為何使用 Options 類別而非直接解析?

- **強型別**: 避免字串魔術值,編譯時檢查錯誤
- **驗證集中**: 所有驗證邏輯封裝在 Options 類別
- **可測試**: 可直接建立 Options 物件進行測試

### 為何使用異步方法 (Async/Await)?

- **網路 I/O 最佳化**: 避免阻塞執行緒,提升並發效能
- **取消支援**: 透過 CancellationToken 支援優雅關閉
- **統一模式**: .NET Core/8 最佳實踐

---

**合約定義完成**
**下一步**: 撰寫 quickstart.md 快速入門指南
