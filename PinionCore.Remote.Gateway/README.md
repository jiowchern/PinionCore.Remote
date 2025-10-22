# PinionCore.Remote.Gateway

## 概述

PinionCore.Remote.Gateway 是一個分散式遊戲服務閘道系統，提供客戶端與多個遊戲服務之間的智慧路由與連線管理。它採用三層架構設計，讓客戶端能夠透過單一連接點同時與多個遊戲服務通訊，而無需關心底層的連線細節。

## 核心概念

Gateway 系統由三個主要組件組成：

### 1. Router (路由閘道)
作為中央協調者，負責：
- 接收遊戲服務的註冊（透過 Registry 端點）
- 接收客戶端連線（透過 Session 端點）
- 根據策略將客戶端路由到對應的遊戲服務
- 管理多個遊戲服務的生命週期
- **支援多協議版本並存**：自動識別並隔離不同協議版本的客戶端與服務

### 2. Registry (註冊中心)
作為遊戲服務的註冊代理，負責：
- 向 Router 註冊自己的 Group ID 與協議版本
- 提供 Listener 給遊戲服務，用於接收玩家連線
- 管理遊戲服務與 Router 之間的通訊

### 3. Agent (客戶端代理)
作為玩家客戶端，負責：
- 連接到 Router 的 Session 端點並提供協議版本資訊
- 透過 AgentPool 管理多個遊戲服務的連線
- 使用 CompositeNotifier 整合多個遊戲服務的介面
- 提供統一的 API 給上層應用

## 架構圖

### 整體架構

```mermaid
%%{init: {'theme':'base', 'themeVariables': { 'primaryColor':'#1e3a5f', 'primaryTextColor':'#e0e0e0', 'primaryBorderColor':'#4a90e2', 'lineColor':'#4a90e2', 'secondaryColor':'#2d5016', 'tertiaryColor':'#5d3a1a', 'background':'#0d1117', 'mainBkg':'#1e3a5f', 'secondBkg':'#2d5016', 'tertiaryBkg':'#5d3a1a', 'textColor':'#e0e0e0', 'border1':'#4a90e2', 'border2':'#6aa84f', 'fontSize':'16px'}}}%%
graph TB
    subgraph Client["客戶端層"]
        ClientApp[客戶端應用]
        Agent[Agent<br/>客戶端代理]
        AgentPool[AgentPool<br/>代理池]
    end

    subgraph Router["路由閘道層 - Router"]
        Session[Session 端點<br/>客戶端連線]
        Registry[Registry 端點<br/>服務註冊]
        SessionHub[SessionHub<br/>會話中心]
        SessionCoordinator[SessionCoordinator<br/>會話協調器]
    end

    subgraph RegistryLayer["註冊層"]
        Reg1[Registry 1<br/>Group ID = 1]
        Reg2[Registry 2<br/>Group ID = 2]
    end

    subgraph GameServices["遊戲服務層"]
        GS1[Game Service 1<br/>IMethodable1]
        GS2[Game Service 2<br/>IMethodable2]
    end

    ClientApp --> Agent
    Agent --> AgentPool
    AgentPool --> Session
    Session --> SessionHub
    SessionHub --> SessionCoordinator

    Reg1 --> Registry
    Reg2 --> Registry
    Registry --> SessionCoordinator

    SessionCoordinator -.路由決策.-> Reg1
    SessionCoordinator -.路由決策.-> Reg2

    Reg1 --> GS1
    Reg2 --> GS2

    AgentPool -.遊戲通訊.-> GS1
    AgentPool -.遊戲通訊.-> GS2

    classDef routerStyle fill:#1e3a5f,stroke:#4a90e2,stroke-width:2px,color:#e0e0e0
    classDef clientStyle fill:#5d1e3a,stroke:#e24a90,stroke-width:2px,color:#e0e0e0
    classDef registryStyle fill:#2d5016,stroke:#6aa84f,stroke-width:2px,color:#e0e0e0
    classDef serviceStyle fill:#5d3a1a,stroke:#e2a84a,stroke-width:2px,color:#e0e0e0

    class ClientApp,Agent,AgentPool clientStyle
    class Session,Registry,SessionHub,SessionCoordinator routerStyle
    class Reg1,Reg2 registryStyle
    class GS1,GS2 serviceStyle
```

### 組件詳細架構

```mermaid
%%{init: {'theme':'base', 'themeVariables': { 'primaryColor':'#1e3a5f', 'primaryTextColor':'#e0e0e0', 'primaryBorderColor':'#4a90e2', 'lineColor':'#4a90e2', 'background':'#0d1117', 'mainBkg':'#1e3a5f', 'secondBkg':'#2d5016', 'tertiaryBkg':'#5d3a1a', 'textColor':'#e0e0e0', 'fontSize':'14px', 'nodeBorder':'#4a90e2', 'clusterBkg':'#161b22', 'clusterBorder':'#30363d', 'titleColor':'#e0e0e0'}}}%%
classDiagram
    class Router {
        +IService Registry
        +IService Session
        -SessionHub _Hub
        -Registrys.Server _Registry
        +Router(ISessionSelectionStrategy)
        +Dispose()
    }

    class SessionHub {
        +IService Source
        +IServiceRegistry Sink
        -SessionCoordinator _sessionCoordinator
        -ClientEntry _clientEntry
        +SessionHub(ISessionSelectionStrategy)
    }

    class SessionCoordinator {
        -ISessionSelectionStrategy _strategy
        -Dictionary~uint, List~ILineAllocatable~~ _allocatorsByGroup
        -Dictionary~IRoutableSession, SessionState~ _sessions
        +Register(uint group, ILineAllocatable)
        +Unregister(uint group, ILineAllocatable)
        +Join(IRoutableSession)
        +Leave(IRoutableSession)
    }

    class ClientEntry {
        -ISessionMembership _sessionMembership
        -Dictionary~IBinder, User~ _Users
        +RegisterClientBinder(IBinder)
        +UnregisterClientBinder(IBinder)
    }

    class Registry {
        +IAgent Agent
        +IListenable Listener
        -Registrys.Server _server
        +Registry(uint group)
        +Dispose()
    }

    class Agent {
        -AgentPool _pool
        +QueryNotifier~T~()
        +HandleMessage()
        +HandlePackets()
        +Connect(IService)
    }

    class AgentPool {
        +IAgent Agent
        +Notifier~IAgent~ Agents
        -IProtocol _gameProtocol
        -List~AgentSession~ _sessions
        +AgentPool(IProtocol)
        +Dispose()
    }

    Router --> SessionHub : 包含
    SessionHub --> SessionCoordinator : 使用
    SessionHub --> ClientEntry : 使用
    Registry --> Agent : 使用
    Agent --> AgentPool : 使用
    SessionCoordinator ..|> IServiceRegistry : 實作
    SessionCoordinator ..|> ISessionMembership : 實作
```


## 時序圖

### 啟動與註冊流程

```mermaid
%%{init: {'theme':'base', 'themeVariables': { 'primaryColor':'#1e3a5f', 'primaryTextColor':'#e0e0e0', 'primaryBorderColor':'#4a90e2', 'lineColor':'#58a6ff', 'secondaryColor':'#2d5016', 'tertiaryColor':'#5d3a1a', 'background':'#0d1117', 'mainBkg':'#1e3a5f', 'secondBkg':'#2d5016', 'tertiaryBkg':'#5d3a1a', 'textColor':'#e0e0e0', 'fontSize':'14px', 'actorBkg':'#1e3a5f', 'actorBorder':'#4a90e2', 'actorTextColor':'#e0e0e0', 'actorLineColor':'#4a90e2', 'signalColor':'#e0e0e0', 'signalTextColor':'#e0e0e0', 'labelBoxBkgColor':'#21262d', 'labelBoxBorderColor':'#30363d', 'labelTextColor':'#e0e0e0', 'loopTextColor':'#e0e0e0', 'noteBorderColor':'#30363d', 'noteBkgColor':'#161b22', 'noteTextColor':'#e0e0e0', 'activationBorderColor':'#58a6ff', 'activationBkgColor':'#1e3a5f', 'sequenceNumberColor':'#0d1117'}}}%%
sequenceDiagram
    participant Router
    participant SessionCoordinator
    participant Registry1 as Registry 1<br/>(Group 1)
    participant Registry2 as Registry 2<br/>(Group 2)
    participant GameService1 as Game Service 1
    participant GameService2 as Game Service 2

    Note over Router: 1. 建立 Router
    Router->>SessionCoordinator: 創建 SessionCoordinator<br/>(RoundRobin 策略)

    Note over GameService1,GameService2: 2. 建立遊戲服務
    GameService1->>GameService1: 創建 Game Service 1
    GameService2->>GameService2: 創建 Game Service 2

    Note over Registry1,Registry2: 3. 建立 Registry
    Registry1->>Registry1: 創建 Registry(group=1)
    Registry2->>Registry2: 創建 Registry(group=2)

    Note over Registry1,GameService1: 4. 綁定遊戲服務到 Listener
    Registry1->>GameService1: Listener.StreamableEnterEvent += Join
    Registry1->>GameService1: Listener.StreamableLeaveEvent += Leave
    Registry2->>GameService2: Listener.StreamableEnterEvent += Join
    Registry2->>GameService2: Listener.StreamableLeaveEvent += Leave

    Note over Registry1,Router: 5. Registry 向 Router 註冊
    Registry1->>Router: Agent.Connect(router.Registry)
    Router->>SessionCoordinator: Register(group=1, allocatable)
    SessionCoordinator->>SessionCoordinator: 將 Registry1 加入 Group 1

    Registry2->>Router: Agent.Connect(router.Registry)
    Router->>SessionCoordinator: Register(group=2, allocatable)
    SessionCoordinator->>SessionCoordinator: 將 Registry2 加入 Group 2
```

### 客戶端連線與通訊流程

```mermaid
%%{init: {'theme':'base', 'themeVariables': { 'primaryColor':'#1e3a5f', 'primaryTextColor':'#e0e0e0', 'primaryBorderColor':'#4a90e2', 'lineColor':'#58a6ff', 'secondaryColor':'#2d5016', 'tertiaryColor':'#5d3a1a', 'background':'#0d1117', 'mainBkg':'#1e3a5f', 'secondBkg':'#2d5016', 'tertiaryBkg':'#5d3a1a', 'textColor':'#e0e0e0', 'fontSize':'14px', 'actorBkg':'#1e3a5f', 'actorBorder':'#4a90e2', 'actorTextColor':'#e0e0e0', 'actorLineColor':'#4a90e2', 'signalColor':'#e0e0e0', 'signalTextColor':'#e0e0e0', 'labelBoxBkgColor':'#21262d', 'labelBoxBorderColor':'#30363d', 'labelTextColor':'#e0e0e0', 'loopTextColor':'#e0e0e0', 'noteBorderColor':'#30363d', 'noteBkgColor':'#161b22', 'noteTextColor':'#e0e0e0', 'activationBorderColor':'#58a6ff', 'activationBkgColor':'#1e3a5f', 'sequenceNumberColor':'#0d1117'}}}%%
sequenceDiagram
    participant Client as 客戶端應用
    participant Agent
    participant AgentPool
    participant Router
    participant SessionCoordinator
    participant Registry1 as Registry 1
    participant Registry2 as Registry 2
    participant GS1 as Game Service 1
    participant GS2 as Game Service 2

    Note over Client,AgentPool: 1. 建立客戶端
    Client->>AgentPool: 創建 AgentPool(gameProtocol)
    Client->>Agent: 創建 Agent(agentPool)

    Note over Agent,Router: 2. 連接到 Router
    Agent->>Router: Connect(router.Session)
    Router->>SessionCoordinator: Join(session)

    Note over SessionCoordinator: 3. Router 進行路由決策
    SessionCoordinator->>SessionCoordinator: 查找所有 Group
    SessionCoordinator->>SessionCoordinator: 使用 RoundRobin 策略選擇

    SessionCoordinator->>Registry1: Alloc() - 分配連線
    Registry1-->>SessionCoordinator: 返回 IStreamable
    SessionCoordinator->>Agent: Set(group=1, stream)

    SessionCoordinator->>Registry2: Alloc() - 分配連線
    Registry2-->>SessionCoordinator: 返回 IStreamable
    SessionCoordinator->>Agent: Set(group=2, stream)

    Note over AgentPool: 4. AgentPool 處理連線
    AgentPool->>AgentPool: OnConnectionSupply(stream1)
    AgentPool->>GS1: 創建 Agent 並連接到 GS1

    AgentPool->>AgentPool: OnConnectionSupply(stream2)
    AgentPool->>GS2: 創建 Agent 並連接到 GS2

    Note over Client,GS1: 5. 客戶端呼叫遊戲服務
    Client->>Agent: QueryNotifier<IMethodable1>()
    Agent-->>Client: Supply(service1)
    Client->>GS1: service1.GetValue1()
    GS1-->>Client: 返回結果 1

    Client->>Agent: QueryNotifier<IMethodable2>()
    Agent-->>Client: Supply(service2)
    Client->>GS2: service2.GetValue2()
    GS2-->>Client: 返回結果 2
```

### 斷線處理流程

```mermaid
%%{init: {'theme':'base', 'themeVariables': { 'primaryColor':'#1e3a5f', 'primaryTextColor':'#e0e0e0', 'primaryBorderColor':'#4a90e2', 'lineColor':'#58a6ff', 'secondaryColor':'#2d5016', 'tertiaryColor':'#5d3a1a', 'background':'#0d1117', 'mainBkg':'#1e3a5f', 'secondBkg':'#2d5016', 'tertiaryBkg':'#5d3a1a', 'textColor':'#e0e0e0', 'fontSize':'14px', 'actorBkg':'#1e3a5f', 'actorBorder':'#4a90e2', 'actorTextColor':'#e0e0e0', 'actorLineColor':'#4a90e2', 'signalColor':'#e0e0e0', 'signalTextColor':'#e0e0e0', 'labelBoxBkgColor':'#21262d', 'labelBoxBorderColor':'#30363d', 'labelTextColor':'#e0e0e0', 'loopTextColor':'#e0e0e0', 'noteBorderColor':'#30363d', 'noteBkgColor':'#161b22', 'noteTextColor':'#e0e0e0', 'activationBorderColor':'#58a6ff', 'activationBkgColor':'#1e3a5f', 'sequenceNumberColor':'#0d1117', 'altBackground':'#161b22'}}}%%
sequenceDiagram
    participant Agent
    participant Router
    participant SessionCoordinator
    participant Registry1 as Registry 1
    participant Registry2 as Registry 2
    participant GS1 as Game Service 1
    participant GS2 as Game Service 2

    Note over Agent: 客戶端斷線
    Agent->>Router: Disconnect
    Router->>SessionCoordinator: Leave(session)

    Note over SessionCoordinator: 清理所有分配的連線
    SessionCoordinator->>SessionCoordinator: 查找 session 的所有 Allocation

    SessionCoordinator->>Registry1: Free(stream1)
    Registry1->>GS1: StreamableLeaveEvent
    GS1->>GS1: 清理客戶端資源

    SessionCoordinator->>Registry2: Free(stream2)
    Registry2->>GS2: StreamableLeaveEvent
    GS2->>GS2: 清理客戶端資源

    SessionCoordinator->>SessionCoordinator: 移除 session

    Note over Registry1: 或者 Registry 斷線
    Registry1->>Router: Agent.Disconnect
    Router->>SessionCoordinator: Unregister(group=1, allocatable)

    SessionCoordinator->>SessionCoordinator: 查找使用此 Registry 的 sessions
    SessionCoordinator->>SessionCoordinator: 嘗試重新分配到同 Group 的其他 Registry

    alt 有其他可用的 Registry
        SessionCoordinator->>Registry2: Alloc() - 重新分配
        SessionCoordinator->>Agent: Set(group=1, new_stream)
    else 沒有其他可用的 Registry
        SessionCoordinator->>SessionCoordinator: 移除該 Group 的分配
        SessionCoordinator->>Agent: Unset(group=1)
    end
```

## 快速開始

### 1. 安裝

透過 NuGet 安裝套件：

```bash
dotnet add package PinionCore.Remote.Gateway
```

### 2. 建立 Router

```csharp
using PinionCore.Remote.Gateway;

// 建立 Router
using var router = new Router();

// router.Registry - 供遊戲服務註冊使用
// router.Session - 供客戶端連接使用
```

### 3. 建立遊戲服務與 Registry

```csharp
using PinionCore.Remote.Gateway;
using PinionCore.Remote.Soul;

// 建立遊戲服務
public class MyGameEntry : IEntry
{
    void IBinderProvider.RegisterClientBinder(IBinder binder)
    {
        // 綁定遊戲服務介面
        binder.Bind<IMyGameService>(this);
    }

    void IBinderProvider.UnregisterClientBinder(IBinder binder)
    {
        // 清理
    }

    void IEntry.Update()
    {
        // 遊戲邏輯更新
    }
}

// 建立服務
var gameEntry = new MyGameEntry();
var gameService = PinionCore.Remote.Standalone.Provider.CreateService(
    gameEntry,
    protocol
);

// 建立 Registry (使用 Group ID = 1)
var registry = new Registry(1);

// 啟動 Agent Worker (處理訊息)
var registryWorker = new AgentWorker(registry.Agent);

// 綁定遊戲服務到 Listener
registry.Listener.StreamableEnterEvent += gameService.Join;
registry.Listener.StreamableLeaveEvent += gameService.Leave;

// 連接到 Router
registry.Agent.Connect(router.Registry);
```

### 4. 建立客戶端

```csharp
using PinionCore.Remote.Gateway;
using PinionCore.Remote.Gateway.Hosts;

// 建立 Agent (需要提供遊戲協議)
var agent = new Agent(new AgentPool(gameProtocol));

// 啟動 Agent Worker
var agentWorker = new AgentWorker(agent);

// 連接到 Router
agent.Connect(router.Session);

// 使用 Agent 查詢遊戲服務
var notifier = agent.QueryNotifier<IMyGameService>();

// 監聽服務供應
notifier.Supply += (service) =>
{
    // 可以開始使用 service
    var result = await service.GetData().RemoteValue();
};

// 處理訊息 (在遊戲迴圈中)
agent.HandleMessage();
agent.HandlePackets();
```

## 完整範例

參考測試檔案 `PinionCore.Remote.Gateway.Test/Tests.cs` 中的 `GatewayRegistryAgentIntegrationTestAsync` 方法，這是一個完整的使用範例，展示了：

1. 如何建立 Router
2. 如何建立多個遊戲服務
3. 如何建立多個 Registry 並註冊到 Router
4. 如何建立客戶端並同時與多個遊戲服務通訊

## API 說明

### Router

```csharp
public class Router : IDisposable
{
    // 供遊戲服務註冊使用的端點
    public readonly IService Registry;

    // 供客戶端連接使用的端點
    public readonly IService Session;

    public Router(ISessionSelectionStrategy strategy);
    public void Dispose();
}
```

### Registry

```csharp
public class Registry : IDisposable
{
    // 用於連接到 Router 的 Agent
    public readonly IAgent Agent;

    // 供遊戲服務監聽玩家連線的 Listener
    public readonly IListenable Listener;

    // group - 用於 Router 路由決策的群組 ID
    public Registry(uint group);

    public void Dispose();
}
```

### Agent

```csharp
public class Agent : IAgent
{
    // 建立 Agent
    // pool - AgentPool，用於管理多個遊戲服務的連線
    public Agent(AgentPool pool);

    // 查詢遊戲服務介面的 Notifier
    INotifier<T> QueryNotifier<T>();

    // 處理訊息 (需在遊戲迴圈中定期呼叫)
    void HandleMessage();
    void HandlePackets();

    // 連接/斷線
    void Enable(IStreamable streamable);
    void Disable();
}
```

### AgentPool

```csharp
public class AgentPool : IDisposable
{
    // 內部 Agent，用於連接到 Router
    public IAgent Agent { get; }

    // 遊戲服務的 Agent 集合
    public Notifier<IAgent> Agents { get; }

    // gameProtocol - 遊戲協議
    public AgentPool(IProtocol gameProtocol);

    public void Dispose();
}
```

## 進階主題

### 自訂路由策略

Router 預設使用 `RoundRobinSelector` 進行路由，您可以實作 `ISessionSelectionStrategy` 介面來自訂路由邏輯：

```csharp
public interface ISessionSelectionStrategy
{
    IEnumerable<Registrys.ILineAllocatable> OrderAllocators(uint group, IReadOnlyList<Registrys.ILineAllocatable> allocators);
}
```

範例：

```csharp
public class CustomStrategy : ISessionSelectionStrategy
{
    public IEnumerable<Registrys.ILineAllocatable> OrderAllocators(uint group, IReadOnlyList<Registrys.ILineAllocatable> allocators)
    {
        // 自訂選擇邏輯，例如：
        // - 基於負載
        // - 基於地理位置
        // - 基於玩家偏好
        return allocators.OrderBy(a => a.AllocatedCount);
    }
}

// 使用自訂策略
var router = new Router(new CustomStrategy());
```

### Group ID 的使用

Group ID 是一個重要的概念，用於區分不同的遊戲服務類型或分區：

- **相同 Group ID**: 代表相同類型的服務，Router 會使用策略選擇其中一個
- **不同 Group ID**: 代表不同類型的服務，Router 會將客戶端路由到所有 Group

範例：
```csharp
// 服務類型 A (Group 1)
var registryA = new Registry(1);

// 服務類型 B (Group 2)
var registryB = new Registry(2);

// 客戶端連接後，會同時與類型 A 服務和類型 B 服務建立連線
```

### 協議版本管理

Gateway 支援多個協議版本同時運行，這對於需要逐步升級的系統特別重要：

#### 版本隔離機制

Router 會根據 `IProtocol.VersionCode` 自動隔離不同版本的客戶端與服務：

```csharp
// 舊版本服務
var oldProtocol = OldProtocolCreator.Create(); // VersionCode = [1, 0, 0]
var registryV1 = new Registry(oldProtocol, groupId: 1);

// 新版本服務
var newProtocol = NewProtocolCreator.Create(); // VersionCode = [2, 0, 0]
var registryV2 = new Registry(newProtocol, groupId: 1);

// 兩者可同時向同一個 Router 註冊
registryV1.Agent.Connect(router.Registry);
registryV2.Agent.Connect(router.Registry);

// 使用舊版本協議的客戶端只會路由到 registryV1
// 使用新版本協議的客戶端只會路由到 registryV2
```

#### 版本升級策略

1. **藍綠部署**：
   - 同時部署新舊版本服務
   - 逐步將客戶端升級到新版本
   - 確認穩定後移除舊版本服務

2. **金絲雀發布**：
   - 新版本服務使用不同的 Group ID
   - 部分客戶端連接到新版本進行測試
   - 驗證無誤後全面切換

3. **版本相容性檢查**：
   ```csharp
   // 在 Registry 建立時指定協議版本
   var protocol = ProtocolCreator.Create();
   var registry = new Registry(protocol, groupId: 1);

   // Router 會自動使用 protocol.VersionCode 進行版本隔離
   // 確保只有相同版本的客戶端能連接到對應的服務
   ```

### 使用 Reactive Extensions (Rx)

Gateway 整合了 Reactive Extensions，讓您可以使用流式處理方式處理遊戲服務：

```csharp
// 使用 LINQ 查詢遊戲服務
var observable = from service in agent.QueryNotifier<IMyService>().SupplyEvent()
                 from result in service.GetData().RemoteValue()
                 select result;

var data = await observable.FirstAsync();
```

### 網路模式 vs 單機模式

範例使用的是 Standalone 模式（單機模式），適合開發和測試。生產環境應使用網路模式：

```csharp
// 網路模式 - Server
var service = Provider.CreateTcpService(entry, protocol, port);

// 網路模式 - Client
var agent = Provider.CreateTcpAgent(protocol);
agent.Connect(host, port);
```

## 注意事項

1. **Worker 的重要性**: Registry 和 Agent 都需要使用 `AgentWorker` 來處理訊息，確保定期呼叫 `HandleMessage()` 和 `HandlePackets()`

2. **資源釋放**: 所有組件都實作了 `IDisposable`，請確保適當釋放資源

3. **Group ID 規劃**: 合理規劃 Group ID 可以讓路由更有效率，避免不必要的連線

4. **策略選擇**: 選擇適合的路由策略可以提升系統效能和玩家體驗

5. **錯誤處理**: 監聽 Agent 的錯誤事件以處理網路異常：
   ```csharp
   agent.ExceptionEvent += (ex) => Console.WriteLine($"Error: {ex}");
   agent.ErrorMethodEvent += (method, msg) => Console.WriteLine($"Method Error: {method} - {msg}");
   ```

## 相關資源

- [PinionCore.Remote 核心文件](../README.md)
- [Protocol 程式碼產生器](../PinionCore.Remote.Tools.Protocol.Sources/README.md)
- [範例專案](../PinionCore.Samples.HelloWorld.Client/README.md)

## 授權

MIT License
