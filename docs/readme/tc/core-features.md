# 核心特色

[上一節：簡介](introduction.md) | [下一節：架構與模組](architecture.md)

## 1. 介面導向通訊

你只需要定義介面，不需要手寫序列化或協議解析：

```csharp
public interface IGreeter
{
    PinionCore.Remote.Value<HelloReply> SayHello(HelloRequest request);
}
```

伺服器實作這個介面：

```csharp
class Greeter : IGreeter
{
    PinionCore.Remote.Value<HelloReply> IGreeter.SayHello(HelloRequest request)
    {
        return new HelloReply { Message = $"Hello {request.Name}." };
    }
}
```

客戶端透過 `QueryNotifier<IGreeter>()` 拿到遠端代理，像本地物件一樣呼叫：

```csharp
agent.QueryNotifier<IGreeter>().Supply += greeter =>
{
    var request = new HelloRequest { Name = "you" };
    greeter.SayHello(request).OnValue += reply =>
    {
        Console.WriteLine($"Receive message: {reply.Message}");
    };
};
```

- `Value<T>` 可以 `await`，也可以透過 `OnValue` 事件取得結果。
- 你不需要處理任何連線 ID 或 RPC ID，只要跟著介面走即可。

## 2. 可控的生命週期（Entry / Session / Soul）

伺服器入口實作 `PinionCore.Remote.IEntry`，在連線建立/關閉時由框架呼叫：

```csharp
public class Entry : PinionCore.Remote.IEntry
{
    private readonly Greeter _greeter = new Greeter();

    void PinionCore.Remote.ISessionObserver.OnSessionOpened(PinionCore.Remote.ISessionBinder binder)
    {
        // 客戶端連線 — 綁定 _greeter
        var soul = binder.Bind<IGreeter>(_greeter);

        // 若之後要解除綁定：
        // binder.Unbind(soul);
    }

    void PinionCore.Remote.ISessionObserver.OnSessionClosed(PinionCore.Remote.ISessionBinder binder)
    {
        // 客戶端斷線時需要的清理
    }

    void PinionCore.Remote.IEntry.Update()
    {
        // 伺服器更新迴圈（可選）
    }
}
```

## 3. Value / Property / Notifier 支援

### Value<T>：一次性非同步呼叫

- 行為類似 `Task<T>`
- 用於 Request/Response 流程
- 只設定一次，可 `await` 或透過 `OnValue` 取得

```csharp
Value<LoginResult> Login(LoginRequest request);
```

### Property<T>：持續的遠端狀態

- 伺服器維護實際值
- 客戶端在值變更時收到更新
- 適合玩家名稱、房間標題、伺服器版本等

```csharp
Property<string> Nickname { get; }
Property<string> RoomName { get; }
```

### Notifier<T>：動態遠端物件集合

描述巢狀結構或動態集合：

```csharp
public interface IChatEntry
{
    INotifier<IRoom> Rooms { get; }
}

public interface IRoom
{
    Property<string> Name { get; }
    INotifier<IPlayer> Players { get; }
}

public interface IPlayer
{
    Property<string> Nickname { get; }
}
```

伺服器端：

- 新增 → `Rooms.Supply(roomImpl)`
- 移除 → `Rooms.Unsupply(roomImpl)`
- 玩家加入 → `room.Players.Supply(playerImpl)`
- 玩家離開 → `room.Players.Unsupply(playerImpl)`

客戶端：

```csharp
agent.QueryNotifier<IRoom>().Supply += room =>
{
    room.Players.Supply += player =>
    {
        Console.WriteLine($"Player joined: {player.Nickname.Value}");
    };
};
```

重點：

- Notifier = 動態集合 + 遠端物件樹同步
- 客戶端不需管理 ID，依介面階層即可

## 4. 響應式方法支援（Reactive）

`PinionCore.Remote.Reactive` 提供 Rx 擴充，讓你用 `IObservable<T>` 組合遠端流程。

常用擴充（位於 `PinionCore.Remote.Reactive.Extensions`）：

- `RemoteValue()` — 把 `Value<T>` 轉成 `IObservable<T>`
- `SupplyEvent()` / `UnsupplyEvent()` — 把 `INotifier<T>` 事件轉成 Observable

範例（節錄自 `PinionCore.Integration.Tests/SampleTests.cs`）：

```csharp
var cts = new CancellationTokenSource();
var runTask = Task.Run(async () =>
{
    while (!cts.Token.IsCancellationRequested)
    {
        proxy.Agent.HandlePackets();
        proxy.Agent.HandleMessages();
        await Task.Delay(1, cts.Token);
    }
}, cts.Token);

var echoObs =
    from e in proxy.Agent
        .QueryNotifier<Echoable>()
        .SupplyEvent()
    from val in e.Echo().RemoteValue()
    select val;

var echoValue = await echoObs.FirstAsync();

cts.Cancel();
await runTask;
```

注意：

- 即使用 Rx，仍需要背景迴圈呼叫 `HandlePackets()` / `HandleMessages()`。
- Rx 讓流程更容易組合，但不會取代底層訊息處理。

## 5. 簡易的公開與私有介面支援

因為是介面導向，伺服器可以根據權限綁定不同介面，達成 Public/Private API。

```csharp
public interface IPublicService
{
    Value<string> GetPublicData();
}

public interface IPrivateService : IPublicService
{
    Value<string> GetPrivateData();
}

class ServiceImpl : IPrivateService
{
    public Value<string> GetPublicData() => "This is public data.";
    public Value<string> GetPrivateData() => "This is private data.";
}
```

伺服器端：

```csharp
void ISessionObserver.OnSessionOpened(ISessionBinder binder)
{
    var serviceImpl = new ServiceImpl();

    if (IsAuthenticatedClient(binder))
    {
        binder.Bind<IPrivateService>(serviceImpl); // 已驗證
    }

    binder.Bind<IPublicService>(serviceImpl);      // 所有人都能取得
}
```

- 未驗證 → **只有 IPublicService**
- 已驗證 → **IPublicService + IPrivateService**

## 6. 多傳輸模式與 Standalone

- `PinionCore.Remote.Server.Tcp.ListeningEndpoint`
- `PinionCore.Remote.Client.Tcp.ConnectingEndpoint`
- `PinionCore.Remote.Server.Web.ListeningEndpoint`
- `PinionCore.Remote.Client.Web.ConnectingEndpoint`
- `PinionCore.Remote.Standalone.ListeningEndpoint`（同時扮演 Server/Client，適合測試）

整合測試 (`SampleTests`) 會同時跑三種傳輸並驗證行為一致。

## 7. Gateway 閘道服務

`PinionCore.Remote.Gateway` 作為多服務統一入口，提供：

- **多服務路由**：集中入口分發到 Chat/Game/Auth 等後端
- **版本共存**：支援多版本 `IProtocol.VersionCode` 平滑升級
- **負載平衡**：`LineAllocator` 管理服務實例分配
- **服務隔離**：各服務可獨立部署與擴充

```mermaid
graph TB
    subgraph Clients["Client Layer"]
        C1[Client v1.0]
        C2[Client v1.1]
        C3[Client v2.0]
    end

    subgraph Gateway["Gateway Layer"]
        GW[Gateway<br/>Unified Entry]
        VA[Version Adapter<br/>Version Matching]
        LB[Line Allocator<br/>Load Balancing]
    end

    subgraph Services["Service Layer"]
        S1[Chat Service v1]
        S2[Chat Service v2]
        S3[Game Service]
        S4[Auth Service]
    end

    C1 --> GW
    C2 --> GW
    C3 --> GW

    GW --> VA
    VA --> LB

    LB --> S1
    LB --> S2
    LB --> S3
    LB --> S4
```

**核心組件**：

- Router：依協議版本、服務類型路由
- LineAllocator：負載平衡與容錯
- Version Adapter：讓不同版本客戶端共存、升級不中斷

**使用場景**：

- 同時運行多個獨立服務（微服務）
- 協議版本管理與平滑升級
- 橫向擴展與負載平衡
- 統一的連線管理與監控入口

更多說明與範例：`PinionCore.Remote.Gateway/README.md`、`PinionCore.Consoles.Chat1.*`。
