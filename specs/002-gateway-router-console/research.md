# 技術研究報告：Gateway Router Console Application

**日期**: 2025-10-23
**階段**: Phase 0 - Research
**目的**: 探索 PinionCore.Remote.Gateway 與 PinionCore.Network 整合模式,確立實作策略

---

## 研究概述

本文件記錄 Gateway Router Console Application 開發所需的關鍵技術決策與最佳實踐。研究涵蓋五個主題:Gateway-Network 整合、ILineAllocatable 實作、Registry 重連策略、最大相容性連線架構、優雅關閉模式。

所有研究基於 PinionCore.Remote 專案現有程式碼與測試範例,確保方案與框架設計理念一致。

---

## 1. PinionCore.Remote.Gateway 與 PinionCore.Network 整合模式

### 1.1 Router 建立與初始化

**核心類別**: `PinionCore.Remote.Gateway.Router` (D:\develop\PinionCore.Remote\PinionCore.Remote.Gateway\Router.cs)

**建構子簽章**:
```csharp
public Router(ISessionSelectionStrategy strategy)
```

**初始化模式**:
```csharp
// 使用 Round-Robin 負載平衡策略
using var router = new PinionCore.Remote.Gateway.Router(
    new PinionCore.Remote.Gateway.Hosts.RoundRobinSelector()
);

// Router 提供兩個服務端點
IService registryEndpoint = router.Registry;  // Registry Client 連接端點
IService sessionEndpoint = router.Session;    // Agent 連接端點
```

**關鍵設計**:
- Router 需要 `ISessionSelectionStrategy` 決定如何分配 Agent 到 Registry
- 提供雙端點架構:Registry(給遊戲服務) 與 Session(給客戶端)
- 內部使用 `SessionHub` 管理路由邏輯,`Registrys.Server` 管理 Registry 註冊

**決策**: Router Console 應用程式將使用 `RoundRobinSelector` 作為預設策略,符合規格需求(FR-016)。

---

### 1.2 Network Listener 建立與綁定

#### TCP Listener

**類別**: `PinionCore.Network.Tcp.Listener` (D:\develop\PinionCore.Remote\PinionCore.Network\Tcp\Listener.cs)

**使用模式**:
```csharp
var listener = new PinionCore.Network.Tcp.Listener();

// 綁定端口並開始監聽
listener.Bind(port: 8001, backlog: 100);

// 訂閱連線事件
listener.AcceptEvent += (peer) =>
{
    // peer 是 IStreamable 實例,代表一個新的 TCP 連線
    HandleNewConnection(peer);
};
```

**特性**:
- 自動設定 `Socket.NoDelay = true` 降低延遲
- 使用非同步 BeginAccept/EndAccept 模式
- 每個接受的連線產生 `Peer` 物件(實現 `IStreamable`)

#### WebSocket Listener

**類別**: `PinionCore.Network.Web.Listener` (D:\develop\PinionCore.Remote\PinionCore.Network\Web\Listener.cs)

**使用模式**:
```csharp
var webListener = new PinionCore.Network.Web.Listener();

// 綁定 URL (需包含 trailing slash)
webListener.Bind("http://0.0.0.0:8002/");

// 訂閱連線事件
webListener.AcceptEvent += (peer) =>
{
    // peer 是 IStreamable 實例,底層使用 WebSocket
    HandleNewConnection(peer);
};
```

**特性**:
- 基於 `System.Net.HttpListener` 自動處理 WebSocket 握手
- 使用 `AcceptWebSocketAsync()` 升級連線
- 產生的 `Peer` 包裝 `System.Net.WebSockets.WebSocket`

**決策**: Router Console 需要為 Agent 端點建立兩個監聽器(TCP + WebSocket),為 Registry 端點建立一個 TCP 監聽器。

---

### 1.3 Listener 與 Router 服務整合

**整合模式** (來自測試範例 D:\develop\PinionCore.Remote\PinionCore.Remote.Gateway.Test\Tests.cs):

```csharp
// 1. 建立 Router
using var router = new Router(new Hosts.RoundRobinSelector());

// 2. 建立 TCP Listener (Agent 端點)
var agentTcpListener = new PinionCore.Network.Tcp.Listener();
agentTcpListener.Bind(8001, 100);

// 3. 將新連線導向 Router 的 Session 服務
agentTcpListener.AcceptEvent += (peer) =>
{
    // 建立 Agent 處理該連線
    var agent = PinionCore.Remote.Standalone.Provider.CreateAgent(protocol);
    agent.Enable(peer);  // 綁定到 IStreamable
    agent.Connect(router.Session);  // 連接到 Router 的 Session 端點

    // 需要持續呼叫 HandlePackets() 與 HandleMessage()
    StartAgentWorker(agent);
};

// 4. Registry Listener 同理
var registryTcpListener = new PinionCore.Network.Tcp.Listener();
registryTcpListener.Bind(8003, 100);
registryTcpListener.AcceptEvent += (peer) =>
{
    // 類似處理,連接到 router.Registry
};
```

**挑戰**: 每個連線需要持續呼叫 `agent.HandlePackets()` 與 `agent.HandleMessage()` 維持通訊。

**解決方案**: 使用類似測試中的 `AgentWorker` 模式,建立背景執行緒池處理 Agent 訊息循環:

```csharp
public class AgentWorker : IDisposable
{
    private readonly IAgent _agent;
    private readonly CancellationTokenSource _cts;
    private readonly Task _loopTask;

    public AgentWorker(IAgent agent)
    {
        _agent = agent;
        _cts = new CancellationTokenSource();
        _loopTask = Task.Run(() =>
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                _agent.HandlePackets();
                _agent.HandleMessage();
                Thread.Sleep(1);  // 避免 CPU 100%
            }
        }, _cts.Token);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _loopTask.Wait(TimeSpan.FromSeconds(5));
        _cts.Dispose();
    }
}
```

**決策**: Router Console 將實作 AgentWorkerPool 管理所有連入的 Agent,確保訊息循環持續運行。

---

### 1.4 IStreamable 在 Gateway 內部的使用

**介面定義** (D:\develop\PinionCore.Remote\PinionCore.Network\IStreamable.cs):
```csharp
public interface IStreamable
{
    IAwaitableSource<int> Receive(byte[] buffer, int offset, int count);
    IAwaitableSource<int> Send(byte[] buffer, int offset, int count);
}
```

**Line 類別** (D:\develop\PinionCore.Remote\PinionCore.Remote.Gateway\Registrys\Line.cs):
```csharp
public class Line
{
    public readonly IStreamable Frontend;   // 連接到 Agent
    public readonly IStreamable Backend;    // 連接到 Registry

    public Line()
    {
        var stream = new Stream();
        Frontend = stream;
        Backend = new PinionCore.Network.ReverseStream(stream);  // 反轉視角
    }
}
```

**SessionCoordinator 分配流程** (D:\develop\PinionCore.Remote\PinionCore.Remote.Gateway\Hosts\SessionCoordinator.cs:223-242):
```csharp
private void TryAssign(SessionState state, uint group)
{
    foreach (var allocator in _strategy.OrderAllocators(group, allocators))
    {
        IStreamable stream = null;
        try
        {
            // 從 Registry 配置器取得 Stream
            stream = allocator.Alloc();
            if (stream == null) continue;

            // 分配給 Session(Agent)
            if (!state.Session.Set(group, stream))
            {
                allocator.Free(stream);
                continue;
            }

            // 記錄分配成功
            state.Allocations[group] = new Allocation(allocator, stream);
            return;
        }
        catch { if (stream != null) allocator.Free(stream); }
    }
}
```

**關鍵概念**:
- IStreamable 是統一的資料流抽象,TCP/WebSocket/Gateway 路由連線都實現此介面
- Line 建立雙向 Stream 配對,Frontend 給 Agent,Backend 給 Registry
- SessionCoordinator 透過 `ILineAllocatable.Alloc()` 取得 Backend Stream,然後分配給 Agent 的 Session

**決策**: Chat Server 作為 Registry Client 時,需要實現 ILineAllocatable 介面,提供 Stream 分配能力。

---

## 2. ILineAllocatable 實作模式

### 2.1 介面定義

**位置**: D:\develop\PinionCore.Remote\PinionCore.Remote.Gateway\Registrys\ILineAllocatable.cs

```csharp
public interface ILineAllocatable
{
    byte[] Version { get; }           // 協議版本(用於匹配)
    uint Group { get; }               // 服務群組 ID
    IStreamable Alloc();              // 分配新的 Stream
    void Free(IStreamable stream);    // 釋放 Stream
}
```

### 2.2 實作範例：UserAllocState

**位置**: D:\develop\PinionCore.Remote\PinionCore.Remote.Gateway\Registrys\UserAllocState.cs

```csharp
class UserAllocState : IStatus, IStreamProviable, ILineAllocatable
{
    private readonly LineAllocator _Allocator;
    private readonly uint _Group;
    private readonly byte[] _Version;

    public UserAllocState(byte[] version, uint group,
                         ICollection<IStreamProviable> streamsProvider,
                         ICollection<ILineAllocatable> lineAllocators)
    {
        _Allocators = lineAllocators;
        _Allocator = new LineAllocator();
        _Group = group;
        _Version = version;
    }

    byte[] ILineAllocatable.Version => _Version;
    uint ILineAllocatable.Group => _Group;

    IStreamable ILineAllocatable.Alloc() => _Allocator.Alloc();
    void ILineAllocatable.Free(IStreamable stream) => _Allocator.Free(stream);

    void IStatus.Enter()
    {
        _StreamProviables.Add(this);
        _Allocators.Add(this);  // 註冊到 SessionCoordinator
    }

    void IStatus.Leave()
    {
        _Allocators.Remove(this);
        _StreamProviables.Remove(this);
        _Allocator.Dispose();
    }
}
```

**LineAllocator 實作** (D:\develop\PinionCore.Remote\PinionCore.Remote.Gateway\Registrys\LineAllocator.cs):
```csharp
class LineAllocator : IDisposable
{
    private readonly Queue<Line> _Lines = new Queue<Line>();

    public IStreamable Alloc()
    {
        lock (_Lines)
        {
            if (_Lines.Count == 0) return null;
            var line = _Lines.Dequeue();
            return line.Backend;  // 返回 Backend Stream 給 Router
        }
    }

    public void Free(IStreamable stream)
    {
        // 釋放邏輯(本專案中通常不重用)
    }

    public void Add(Line line)
    {
        lock (_Lines)
        {
            _Lines.Enqueue(line);
        }
    }

    public void Dispose()
    {
        lock (_Lines)
        {
            _Lines.Clear();
        }
    }
}
```

### 2.3 Registry Client 整合 ILineAllocatable

**Registry 類別** (D:\develop\PinionCore.Remote\PinionCore.Remote.Gateway\Registry.cs):
```csharp
public class Registry : Registrys.Client
{
    public Registry(IProtocol protocol, uint group)
        : base(group, protocol.VersionCode)
    {
    }
}
```

**Registrys.Client 實作** (D:\develop\PinionCore.Remote\PinionCore.Remote.Gateway\Registrys\Client.cs:17-58):
```csharp
class Client : IDisposable
{
    public readonly PinionCore.Remote.Soul.IListenable Listener;  // 接收來自 Router 的 Stream
    public readonly IAgent Agent;  // 連接到 Router 的 Agent

    public Client(uint group, byte[] version)
    {
        _Streams = new Depot<IStreamable>();
        _Notifier = new Notifier<IStreamable>(_Streams);

        // Listener 透過 NotifierListener 橋接,當 Router 分配 Stream 時觸發事件
        Listener = new Gateway.Misc.NotifierListener(_Notifier);

        Agent = Protocols.Provider.CreateAgent();

        // 使用 Rx.NET 組合查詢,訂閱 Router 的 IRegisterable 介面
        var obs = from r in _Queryer.QueryNotifier<IRegisterable>().SupplyEvent()
                  from l in r.LoginNotifier.SupplyEvent()
                  from ret in l.Login(Group, version).RemoteValue()
                  from s in r.StreamsNotifier.SupplyEvent()
                  select s;

        _Dispose = obs.Subscribe(_Set);
    }

    private void _Set(IStreamProviable proviable)
    {
        // Router 提供的 Stream 加入本地 Depot
        proviable.Streams.Base.Supply += _Streams.Items.Add;
        proviable.Streams.Base.Unsupply += (s) => _Streams.Items.Remove(s);
    }
}
```

**NotifierListener 橋接** (D:\develop\PinionCore.Remote\PinionCore.Remote.Gateway\Misc\NotifierListener.cs):
```csharp
internal class NotifierListener : PinionCore.Remote.Soul.IListenable
{
    readonly Notifier<IStreamable> _notifier;

    event Action<IStreamable> IListenable.StreamableEnterEvent
    {
        add { _notifier.Base.Supply += value; }
        remove { _notifier.Base.Supply -= value; }
    }

    event Action<IStreamable> IListenable.StreamableLeaveEvent
    {
        add { _notifier.Base.Unsupply += value; }
        remove { _notifier.Base.Unsupply -= value; }
    }
}
```

### 2.4 Chat Server 整合策略

**目標**: Enhanced Chat Server 需要作為 Registry Client 連接到 Router,接收 Router 分配的 Stream,並將其導入業務邏輯。

**整合模式** (基於測試 D:\develop\PinionCore.Remote\PinionCore.Remote.Gateway.Test\Tests.cs:153-154):
```csharp
// 1. 建立 Registry Client
var protocol = ProtocolCreator.Create();
var registry = new PinionCore.Remote.Gateway.Registry(protocol, group: 1);

// 2. 建立遊戲服務
var gameService = PinionCore.Remote.Server.Provider.CreateService(
    new ChatServerEntry(),
    protocol
);

// 3. 綁定 Registry Listener 到遊戲服務
registry.Listener.StreamableEnterEvent += gameService.Join;
registry.Listener.StreamableLeaveEvent += gameService.Leave;

// 4. Registry Agent 連接到 Router
var connector = new PinionCore.Network.Tcp.Connector();
var peer = await connector.Connect(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8003));
registry.Agent.Enable(peer);
registry.Agent.Connect(router.Registry);

// 5. 持續處理 Agent 訊息
var worker = new AgentWorker(registry.Agent);
```

**決策**:
- Enhanced Chat Server 將實作 `RegistryClientService` 封裝上述邏輯
- 使用 `CompositeConnectionService` 統一管理來自三種來源的連線(TCP 直連、WebSocket 直連、Gateway 路由)
- Registry Listener 與 TCP/WebSocket Listener 使用相同的 `gameService.Join()` 處理連線,達成最大相容性

---

## 3. Registry 重連策略

### 3.1 需求

根據規格 FR-034:Enhanced Chat Server 在 Registry 模式下,當與 Router 連線中斷時,必須實現重連邏輯(如指數退避重試)。

### 3.2 指數退避演算法設計

**概念**:
- 首次重連延遲:1 秒
- 每次失敗後延遲加倍:2秒、4秒、8秒...
- 最大延遲上限:60 秒
- 可選最大重試次數或無限重試

**實作範例**:
```csharp
public class ExponentialBackoffReconnector
{
    private readonly Func<Task<bool>> _connectAction;
    private readonly ILog _log;
    private int _retryCount = 0;
    private CancellationTokenSource _cts;

    public ExponentialBackoffReconnector(Func<Task<bool>> connectAction, ILog log)
    {
        _connectAction = connectAction;
        _log = log;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                _log.WriteInfo($"嘗試連接到 Router (第 {_retryCount + 1} 次)");

                bool success = await _connectAction();

                if (success)
                {
                    _log.WriteInfo("成功連接到 Router");
                    _retryCount = 0;  // 重置重試計數
                    return;
                }
            }
            catch (Exception ex)
            {
                _log.WriteInfo($"連接失敗: {ex.Message}");
            }

            // 計算延遲時間(指數退避)
            int delay = Math.Min((int)Math.Pow(2, _retryCount) * 1000, 60000);
            _log.WriteInfo($"將在 {delay / 1000} 秒後重試");

            _retryCount++;
            await Task.Delay(delay, _cts.Token);
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
    }
}
```

### 3.3 連線狀態管理

**狀態機設計**:
```
[未連接] --Connect()--> [連接中] --成功--> [已連接]
                             |
                            失敗
                             |
                             v
                         [等待重連] --延遲後--> [連接中]
                             ^                     |
                             |----失敗重試---------|

[已連接] --斷線偵測--> [等待重連]
```

**狀態列舉**:
```csharp
public enum RegistryConnectionState
{
    Disconnected,   // 未連接
    Connecting,     // 連接中
    Connected,      // 已連接
    WaitingRetry    // 等待重連
}
```

**狀態管理類別**:
```csharp
public class RegistryConnectionManager
{
    private RegistryConnectionState _state = RegistryConnectionState.Disconnected;
    private readonly object _stateLock = new object();

    public RegistryConnectionState State
    {
        get { lock (_stateLock) { return _state; } }
        private set
        {
            lock (_stateLock)
            {
                if (_state != value)
                {
                    _log.WriteInfo($"Registry 連線狀態: {_state} -> {value}");
                    _state = value;
                }
            }
        }
    }

    public async Task<bool> ConnectAsync()
    {
        State = RegistryConnectionState.Connecting;

        try
        {
            var connector = new PinionCore.Network.Tcp.Connector();
            var peer = await connector.Connect(new IPEndPoint(_routerHost, _routerPort));

            _registry.Agent.Enable(peer);
            _registry.Agent.Connect(_routerRegistryService);

            State = RegistryConnectionState.Connected;

            // 啟動監控執行緒偵測斷線
            StartDisconnectionMonitor();

            return true;
        }
        catch
        {
            State = RegistryConnectionState.Disconnected;
            return false;
        }
    }

    private void StartDisconnectionMonitor()
    {
        Task.Run(() =>
        {
            while (State == RegistryConnectionState.Connected)
            {
                if (!_registry.Agent.Ping)  // 偵測斷線
                {
                    _log.WriteWarning("偵測到 Router 連線中斷");
                    State = RegistryConnectionState.WaitingRetry;
                    _reconnector.StartAsync(CancellationToken.None);
                    break;
                }
                Thread.Sleep(1000);
            }
        });
    }
}
```

### 3.4 決策

- Enhanced Chat Server 將實作 `RegistryConnectionManager` 管理連線狀態
- 使用 `ExponentialBackoffReconnector` 處理重連邏輯
- 首次重連延遲 1 秒,最大延遲 60 秒,無限重試(直到應用程式關閉)
- 透過 `Agent.Ping` 屬性偵測斷線,觸發重連流程

---

## 4. 最大相容性連線架構

### 4.1 需求

根據規格 FR-027、FR-028:Enhanced Chat Server 必須支援最大相容性連線模式,同時開啟:
1. 直接 TCP 連線(`--tcp-port`)
2. 直接 WebSocket 連線(`--web-port`)
3. Gateway 路由連線(`--router-host` + `--router-port`)

所有連線在業務邏輯層統一視為 `IStreamable`,無差別處理。

### 4.2 CompositeListenable 模式

**參考**: PinionCore.Consoles.Chat1.Server\Program.cs:88-102 中的 `CompositeListenable` 用法

```csharp
// 現有 Chat1.Server 實作
var tcpListener = new PinionCore.Network.Tcp.Listener();
tcpListener.Bind(tcpPort, 100);

var webListener = new PinionCore.Network.Web.Listener();
webListener.Bind($"http://0.0.0.0:{webPort}/");

// 組合兩個監聽器
var compositeListener = new PinionCore.Remote.Server.CompositeListenable(
    new PinionCore.Remote.Server.Listenable(tcpListener),
    new PinionCore.Remote.Server.Listenable(webListener)
);

// Service 統一處理來自兩個來源的連線
service.Join(compositeListener);
```

### 4.3 擴展為三重來源

**新增 Gateway Listener 整合**:
```csharp
// 1. 建立 Registry Client
var protocol = ProtocolCreator.Create();
var registry = new PinionCore.Remote.Gateway.Registry(protocol, group);

// 2. Registry Listener 包裝成 Listenable
var gatewayListenable = new PinionCore.Remote.Server.Listenable(registry.Listener);

// 3. 組合三個監聽器
var compositeListener = new PinionCore.Remote.Server.CompositeListenable(
    tcpListenable,         // 直接 TCP
    webListenable,         // 直接 WebSocket
    gatewayListenable      // Gateway 路由
);

// 4. Service 統一處理
service.Join(compositeListener);
```

**關鍵概念**:
- `PinionCore.Remote.Server.Listenable` 包裝不同來源的 `IListenable` 介面
- `CompositeListenable` 聚合多個 Listenable,統一觸發 `StreamableEnterEvent`
- `IService.Join()` 接收 `IListenable`,對所有來源的 `IStreamable` 視為相同

### 4.4 條件啟用邏輯

**命令列參數組合**:
```csharp
public class ChatServerOptions
{
    public int? TcpPort { get; set; }        // --tcp-port
    public int? WebPort { get; set; }        // --web-port
    public string RouterHost { get; set; }   // --router-host
    public int? RouterPort { get; set; }     // --router-port
    public uint? Group { get; set; }         // --group
}
```

**啟用邏輯**:
```csharp
var listenables = new List<IListenable>();

// 條件 1: TCP 直連
if (options.TcpPort.HasValue)
{
    var tcpListener = new PinionCore.Network.Tcp.Listener();
    tcpListener.Bind(options.TcpPort.Value, 100);
    listenables.Add(new PinionCore.Remote.Server.Listenable(tcpListener));
    log.WriteInfo($"TCP 直連模式已啟用,端口: {options.TcpPort.Value}");
}

// 條件 2: WebSocket 直連
if (options.WebPort.HasValue)
{
    var webListener = new PinionCore.Network.Web.Listener();
    webListener.Bind($"http://0.0.0.0:{options.WebPort.Value}/");
    listenables.Add(new PinionCore.Remote.Server.Listenable(webListener));
    log.WriteInfo($"WebSocket 直連模式已啟用,端口: {options.WebPort.Value}");
}

// 條件 3: Gateway 路由
if (!string.IsNullOrEmpty(options.RouterHost) && options.RouterPort.HasValue)
{
    var registry = new PinionCore.Remote.Gateway.Registry(protocol, options.Group ?? 1);

    // 連接到 Router (使用重連管理器)
    var connectionManager = new RegistryConnectionManager(registry, options.RouterHost, options.RouterPort.Value);
    await connectionManager.ConnectAsync();

    listenables.Add(new PinionCore.Remote.Server.Listenable(registry.Listener));
    log.WriteInfo($"Gateway 路由模式已啟用,Router: {options.RouterHost}:{options.RouterPort.Value}");
}

// 驗證至少有一個模式啟用
if (listenables.Count == 0)
{
    log.WriteError("錯誤:必須至少提供 --tcp-port, --web-port 或 --router-host 其中一個參數");
    return 1;
}

// 組合所有監聽器
var compositeListener = new PinionCore.Remote.Server.CompositeListenable(listenables.ToArray());
service.Join(compositeListener);
```

### 4.5 決策

- 使用 `CompositeListenable` 實現三重來源整合
- 根據命令列參數動態啟用監聽器
- 所有來源的 `IStreamable` 在 `service.Join()` 後統一處理,業務邏輯無需區分來源
- 日誌明確記錄啟用的連線模式

---

## 5. 優雅關閉模式

### 5.1 需求

根據規格 FR-021、FR-022:
- 正確處理 SIGTERM、SIGINT 訊號
- 在 20 秒內完成優雅關閉(關閉所有連線與監聽器)
- 確保日誌完整寫入(`Log.Shutdown()` 與 `LogFileRecorder.Save()/Close()`)

### 5.2 .NET 訊號捕捉

**.NET 6+ 使用 `Console.CancelKeyPress` 與 `AppDomain.CurrentDomain.ProcessExit`**:

```csharp
class Program
{
    private static CancellationTokenSource _shutdownCts = new CancellationTokenSource();

    static async Task Main(string[] args)
    {
        // 捕捉 Ctrl+C (SIGINT)
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;  // 防止立即終止
            log.WriteInfo("收到 SIGINT 訊號,開始優雅關閉...");
            _shutdownCts.Cancel();
        };

        // 捕捉 Process Exit (SIGTERM in Docker)
        AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
        {
            log.WriteInfo("收到 SIGTERM 訊號,開始優雅關閉...");
            _shutdownCts.Cancel();
        };

        // 啟動應用程式
        await RunApplicationAsync(_shutdownCts.Token);
    }

    static async Task RunApplicationAsync(CancellationToken cancellationToken)
    {
        // ... 初始化 Router, Listeners 等 ...

        // 等待關閉訊號
        await Task.Delay(Timeout.Infinite, cancellationToken);
    }
}
```

### 5.3 20 秒超時關閉策略

**關閉步驟順序**:
1. 停止接受新連線(關閉所有 Listener)
2. 關閉現有連線(Dispose Agents, Services)
3. 等待日誌寫入完成
4. 超時保護(20 秒後強制終止)

**實作**:
```csharp
async Task ShutdownAsync(CancellationToken cancellationToken)
{
    var shutdownCts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
    var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, shutdownCts.Token);

    try
    {
        log.WriteInfo("開始優雅關閉流程(超時時間: 20 秒)");

        // 步驟 1: 停止監聽器
        log.WriteInfo("關閉監聽器...");
        agentTcpListener?.Dispose();
        agentWebListener?.Dispose();
        registryTcpListener?.Dispose();
        await Task.Delay(100, combinedCts.Token);  // 短暫延遲確保關閉完成

        // 步驟 2: 關閉 AgentWorkerPool
        log.WriteInfo($"關閉 {agentWorkerPool.Count} 個 Agent 連線...");
        await agentWorkerPool.DisposeAllAsync(combinedCts.Token);

        // 步驟 3: 關閉 Router
        log.WriteInfo("關閉 Router 服務...");
        router?.Dispose();
        await Task.Delay(100, combinedCts.Token);

        // 步驟 4: 關閉日誌系統
        log.WriteInfo("寫入日誌檔案...");
        fileRecorder.Save();
        fileRecorder.Close();
        log.Shutdown();  // 等待非同步佇列清空

        log.WriteInfo("優雅關閉完成");
    }
    catch (OperationCanceledException)
    {
        log.WriteWarning("優雅關閉超時(20 秒),強制終止");
    }
    catch (Exception ex)
    {
        log.WriteError($"優雅關閉發生錯誤: {ex.Message}");
    }
}
```

### 5.4 AgentWorkerPool 批次關閉

**Pool 設計**:
```csharp
public class AgentWorkerPool : IDisposable
{
    private readonly List<AgentWorker> _workers = new List<AgentWorker>();
    private readonly object _lock = new object();

    public void Add(AgentWorker worker)
    {
        lock (_lock) { _workers.Add(worker); }
    }

    public void Remove(AgentWorker worker)
    {
        lock (_lock) { _workers.Remove(worker); }
    }

    public int Count
    {
        get { lock (_lock) { return _workers.Count; } }
    }

    public async Task DisposeAllAsync(CancellationToken cancellationToken)
    {
        List<AgentWorker> workersCopy;
        lock (_lock)
        {
            workersCopy = new List<AgentWorker>(_workers);
            _workers.Clear();
        }

        // 平行關閉所有 Worker
        var disposeTasks = workersCopy.Select(w => Task.Run(() => w.Dispose(), cancellationToken));
        await Task.WhenAll(disposeTasks);
    }

    public void Dispose()
    {
        DisposeAllAsync(CancellationToken.None).Wait();
    }
}
```

### 5.5 Log.Shutdown() 與 LogFileRecorder 正確順序

**來自規格附錄範例**:
```csharp
// 優雅關閉時的正確順序
fileRecorder.Save();     // 1. 寫入緩衝的日誌到檔案
fileRecorder.Close();    // 2. 關閉檔案串流
log.Shutdown();          // 3. 等待非同步日誌佇列清空
```

**說明**:
- `LogFileRecorder.Save()`: 將記憶體緩衝的日誌寫入磁碟
- `LogFileRecorder.Close()`: 關閉檔案串流,釋放檔案鎖
- `Log.Shutdown()`: 等待 Log 內部的非同步執行緒完成所有日誌寫入操作

**注意**: 必須在所有日誌寫入完成後才呼叫,否則部分日誌可能遺失。

### 5.6 決策

- Router Console 與 Enhanced Chat 應用程式將實作統一的優雅關閉模式
- 使用 `CancellationTokenSource` 配合 20 秒超時保護
- 關閉順序:Listeners → Agents → Router/Service → Logs
- 使用 `AgentWorkerPool` 集中管理所有 Agent 的生命週期
- 確保日誌系統最後關閉,避免日誌遺失

---

## 6. 額外研究發現

### 6.1 Agent 必須持續呼叫 HandlePackets() 與 HandleMessage()

**原因**: PinionCore.Remote 的 IAgent 不會自動啟動訊息循環,必須由應用程式手動驅動。

**證據**: 所有測試範例都使用 `AgentWorker` 或手動循環:
```csharp
while (running)
{
    agent.HandlePackets();
    agent.HandleMessage();
    Thread.Sleep(1);
}
```

**決策**: Router Console 必須為每個連入的連線建立 AgentWorker,使用執行緒池管理。

---

### 6.2 Protocol 版本匹配機制

**版本取得**: `IProtocol.VersionCode` 屬性提供協議版本 byte[]

**匹配邏輯** (D:\develop\PinionCore.Remote\PinionCore.Remote.Gateway\Hosts\SessionCoordinator.cs:178-190):
```csharp
private void _OnProtocolSubmitted(SessionState state, byte[] version)
{
    // 根據版本取得可用的 Allocators
    IEnumerable<ILineAllocatable> allocators;
    if (!_Allocators.TryGetValue(version, out allocators))
    {
        // 沒有匹配版本的 Allocator,進入等待狀態
        return;
    }
    // ... 繼續分配流程 ...
}
```

**決策**: Router 不需要實作版本檢查邏輯,SessionCoordinator 已內建版本匹配機制。若無匹配版本,連線自然進入等待狀態,符合規格 FR-017 的等待匹配機制。

---

### 6.3 Round-Robin 負載平衡實作

**RoundRobinSelector** (D:\develop\PinionCore.Remote\PinionCore.Remote.Gateway\Hosts\RoundRobinSelector.cs):
```csharp
class RoundRobinSelector : ISessionSelectionStrategy
{
    private readonly Dictionary<uint, int> _Indexes = new Dictionary<uint, int>();

    public IEnumerable<ILineAllocatable> OrderAllocators(uint group, ILineAllocatable[] allocators)
    {
        if (allocators.Length == 0) yield break;

        if (!_Indexes.TryGetValue(group, out int index))
        {
            index = 0;
        }

        // 從當前索引開始輪詢
        for (int i = 0; i < allocators.Length; i++)
        {
            int currentIndex = (index + i) % allocators.Length;
            yield return allocators[currentIndex];
        }

        // 更新索引
        _Indexes[group] = (index + 1) % allocators.Length;
    }
}
```

**特性**:
- 按 Group 獨立追蹤輪詢索引
- 每次分配後遞增索引,實現輪流分配
- 即使分配失敗也會遞增索引,避免重複嘗試同一個 Allocator

**決策**: Router Console 直接使用 `RoundRobinSelector`,無需自訂負載平衡邏輯。

---

## 7. 技術決策總結

### 7.1 Router Console 架構

**核心元件**:
1. **RouterService**: 封裝 `PinionCore.Remote.Gateway.Router` 實例,使用 `RoundRobinSelector`
2. **AgentListenerService**: 管理 Agent TCP/WebSocket 監聽器,接受連線並建立 AgentWorker
3. **RegistryListenerService**: 管理 Registry TCP 監聽器,接受連線並建立 AgentWorker
4. **AgentWorkerPool**: 集中管理所有連入連線的 AgentWorker,支援批次關閉
5. **LoggingConfiguration**: 配置 PinionCore.Utility.Log + LogFileRecorder
6. **GracefulShutdownHandler**: 處理 SIGTERM/SIGINT,執行 20 秒超時優雅關閉

### 7.2 Enhanced Chat Server 架構

**核心元件**:
1. **CompositeConnectionService**: 整合 TCP/WebSocket/Gateway 三重監聽來源
2. **RegistryClientService**: 封裝 `PinionCore.Remote.Gateway.Registry`,提供連線管理
3. **RegistryConnectionManager**: 管理 Registry 連線狀態,實作重連邏輯
4. **ExponentialBackoffReconnector**: 指數退避重連演算法
5. **GatewayConfiguration**: 命令列參數解析與驗證

### 7.3 Enhanced Chat Client 架構

**核心元件**:
1. **RouterConnectionService**: 使用 `Tcp.Connector` 連接到 Router 的 Agent 端點
2. **DualModeManager**: 管理 Router 模式與直連模式切換
3. **RouterConfiguration**: 命令列參數解析

### 7.4 關鍵設計原則

1. **避免 static class**: 所有服務使用實例化類別,支援依賴注入
2. **使用原生套件**: 嚴格使用 PinionCore.Network, Gateway, Utility,不引入第三方框架
3. **統一 IStreamable 抽象**: 所有連線來源統一視為 IStreamable,業務邏輯無差別處理
4. **分層關閉順序**: Listeners → Agents → Router/Service → Logs
5. **結構化日誌**: 包含時間戳、級別、來源、訊息,輸出到 stdout + 檔案

---

## 8. 替代方案與權衡

### 8.1 AgentWorker 執行緒池 vs 單一執行緒輪詢

**評估的替代方案**: 使用單一執行緒輪詢所有 Agent 的 HandlePackets/HandleMessage

**優點**: 較低的執行緒開銷

**缺點**:
- Agent 數量增加時,輪詢延遲增加
- 單一執行緒瓶頸,無法利用多核心

**決策**: 使用執行緒池,每個 AgentWorker 獨立執行緒,利用多核心效能,符合 50 並發 Agent 的效能目標。

---

### 8.2 Registry 重連 vs 直接終止

**評估的替代方案**: Registry 斷線後直接終止 Chat Server,由外部系統(如 Docker)重啟

**優點**: 實作簡單

**缺點**:
- 正在服務的玩家連線會中斷
- 重啟時間較長(需重新初始化所有資源)

**決策**: 實作重連機制,保持現有玩家連線,符合高可用性需求(規格 FR-034)。

---

### 8.3 日誌框架: PinionCore.Utility.Log vs Serilog/NLog

**評估的替代方案**: 使用 Serilog 或 NLog 等成熟日誌框架

**優點**: 功能豐富(結構化日誌、多目標輸出、效能優化)

**缺點**:
- 引入第三方依賴,違反專案原則
- 增加套件複雜度

**決策**: 使用 PinionCore.Utility.Log,符合規格 FR-014/FR-015 與專案原則(嚴禁第三方框架)。

---

## 9. 下一步行動

根據本研究報告,Phase 1 設計階段需要產生:

1. **data-model.md**: 定義 Router、Registry Client、Agent 的狀態模型與資料結構
2. **contracts/**: 定義服務介面(IRouterService, IRegistryClientService, ILoggingService 等)
3. **quickstart.md**: 撰寫快速入門指南,涵蓋本地開發與 Docker 部署

所有設計將基於本研究報告的技術決策與最佳實踐。

---

**研究完成日期**: 2025-10-23
**下一階段**: Phase 1 - Design
