# 資料模型：Gateway Router Console Application

**日期**: 2025-10-23
**階段**: Phase 1 - Design
**目的**: 定義核心實體、資料結構與狀態模型

---

## 概述

本文件定義 Gateway Router Console Application 的核心資料模型,包含 Router、Registry Client、Agent 的狀態管理與資料流。所有設計基於 PinionCore.Remote.Gateway 框架的現有抽象,並遵循不使用 static class 的設計原則。

---

## 1. Router 核心實體

### 1.1 Router 實例

**類別**: `PinionCore.Remote.Gateway.Router` (框架提供)

**職責**: 核心路由服務,管理 Registry 註冊與 Agent 路由分配

**屬性**:
```csharp
public class Router : IDisposable
{
    // 公開端點
    public IService Registry { get; }   // Registry Client 連接端點
    public IService Session { get; }    // Agent 連接端點

    // 內部元件 (私有)
    private readonly SessionHub _SessionHub;              // 管理 Session 狀態與路由邏輯
    private readonly Registrys.Server _RegistryServer;    // 管理 Registry 註冊
    private readonly ISessionSelectionStrategy _Strategy; // 負載平衡策略
}
```

**生命週期**:
- 建構時需要 `ISessionSelectionStrategy` 實例
- `Registry` 與 `Session` 端點暴露給外部監聽器使用
- Dispose 時釋放所有內部資源

---

### 1.2 SessionCoordinator (內部)

**類別**: `PinionCore.Remote.Gateway.Hosts.SessionCoordinator` (框架提供)

**職責**: 協調 Agent 與 Registry 的路由分配,執行版本匹配與負載平衡

**狀態追蹤**:
```csharp
// 內部狀態類別 (框架實作)
class SessionState
{
    public ISession Session { get; }                                      // 關聯的 Agent Session
    public Dictionary<uint, Allocation> Allocations { get; }              // Group -> Allocation 映射
}

class Allocation
{
    public ILineAllocatable Allocator { get; }  // 配置的 Registry Allocator
    public IStreamable Stream { get; }          // 分配的 Backend Stream
}
```

**關鍵資料結構**:
```csharp
// Allocator 按版本與 Group 分組
private readonly Dictionary<byte[], List<ILineAllocatable>> _Allocators;

// 追蹤所有 Session 狀態
private readonly List<SessionState> _SessionStates;
```

**路由分配流程**:
1. Agent 提交協議版本 (`ProtocolSubmitted` 事件)
2. 根據版本查找可用的 `ILineAllocatable` 列表
3. 使用 `ISessionSelectionStrategy` 決定順序
4. 依序嘗試呼叫 `Allocator.Alloc()` 取得 Stream
5. 成功時呼叫 `Session.Set(group, stream)` 完成路由
6. 失敗時進入等待狀態,直到新 Registry 註冊或 Agent 斷線

**版本匹配邏輯**:
- 使用 `byte[]` 比較協議版本 (`IProtocol.VersionCode`)
- 只有版本完全匹配的 Agent 與 Registry 會被路由配對
- 不匹配時 Agent 保持等待狀態,不拒絕連線

---

### 1.3 Line (虛擬 Stream 配對)

**類別**: `PinionCore.Remote.Gateway.Registrys.Line` (框架提供)

**職責**: 建立 Agent 與 Registry 之間的雙向 Stream 配對

**結構**:
```csharp
public class Line
{
    public readonly IStreamable Frontend;  // 連接到 Agent
    public readonly IStreamable Backend;   // 連接到 Registry

    public Line()
    {
        var stream = new Stream();
        Frontend = stream;
        Backend = new PinionCore.Network.ReverseStream(stream);  // 反轉視角
    }
}
```

**資料流**:
```
Agent ←→ Frontend (IStreamable) ←→ [Line] ←→ Backend (IStreamable) ←→ Registry
```

**特性**:
- Frontend 與 Backend 共享相同的底層 Stream,但讀寫方向相反
- Agent 寫入 Frontend,Registry 從 Backend 讀取
- Registry 寫入 Backend,Agent 從 Frontend 讀取
- 實現透明的雙向訊息轉發

---

### 1.4 ISessionSelectionStrategy

**介面**: `PinionCore.Remote.Gateway.Hosts.ISessionSelectionStrategy` (框架提供)

**職責**: 定義負載平衡策略,決定如何從多個可用 Registry 中選擇

**定義**:
```csharp
public interface ISessionSelectionStrategy
{
    IEnumerable<ILineAllocatable> OrderAllocators(uint group, ILineAllocatable[] allocators);
}
```

**預設實作**: `RoundRobinSelector` (輪詢策略)

**行為**:
- 維護每個 Group 的索引狀態
- 每次呼叫返回 allocators 的輪詢順序
- 失敗後自動移至下一個 allocator

---

## 2. Registry Client 狀態模型

### 2.1 Registry Client 實例

**類別**: `PinionCore.Remote.Gateway.Registry` (框架提供)

**職責**: 遊戲服務作為 Registry 連接到 Router,接收路由的 Stream

**屬性**:
```csharp
public class Registry : IDisposable
{
    public PinionCore.Remote.Soul.IListenable Listener { get; }  // 接收 Router 分配的 Stream
    public IAgent Agent { get; }                                  // 連接到 Router 的 Agent

    private readonly uint _Group;        // 服務群組 ID
    private readonly byte[] _Version;    // 協議版本
}
```

**使用模式**:
```csharp
var protocol = ProtocolCreator.Create();
var registry = new PinionCore.Remote.Gateway.Registry(protocol, group: 1);

// 綁定 Listener 到遊戲服務
registry.Listener.StreamableEnterEvent += gameService.Join;
registry.Listener.StreamableLeaveEvent += gameService.Leave;

// Agent 連接到 Router
var peer = await connector.Connect(routerEndpoint);
registry.Agent.Enable(peer);
registry.Agent.Connect(router.Registry);

// 持續處理訊息
while (running)
{
    registry.Agent.HandlePackets();
    registry.Agent.HandleMessage();
}
```

---

### 2.2 Registry 連線狀態機

**定義**:
```csharp
public enum RegistryConnectionState
{
    Disconnected,   // 未連接:初始狀態或連線失敗後
    Connecting,     // 連接中:正在嘗試連接到 Router
    Connected,      // 已連接:連線成功且註冊完成
    WaitingRetry    // 等待重連:斷線後進入指數退避重連
}
```

**狀態轉換圖**:
```
[Disconnected]
    ↓ ConnectAsync()
[Connecting]
    ↓ 成功
[Connected]
    ↓ 斷線偵測 (Agent.Ping == false)
[WaitingRetry]
    ↓ 延遲後
[Connecting] → (循環重試)
```

**狀態管理資料**:
```csharp
public class RegistryConnectionManager
{
    public RegistryConnectionState State { get; private set; }

    private readonly Registry _registry;
    private readonly string _routerHost;
    private readonly int _routerPort;
    private readonly ExponentialBackoffReconnector _reconnector;

    // 連線監控
    private Task _monitorTask;
    private CancellationTokenSource _cts;
}
```

---

### 2.3 重連策略資料

**指數退避參數**:
```csharp
public class ExponentialBackoffReconnector
{
    private int _retryCount = 0;                       // 當前重試次數
    private const int InitialDelayMs = 1000;           // 初始延遲:1 秒
    private const int MaxDelayMs = 60000;              // 最大延遲:60 秒

    // 計算延遲:min(2^retryCount * 1000, 60000)
    public int CalculateDelay() => Math.Min((int)Math.Pow(2, _retryCount) * InitialDelayMs, MaxDelayMs);
}
```

**重試序列**:
- 第 1 次:1 秒
- 第 2 次:2 秒
- 第 3 次:4 秒
- 第 4 次:8 秒
- 第 5 次:16 秒
- 第 6 次:32 秒
- 第 7 次及以後:60 秒 (上限)

---

### 2.4 ILineAllocatable 實作

**介面**: `PinionCore.Remote.Gateway.Registrys.ILineAllocatable` (框架提供)

**定義**:
```csharp
public interface ILineAllocatable
{
    byte[] Version { get; }           // 協議版本
    uint Group { get; }               // 服務群組 ID
    IStreamable Alloc();              // 分配新的 Stream
    void Free(IStreamable stream);    // 釋放 Stream
}
```

**實作模式** (框架內部):
```csharp
class LineAllocator : IDisposable
{
    private readonly Queue<Line> _Lines = new Queue<Line>();

    public IStreamable Alloc()
    {
        lock (_Lines)
        {
            if (_Lines.Count == 0) return null;  // 無可用 Line
            var line = _Lines.Dequeue();
            return line.Backend;  // 返回 Backend Stream
        }
    }

    public void Add(Line line)
    {
        lock (_Lines) { _Lines.Enqueue(line); }
    }
}
```

---

## 3. Agent 狀態模型

### 3.1 Agent 實例

**類別**: `IAgent` (PinionCore.Remote 框架介面)

**職責**: 客戶端連接到 Router,等待路由分配並與 Registry 通訊

**屬性**:
```csharp
public interface IAgent
{
    bool Ping { get; }  // 連線存活狀態

    void Enable(IStreamable stream);       // 綁定網路 Stream
    void Connect(IService service);        // 連接到 Router Service
    void HandlePackets();                  // 處理接收封包
    void HandleMessage();                  // 處理訊息佇列
    void Disconnect();                     // 斷線
}
```

**使用模式**:
```csharp
var agent = PinionCore.Remote.Standalone.Provider.CreateAgent(protocol);
var peer = await connector.Connect(routerEndpoint);
agent.Enable(peer);
agent.Connect(router.Session);  // 連接到 Router 的 Session 端點

// 持續處理訊息
while (agent.Ping)
{
    agent.HandlePackets();
    agent.HandleMessage();
    Thread.Sleep(1);
}
```

---

### 3.2 Agent 狀態類型

**狀態定義**:
```csharp
public enum AgentState
{
    Connecting,      // 連接中:已建立 TCP/WebSocket 連線,等待握手完成
    WaitingMatch,    // 等待匹配:已連接但尚未分配到 Registry (無可用 Registry 或版本不匹配)
    Routed,          // 已路由:成功分配到 Registry,可進行業務通訊
    Disconnected     // 已斷線:連線中斷或主動斷開
}
```

**狀態轉換**:
```
[Connecting]
    ↓ 握手完成
[WaitingMatch] ←─┐
    ↓ 分配成功    │
[Routed]          │ 無可用 Registry
    ↓ 斷線        │
[Disconnected] ───┘
```

---

## 4. 最大相容性連線模型

### 4.1 連線來源類型

**定義**:
```csharp
public enum ConnectionSource
{
    DirectTcp,         // 直接 TCP 連線 (--tcp-port)
    DirectWebSocket,   // 直接 WebSocket 連線 (--web-port)
    GatewayRouted      // Gateway 路由連線 (--router-host)
}
```

---

### 4.2 CompositeListenable 模式

**類別**: `PinionCore.Remote.Server.CompositeListenable` (框架提供)

**職責**: 聚合多個 `IListenable` 來源,統一觸發 StreamableEnterEvent

**結構**:
```csharp
public class CompositeListenable : IListenable
{
    private readonly IListenable[] _listenables;

    public CompositeListenable(params IListenable[] listenables)
    {
        _listenables = listenables;

        foreach (var listenable in _listenables)
        {
            listenable.StreamableEnterEvent += (s) => StreamableEnterEvent?.Invoke(s);
            listenable.StreamableLeaveEvent += (s) => StreamableLeaveEvent?.Invoke(s);
        }
    }

    public event Action<IStreamable> StreamableEnterEvent;
    public event Action<IStreamable> StreamableLeaveEvent;
}
```

**使用模式**:
```csharp
var listenables = new List<IListenable>();

// 添加 TCP 直連
if (options.TcpPort.HasValue)
{
    var tcpListener = new Tcp.Listener();
    tcpListener.Bind(options.TcpPort.Value, 100);
    listenables.Add(new Listenable(tcpListener));
}

// 添加 WebSocket 直連
if (options.WebPort.HasValue)
{
    var webListener = new Web.Listener();
    webListener.Bind($"http://0.0.0.0:{options.WebPort.Value}/");
    listenables.Add(new Listenable(webListener));
}

// 添加 Gateway 路由
if (!string.IsNullOrEmpty(options.RouterHost))
{
    var registry = new Registry(protocol, options.Group ?? 1);
    // ... 連接到 Router ...
    listenables.Add(new Listenable(registry.Listener));
}

// 統一處理
var composite = new CompositeListenable(listenables.ToArray());
service.Join(composite);
```

---

### 4.3 IStreamable 統一抽象

**介面**: `PinionCore.Network.IStreamable` (框架提供)

**定義**:
```csharp
public interface IStreamable
{
    IAwaitableSource<int> Receive(byte[] buffer, int offset, int count);
    IAwaitableSource<int> Send(byte[] buffer, int offset, int count);
}
```

**實作來源**:
- **Tcp.Peer**: 來自 `Server.Tcp.Listener` 的 TCP 連線,透過 `StreamableEnterEvent` 事件傳遞
- **Web.Peer**: 來自 `Web.Listener` 的 WebSocket 連線,透過 `StreamableEnterEvent` 事件傳遞
- **Line.Frontend/Backend**: 來自 Gateway Router 的虛擬 Stream

**統一處理**:
```csharp
// IService.Join() 接收 IListenable
service.Join(compositeListenable);

// 內部對所有來源的 IStreamable 視為相同
compositeListenable.StreamableEnterEvent += (stream) =>
{
    // stream 可能來自 TCP、WebSocket 或 Gateway
    // 業務邏輯無需區分來源
    HandleNewConnection(stream);
};
```

---

## 5. 日誌資料模型

### 5.1 日誌事件類型

**定義**:
```csharp
public enum LogEventType
{
    Info,       // 一般資訊 (啟動、連線、路由分配)
    Warning,    // 警告 (重連、部分失敗)
    Error,      // 錯誤 (啟動失敗、端口衝突)
    Debug       // 除錯資訊 (包含堆疊追蹤)
}
```

---

### 5.2 日誌訊息結構

**格式**:
```
[時間戳記] [級別] [來源] 訊息內容
```

**範例**:
```
2025-10-23 14:30:15.123 [INFO] [RouterService] Router 啟動成功,負載平衡策略: Round-Robin
2025-10-23 14:30:15.456 [INFO] [AgentListenerService] Agent TCP 監聽已啟動,端口: 8001
2025-10-23 14:30:15.789 [INFO] [AgentListenerService] Agent WebSocket 監聽已啟動,端口: 8002
2025-10-23 14:30:16.012 [INFO] [RegistryListenerService] Registry TCP 監聽已啟動,端口: 8003
2025-10-23 14:31:20.345 [INFO] [RegistryListenerService] 新 Registry 連接,Group: 1, Version: [1, 0, 0]
2025-10-23 14:32:10.567 [INFO] [AgentListenerService] 新 Agent 連接 (TCP),Worker ID: agent-001
2025-10-23 14:32:10.678 [INFO] [RouterService] Agent 路由成功,Group: 1, Registry: registry-001
2025-10-23 14:35:45.890 [WARNING] [RegistryConnectionManager] Registry 連線中斷,進入重連流程
2025-10-23 14:35:46.123 [INFO] [ExponentialBackoffReconnector] 嘗試重連到 Router (第 1 次)
2025-10-23 14:35:47.234 [INFO] [RegistryConnectionManager] 成功重連到 Router
2025-10-23 14:40:00.000 [INFO] [Program] 收到 SIGTERM 訊號,開始優雅關閉...
2025-10-23 14:40:00.100 [INFO] [GracefulShutdownHandler] 關閉監聽器...
2025-10-23 14:40:00.200 [INFO] [GracefulShutdownHandler] 關閉 45 個 Agent 連線...
2025-10-23 14:40:02.500 [INFO] [GracefulShutdownHandler] 關閉 Router 服務...
2025-10-23 14:40:02.600 [INFO] [GracefulShutdownHandler] 寫入日誌檔案...
2025-10-23 14:40:02.700 [INFO] [Program] 優雅關閉完成
```

---

### 5.3 日誌配置資料

**Log 實例配置**:
```csharp
public class LoggingConfiguration
{
    public PinionCore.Utility.Log Log { get; }                    // 日誌實例
    public PinionCore.Utility.LogFileRecorder FileRecorder { get; }  // 檔案記錄器

    public string FileNamePrefix { get; set; }  // 檔案名稱前綴 (如 "RouterConsole")
    public bool EnableStdout { get; set; }      // 是否輸出到 stdout
    public bool EnableFile { get; set; }        // 是否輸出到檔案
}
```

**檔案命名格式**:
```
{FileNamePrefix}_yyyy_MM_dd_HH_mm_ss.log
```

**範例**:
- Router Console: `RouterConsole_2025_10_23_14_30_15.log`
- Chat Server: `ChatServer_2025_10_23_14_31_00.log`
- Chat Client: `ChatClient_2025_10_23_14_32_00.log`

---

## 6. 配置資料模型

### 6.1 RouterOptions

**結構**:
```csharp
public class RouterOptions
{
    public int AgentTcpPort { get; set; } = 8001;        // Agent TCP 端口
    public int AgentWebPort { get; set; } = 8002;        // Agent WebSocket 端口
    public int RegistryTcpPort { get; set; } = 8003;     // Registry TCP 端口

    // 驗證邏輯
    public bool Validate(out string error)
    {
        if (!IsValidPort(AgentTcpPort))
        {
            error = $"Agent TCP 端口無效: {AgentTcpPort}";
            return false;
        }
        // ... 其他驗證 ...
        error = null;
        return true;
    }

    private bool IsValidPort(int port) => port >= 1 && port <= 65535;
}
```

---

### 6.2 ChatServerOptions

**結構**:
```csharp
public class ChatServerOptions
{
    // 直連模式參數
    public int? TcpPort { get; set; }        // --tcp-port (可選)
    public int? WebPort { get; set; }        // --web-port (可選)

    // Gateway 模式參數
    public string RouterHost { get; set; }   // --router-host (可選)
    public int? RouterPort { get; set; }     // --router-port (可選)
    public uint Group { get; set; } = 1;     // --group (預設 1)

    // 驗證邏輯
    public bool HasDirectMode => TcpPort.HasValue || WebPort.HasValue;
    public bool HasGatewayMode => !string.IsNullOrEmpty(RouterHost) && RouterPort.HasValue;
    public bool HasAnyMode => HasDirectMode || HasGatewayMode;
}
```

---

### 6.3 ChatClientOptions

**結構**:
```csharp
public class ChatClientOptions
{
    // Router 模式參數
    public string RouterHost { get; set; }   // --router-host (可選)
    public int? RouterPort { get; set; }     // --router-port (可選)

    // 直連模式參數 (傳統模式,透過互動輸入)
    // 無命令列參數

    public bool HasRouterMode => !string.IsNullOrEmpty(RouterHost) && RouterPort.HasValue;
}
```

---

## 7. 優雅關閉資料模型

### 7.1 關閉步驟定義

**結構**:
```csharp
public enum ShutdownPhase
{
    ListenersClosure,    // 階段 1:關閉監聽器
    AgentsClosure,       // 階段 2:關閉 Agent 連線
    ServiceClosure,      // 階段 3:關閉 Router/Service
    LogsClosure          // 階段 4:關閉日誌系統
}
```

---

### 7.2 關閉超時配置

**參數**:
```csharp
public class ShutdownConfiguration
{
    public TimeSpan TotalTimeout { get; set; } = TimeSpan.FromSeconds(20);  // 總超時時間

    // 各階段時間分配建議
    public TimeSpan ListenersTimeout => TimeSpan.FromMilliseconds(500);     // 0.5 秒
    public TimeSpan AgentsTimeout => TimeSpan.FromSeconds(15);              // 15 秒
    public TimeSpan ServiceTimeout => TimeSpan.FromSeconds(2);              // 2 秒
    public TimeSpan LogsTimeout => TimeSpan.FromSeconds(2.5);               // 2.5 秒
}
```

---

## 8. 資料流圖

### 8.1 Router 路由分配流程

```
1. Agent 連接到 Router (TCP/WebSocket)
   ↓
2. Router 建立 IAgent 實例並綁定 IStreamable
   ↓
3. IAgent 連接到 Router.Session 端點
   ↓
4. SessionCoordinator 接收 ProtocolSubmitted 事件
   ↓
5. 根據 Version 查找可用的 ILineAllocatable
   ↓
6. 使用 ISessionSelectionStrategy 排序 Allocator
   ↓
7. 依序呼叫 Allocator.Alloc() 取得 Backend Stream
   ↓
8. 成功時呼叫 Session.Set(group, stream) 完成路由
   ↓
9. Agent 與 Registry 透過 Line 配對的 Stream 通訊
```

---

### 8.2 Registry Client 註冊流程

```
1. Chat Server 建立 Registry 實例
   ↓
2. Registry.Agent 使用 Tcp.Connector 連接到 Router.Registry 端點
   ↓
3. Registry.Agent 透過 Protocol 提交 Version 與 Group
   ↓
4. Router 的 Registrys.Server 接收註冊請求
   ↓
5. 建立 UserAllocState 並加入 SessionCoordinator 的 Allocator 列表
   ↓
6. Router 透過 IStreamProviable 介面推送 Stream 到 Registry
   ↓
7. Registry.Listener 觸發 StreamableEnterEvent
   ↓
8. Chat Server 的 IService 接收 IStreamable 並處理業務邏輯
```

---

### 8.3 最大相容性連線資料流

```
來源 1: 直接 TCP 連線
TCP Client → Tcp.Listener → Listenable → CompositeListenable → IService

來源 2: 直接 WebSocket 連線
WebSocket Client → Web.Listener → Listenable → CompositeListenable → IService

來源 3: Gateway 路由連線
Agent → Router → Registry.Listener → Listenable → CompositeListenable → IService

                                                    ↓
                                           統一的 IStreamable
                                                    ↓
                                            業務邏輯無差異處理
```

---

## 9. 效能與規模參數

### 9.1 目標效能指標

**並發連線**:
- Agent 連線:50 個並發
- Registry 連線:5 個並發
- 總連線數:55 個

**路由延遲**:
- 訊息轉發延遲: <10ms (相比直連)
- Agent 分配延遲: <2 秒 (當 Registry 已存在時)
- Registry 註冊延遲: <3 秒

---

### 9.2 資源估算

**記憶體使用**:
- Router 實例: ~5-10 MB (狀態追蹤 + 協調器)
- 每個 Agent 連線: ~100-200 KB (Agent 實例 + 緩衝)
- 預估總記憶體: 50 * 0.15MB + 10MB ≈ 17.5 MB

**執行緒數**:
- Router 內部自動管理 Agent 生命週期與訊息循環,無需額外執行緒開銷
- 監聽器: 3 個 Listener (Registry TCP, Session TCP, Session WebSocket)
- Router 內部: ~5-10 個執行緒 (非同步處理、事件分發)
- 預估總執行緒: ~8-13 個

---

## 10. 設計決策摘要

### 10.1 核心原則

1. **統一抽象**: 所有連線來源統一為 IStreamable,業務邏輯無差異
2. **狀態分離**: Router、Registry Client、Agent 各自維護獨立狀態,透過事件通訊
3. **生命週期管理**: 使用 IDisposable 與 CancellationToken 管理資源
4. **避免 static**: 所有服務使用實例化類別,支援依賴注入
5. **結構化日誌**: 統一格式,包含時間戳、級別、來源、訊息

---

### 10.2 擴展性考量

- `ISessionSelectionStrategy` 介面支援未來擴展其他負載平衡策略
- `CompositeListenable` 模式支援添加更多連線來源 (如 Unix Socket)
- `RegistryConnectionManager` 狀態機支援擴展更複雜的重連策略
- 日誌系統透過事件訂閱支援添加更多輸出目標 (如遠端日誌服務)

---

**資料模型定義完成**
**下一步**: 定義 contracts/ 目錄的介面合約
