# PinionCore Remote
[![Maintainability](https://api.codeclimate.com/v1/badges/89c3a646f9daff42a38e/maintainability)](https://codeclimate.com/github/jiowchern/PinionCore.Remote/maintainability)
[![Build](https://github.com/jiowchern/PinionCore.Remote/actions/workflows/dotnet-desktop.yml/badge.svg?branch=master)](https://github.com/jiowchern/PinionCore.Remote/actions/workflows/dotnet-desktop.yml)
[![Coverage Status](https://coveralls.io/repos/github/jiowchern/PinionCore.Remote/badge.svg?branch=master)](https://coveralls.io/github/jiowchern/PinionCore.Remote?branch=master)
![commit last date](https://img.shields.io/github/last-commit/jiowchern/PinionCore.Remote)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/jiowchern/PinionCore.Remote)  
[Ask OpenDeepWiki](https://opendeep.wiki/jiowchern/PinionCore.Remote/introduction?branch=master)


## 簡介

PinionCore Remote 是一個以 C# 開發的物件導向遠端通訊框架。
你可以用「介面」定義通訊協議，伺服器實作這些介面，客戶端像呼叫本地物件一樣呼叫，實際資料透過 TCP / WebSocket / 單機模擬等管道傳輸。

- 支援 .NET Standard 2.1（.NET 6/7/8、Unity 2021+）
- 支援 IL2CPP 與 AOT（需預先註冊序列化型別）
- 內建 TCP、WebSocket 與 Standalone 單機模式
- 透過 Source Generator 自動產生 `IProtocol` 實作，降低維護成本

## 核心特色

### 1. 介面導向通訊

只需要定義介面，不需要手寫序列化與協議解析：

```csharp
public interface IGreeter
{
    PinionCore.Remote.Value<HelloReply> SayHello(HelloRequest request);
}
```
伺服器實作介面：
```csharp
class Greeter : IGreeter
{
    PinionCore.Remote.Value<HelloReply> IGreeter.SayHello(HelloRequest request)
    {
        return new HelloReply { Message = $"Hello {request.Name}." };
    }
}
```
客戶端透過 QueryNotifier<IGreeter>() 拿到遠端代理，直接呼叫 SayHello，回傳 Value<T> 可以 await。
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

### 2. 可控的生命週期（Entry / Session / Soul）

伺服器入口實作 PinionCore.Remote.IEntry，在連線建立時收到 ISessionBinder，由你決定何時綁定/解除綁定介面：
```csharp
public class Entry : PinionCore.Remote.IEntry
{
    private readonly Greeter _greeter = new Greeter();

    void PinionCore.Remote.ISessionObserver.OnSessionOpened(PinionCore.Remote.ISessionBinder binder)
    {
        // 客戶端連線成功，綁定 _greeter
        var soul = binder.Bind<IGreeter>(_greeter);


        // 若要解除綁定可呼叫這行
        binder.Unbind(soul);
    }

    void PinionCore.Remote.ISessionObserver.OnSessionClosed(PinionCore.Remote.ISessionBinder binder)
    {
        // 客戶端斷線時要做的清理        
    }

    void PinionCore.Remote.IEntry.Update()
    {
        // 每迴圈更新（可為空，視需求而定）
    }
}
```
伺服器端使用 Host 建立服務（Host 繼承自 Soul.Service，內部透過 SessionEngine 管理所有連線與 Session）：`new PinionCore.Remote.Server.Host(entry, protocol)`。


### 3. Value / Property / Notifier 支援

PinionCore.Remote 以「介面」為中心，提供三種常用成員型別來描述遠端行為與狀態：

- **Value\<T>**：描述「一次性非同步呼叫」  
  - 用於方法回傳值（類似 Task\<T> 的概念）  
  - 適合請求 / 回應流程，例如：登入、取得設定、送出指令等  
  - 呼叫端只需等待結果，不需維護長期狀態

- **Property**：描述「穩定存在的遠端狀態」  
  - 介面上的屬性會由伺服器端實作，客戶端透過代理讀取  
  - 適合表示較穩定的資訊，例如：玩家名稱、房間標題、伺服器版本等  
  - 搭配事件或 Notifier，可以在狀態變化時通知客戶端更新 UI

- **Notifier\<T>：支援巢狀介面與物件樹同步的動態集合**  
  `INotifier<T>` 用來表示「一組動態存在的遠端物件」。  
  特別的是，`T` 不只可以是基礎型別，更可以是**介面本身**，因此可以自然地描述**巢狀物件結構（物件樹）**，並在伺服器與客戶端之間同步這棵樹的生命週期。

  典型場景是 Lobby / Room / Player 等分層結構：

  ```csharp
  public interface IChatEntry
  {
      // 目前所有房間的動態列表
      INotifier<IRoom> Rooms { get; }
  }

  public interface IRoom
  {
      Property<string> Name { get; }
      // 房間內玩家的動態列表
      INotifier<IPlayer> Players { get; }
  }

  public interface IPlayer
  {
      Property<string> Nickname { get; }
  }
  ```

  - 伺服器端維護實際的房間與玩家物件，並對應到 `INotifier<IRoom>`、`INotifier<IPlayer>`  
    - 房間建立時，由伺服器「供應（Supply）」一個 `IRoom` 實例給 `Rooms`  
    - 房間刪除時，將該 `IRoom` 從 `Rooms` 中移除  
    - 玩家進出房間時，針對該 `IRoom.Players` 供應 / 移除對應的 `IPlayer`  
  - 客戶端只需透過介面拿到 `INotifier<IRoom>`，就能：  
    - 自動收到「房間新增 / 移除」通知  
    - 對每個房間，再繼續訂閱 `room.Players`，自動收到「玩家進入 / 離開」通知  
    - 取得的 `IRoom` / `IPlayer` 都是遠端代理，直接呼叫其介面成員即可

  透過這種設計，Notifier 不只是「事件的集合」，而是：

  - 用來描述「會動的集合」與「會變化的物件樹」  
  - 支援介面巢狀：`INotifier<IRoom>` → `IRoom` 內再有 `INotifier<IPlayer>` → 甚至更深層的子模組  
  - 客戶端不需要管理任何 id 或查表邏輯，只要依照介面層級存取，即可自動追蹤伺服器端物件的產生與銷毀

  **總結**：  
  - `Value<T>`：一次性呼叫結果  
  - `Property`：穩定狀態值  
  - `Notifier<T>`：同步「會增減的物件集合」，並支援以介面為節點的巢狀物件樹，是 PinionCore.Remote 用來表達複雜遠端結構的核心能力。

#### Notifier 供應 / 移除流程概觀

以 `IChatEntry.Rooms` 為例，可以用以下流程理解 Notifier 的運作：

1. 伺服器啟動後，建立 `IRoom` 實作物件，並透過 `INotifier<IRoom>` 供應：
   - 當房間存在時呼叫 `Rooms.Supply(roomImpl)`
   - 當房間關閉時呼叫 `Rooms.Unsupply(roomImpl)`
2. 通訊層會將這些供應 / 移除事件轉送到每一個已連線的客戶端。
3. 客戶端透過 `agent.QueryNotifier<IRoom>()` 取得對應的 `INotifier<IRoom>` 代理，並訂閱：

   ```csharp
   agent.QueryNotifier<IRoom>().Supply += room =>
   {
       // 這裡的 room 已經是遠端代理，可以直接使用
       room.Players.Supply += player =>
       {
           // 處理玩家加入事件
       };
   };
   ```

4. 當伺服器端 Unsupply 物件時，客戶端會收到對應的 `Unsupply` 事件，並自動釋放該代理。

透過這個機制，伺服器只需管理真實物件的生命週期，客戶端就能自動維持一份同步的巢狀物件樹（Entry → Room → Player…）。

### 4. 響應式方法支援（Reactive）

PinionCore.Remote.Reactive 提供 Rx 擴充，用 IObservable<T> 串接遠端呼叫。

PinionCore.Remote.Reactive.Extensions 中重要的擴充方法：

- RemoteValue()：把 Value<T> 轉成 IObservable<T>
- SupplyEvent() / UnsupplyEvent()：把 Notifier 轉成 IObservable<T>

在整合測試 PinionCore.Integration.Tests/SampleTests.cs 中：
```csharp
// 重要：Rx 模式仍需要背景處理迴圈
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

// 建立 Rx 查詢鏈
var echoObs =
    from e in proxy.Agent
        .QueryNotifier<PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Echoable>()
        .SupplyEvent()
    from val in e.Echo().RemoteValue()
    select val;

var echoValue = await echoObs.FirstAsync();

// 停止背景處理
cts.Cancel();
await runTask;
```
這個例子同時示範：

- **背景處理迴圈是必須的**：即使使用 Rx，仍需持續呼叫 HandlePackets/HandleMessages
- 透過 Notifier 的 SupplyEvent() 等待伺服器供應介面
- 呼叫遠端方法 Echo() 回傳 Value<int>
- 用 RemoteValue() 轉成 IObservable<int>，再用 Rx 取得一次結果
### 5. 簡易的公開與私有介面支援
由於PinionCore.Remote採用介面導向設計，伺服器可以根據需求公開不同的介面給不同的客戶端。這使得實現公開與私有介面的需求變得簡單且直觀。
例如，可以定義一個公開介面 `IPublicService` 和一個私有介面 `IPrivateService`：
```csharp
public interface IPublicService  
{
    PinionCore.Remote.Value<string> GetPublicData();
}

public interface IPrivateService : IPublicService
{
    PinionCore.Remote.Value<string> GetPrivateData();
}

class ServiceImpl : IPrivateService
{
    public PinionCore.Remote.Value<string> GetPublicData()
    {
        return "This is public data.";
    }

    public PinionCore.Remote.Value<string> GetPrivateData()
    {
        return "This is private data.";
    }

}
```
伺服器可以根據客戶端的身份驗證狀態，決定綁定哪個介面：
```csharp
void ISessionObserver.OnSessionOpened(ISessionBinder binder)
{
    var serviceImpl = new ServiceImpl();
    if (IsAuthenticatedClient(binder))
    {
        // 綁定私有介面給已驗證的客戶端
        binder.Bind<IPrivateService>(serviceImpl);
    }

    // 綁定公開介面給未驗證的客戶端
    binder.Bind<IPublicService>(serviceImpl);
}
```
這樣，未經驗證的客戶端只能訪問 `IPublicService`，而已驗證的客戶端則可以訪問 `IPrivateService`，從而實現了介面的公開與私有控制。

### 6. 多傳輸模式與 Standalone

內建三種傳輸方式：

- TCP：PinionCore.Remote.Server.Tcp.ListeningEndpoint / PinionCore.Remote.Client.Tcp.ConnectingEndpoint
- WebSocket：PinionCore.Remote.Server.Web.ListeningEndpoint / PinionCore.Remote.Client.Web.ConnectingEndpoint
- Standalone：PinionCore.Remote.Standalone.ListeningEndpoint（同時實作 Server 與 Client 端點，用於單機模擬）

整合測試 SampleTests 同時啟動三種端點並逐一驗證，確保各模式行為一致。

---
## 架構與模組總覽

主要專案：

- PinionCore.Remote：核心介面與抽象（IEntry、ISessionBinder、Value<T>、Property<T>、Notifier<T> 等）
- PinionCore.Remote.Client：Proxy、IConnectingEndpoint 及連線擴充（AgentExtensions.Connect）
- PinionCore.Remote.Server：Host、IListeningEndpoint、ServiceExtensions.ListenAsync
- PinionCore.Remote.Soul：伺服器 Session 管理、更新迴圈（ServiceUpdateLoop）
- PinionCore.Remote.Ghost：客戶端 Agent 實作（User），封包編碼與處理
- PinionCore.Remote.Standalone：ListeningEndpoint 以記憶體流模擬 Server/Client
- PinionCore.Network：IStreamable、TCP/WebSocket Peer、封包讀寫
- PinionCore.Serialization：預設序列化實作與型別描述
- PinionCore.Remote.Tools.Protocol.Sources：Source Generator，透過 [PinionCore.Remote.Protocol.Creator] 自動產生 IProtocol
- PinionCore.Remote.Gateway：閘道與多服務路由（詳細見該模組 README）


---
## 快速開始（Hello World）

建議建立三個專案：Protocol、Server、Client。以下範例會對齊實際樣板 (PinionCore.Samples.HelloWorld.*) 的寫法。

### 環境需求

- .NET SDK 6 或以上
- Visual Studio 2022 / Rider / VS Code
- 若需 Unity，建議 Unity 2021 LTS 以上

### 1. Protocol 專案

建立 Class Library：

Sample/Protocol> dotnet new classlib

加入 NuGet 參考（版本請依實際發佈為準）：
```xml
<ItemGroup>
<PackageReference Include="PinionCore.Remote" Version="0.1.14.15" />
<PackageReference Include="PinionCore.Serialization" Version="0.1.14.12" />
<PackageReference Include="PinionCore.Remote.Tools.Protocol.Sources" Version="0.0.4.25">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
</ItemGroup>
```
定義資料與介面：
```csharp
namespace Protocol
{
    public struct HelloRequest
    {
        public string Name;
    }

    public struct HelloReply
    {
        public string Message;
    }

    public interface IGreeter
    {
        PinionCore.Remote.Value<HelloReply> SayHello(HelloRequest request);
    }
}
```
建立 ProtocolCreator（Source Generator 入口）：
```csharp
namespace Protocol
{
    public static partial class ProtocolCreator
    {
        public static PinionCore.Remote.IProtocol Create()
        {
            PinionCore.Remote.IProtocol protocol = null;
            _Create(ref protocol);
            return protocol;
        }

        [PinionCore.Remote.Protocol.Creator]
        static partial void _Create(ref PinionCore.Remote.IProtocol protocol);
    }
}
```
 
> 注意：被標記為 [PinionCore.Remote.Protocol.Creator] 的方法必須是
> static partial void Method(ref PinionCore.Remote.IProtocol)，否則無法編譯。
 
### 2. Server 專案

建立 Console 專案：

```Sample/Server> dotnet new console```

csproj 參考：
```xml
<ItemGroup>
<PackageReference Include="PinionCore.Remote.Server" Version="0.1.14.13" />
<ProjectReference Include="..\Protocol\Protocol.csproj" />
</ItemGroup>
```
實作 IGreeter（對齊 PinionCore.Samples.HelloWorld.Server/Greeter.cs）：
```csharp
using Protocol;

namespace Server
{
    class Greeter : IGreeter
    {
        PinionCore.Remote.Value<HelloReply> IGreeter.SayHello(HelloRequest request)
        {
            return new HelloReply { Message = $"Hello {request.Name}." };
        }
    }
}
```
實作 Entry（對齊 HelloWorld 範例）：
```csharp
using PinionCore.Remote;
using Protocol;

namespace Server
{
    class Entry : IEntry
    {
        public volatile bool Enable = true;

        private readonly Greeter _greeter = new Greeter();

        void ISessionObserver.OnSessionOpened(ISessionBinder binder)
        {
            // 客戶端連線成功，綁定 IGreeter
            var soul = binder.Bind<IGreeter>(_greeter);
            // 若要解除綁定可呼叫 binder.Unbind(soul);
        }

        void ISessionObserver.OnSessionClosed(ISessionBinder binder)
        {
            // 客戶端斷線時要做的處理
            Enable = false;
        }

        void IEntry.Update()
        {
            // 若需要伺服器主迴圈更新，可放在這裡
        }
    }
}
```
啟動伺服器主程式（沿用 HelloWorld 實作方式）：
```csharp
using System;
using System.Threading.Tasks;
using PinionCore.Remote.Server;
using Protocol;

namespace Server
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            int port = int.Parse(args[0]);

            var protocol = ProtocolCreator.Create();
            var entry = new Entry();

            var host = new PinionCore.Remote.Server.Host(entry, protocol);
            PinionCore.Remote.Soul.IService service = host;

            var (disposeServer, errorInfos) = await service.ListenAsync(
                new PinionCore.Remote.Server.Tcp.ListeningEndpoint(port, 10));

            foreach (var error in errorInfos)
            {
                Console.WriteLine($"Listener error: {error.Exception}");
                return;
            }

            Console.WriteLine("Server started.");

            while (entry.Enable)
            {
                System.Threading.Thread.Sleep(0);
                // 若有需要，也可以在這裡手動呼叫 entry.Update()
            }

            disposeServer.Dispose();
            host.Dispose();

            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }
    }
}
```
### 3. Client 專案

建立 Console 專案：
```
Sample/Client> dotnet new console
```
csproj 參考：
```xml
<ItemGroup>
<PackageReference Include="PinionCore.Remote.Client" Version="0.1.14.12" />
<PackageReference Include="PinionCore.Remote.Reactive" Version="0.1.14.13" />
<ProjectReference Include="..\Protocol\Protocol.csproj" />
</ItemGroup>
```
客戶端程式（對齊 HelloWorld Client 實作）：
```csharp
using System;
using System.Net;
using System.Threading.Tasks;
using PinionCore.Remote.Client;
using Protocol;

namespace Client
{
    internal class Program
    {
        private static bool _enable = true;

        static void Main(string[] args)
        {
            _Run(args).Wait();
        }

        private static async Task _Run(string[] args)
        {
            var ip = IPAddress.Parse(args[0]);
            var port = int.Parse(args[1]);

            var protocol = ProtocolCreator.Create();
            var proxy = new Proxy(protocol);
            var agent = proxy.Agent;

            var endpoint = new PinionCore.Remote.Client.Tcp.ConnectingEndpoint(
                new IPEndPoint(ip, port));

            // Connect() 是 AgentExtensions 中的擴充方法
            // 它會自動呼叫 Enable(stream) 並返回 IDisposable 用於清理
            // 呼叫 Dispose() 時會自動執行 Disable() 和 endpoint.Dispose()
            var connection = await agent.Connect(endpoint).ConfigureAwait(false);

            agent.QueryNotifier<IGreeter>().Supply += greeter =>
            {
                var request = new HelloRequest { Name = "you" };
                greeter.SayHello(request).OnValue += _OnReply;
            };

            // 必須持續處理封包與訊息，否則遠端事件不會被觸發
            while (_enable)
            {
                System.Threading.Thread.Sleep(0);
                agent.HandleMessages();  // 處理遠端傳來的訊息
                agent.HandlePackets();   // 處理封包編碼
            }

            connection.Dispose();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        private static void _OnReply(HelloReply reply)
        {
            Console.WriteLine($"Receive message: {reply.Message}");
            _enable = false;
        }
    }
}
```

## 核心概念詳解

### IEntry / ISessionBinder / ISoul

- IEntry：伺服器入口，負責 Session 開/關與主迴圈更新。
- ISessionBinder：在 OnSessionOpened 傳入，用來 Bind<T> / Unbind(ISoul)。
- ISoul：代表一個已綁定到 Session 的實例，之後可用於解除綁定或查詢。

介面定義：

- PinionCore.Remote/IEntry.cs
- PinionCore.Remote/ISessionObserver.cs
- PinionCore.Remote/ISessionBinder.cs
- PinionCore.Remote/ISoul.cs

PinionCore.Remote.Soul.Service 會在內部使用 SessionEngine 管理所有 Session，PinionCore.Remote.Server.Host 則包裝它方便建立服務。

### Value<T>

Value<T> 主要特性：

- 支援 OnValue 事件與 await。
- 只會設定一次值（一次性結果）。
- 使用隱含轉型接出：return new HelloReply { ... }; 會自動包成 Value<HelloReply>。

實作位置：PinionCore.Utility/PinionCore.Utility/Remote/Value.cs

### Property<T>

Property<T> 是可通知的值型狀態：

- Value 屬性改變會觸發 DirtyEvent。
- 透過 PropertyObservable 可以轉成 IObservable<T>（PinionCore.Remote.Reactive/PropertyObservable.cs）。
- 提供隱含轉型成 T，用起來像普通屬性。

實作位置：PinionCore.Remote/Property.cs

### Notifier<T> 與 Depot<T>

Depot<T>（PinionCore.Utility/Remote/Depot.cs）是一個集合 + Notifier：

- Items.Add(item)：會觸發 Supply。
- Items.Remove(item)：會觸發 Unsupply。
- Notifier<T> 則包裝 Depot<TypeObject>，支援跨型別查詢與事件訂閱。

INotifierQueryable 介面（PinionCore.Remote/INotifierQueryable.cs）允許呼叫：

INotifier<T> QueryNotifier<T>();

Ghost.User 實作了 INotifierQueryable，所以客戶端可以透過 QueryNotifier<T> 取得任何介面的 Notifier。

### 串流方法（Streamable Method）

若介面方法定義如下：
```csharp
PinionCore.Remote.IAwaitableSource<int> StreamEcho(
    byte[] buffer,
    int offset,
    int count);
```
Source Generator 會將其視為「串流方法」：

- 傳送的資料只會包含 buffer[offset..offset+count)。
- 伺服器處理後的資料會原地寫回同一段區間。
- 回傳的 IAwaitableSource<int> 表示實際處理的位元組數（長度）。

內部檢查邏輯見 PinionCore.Remote.Tools.Protocol.Sources/MethodPinionCoreRemoteStreamable.cs。

---

## 傳輸模式與 Standalone

### TCP

伺服器端：
```csharp
var host = new PinionCore.Remote.Server.Host(entry, protocol);
PinionCore.Remote.Soul.IService service = host;
var (disposeServer, errorInfos) = await service.ListenAsync(
    new PinionCore.Remote.Server.Tcp.ListeningEndpoint(port, backlog: 10));
```
客戶端：
```csharp
var proxy = new PinionCore.Remote.Client.Proxy(protocol);
using var connection = await proxy.Connect(
    new PinionCore.Remote.Client.Tcp.ConnectingEndpoint(
        new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, port)));
```
### WebSocket

伺服器端：
```csharp
var (disposeServer, errorInfos) = await service.ListenAsync(
    new PinionCore.Remote.Server.Web.ListeningEndpoint($"http://localhost:{webPort}/"));
```
客戶端：
```csharp
var proxy = new PinionCore.Remote.Client.Proxy(protocol);
using var connection = await proxy.Connect(
    new PinionCore.Remote.Client.Web.ConnectingEndpoint(
        $"ws://localhost:{webPort}/"));
```
PinionCore.Remote.Client.Web.ConnectingEndpoint 內部使用 System.Net.WebSockets.ClientWebSocket 與 PinionCore.Network.Web.Peer。

### Standalone（單機模擬）

PinionCore.Remote.Standalone.ListeningEndpoint 同時實作：

- PinionCore.Remote.Server.IListeningEndpoint
- PinionCore.Remote.Client.IConnectingEndpoint

用法（與 SampleTests 一致）：
```csharp
var protocol = ProtocolCreator.Create();
var entry = new Entry();
var host = new PinionCore.Remote.Server.Host(entry, protocol);
PinionCore.Remote.Soul.IService service = host;

var standaloneEndpoint = new PinionCore.Remote.Standalone.ListeningEndpoint();

var (disposeServer, errors) = await service.ListenAsync(standaloneEndpoint);

var proxy = new PinionCore.Remote.Client.Proxy(protocol);
using var connection = await proxy.Connect(standaloneEndpoint);

// 重要：必須持續處理封包與訊息
var running = true;
var processTask = Task.Run(async () =>
{
    while (running)
    {
        proxy.Agent.HandlePackets();
        proxy.Agent.HandleMessages();
        await Task.Delay(1);
    }
});

// 之後流程與一般 Client 相同
proxy.Agent.QueryNotifier<IGreeter>().Supply += async greeter =>
{
    var reply = await greeter.SayHello(new HelloRequest { Name = "offline" });
    Console.WriteLine(reply.Message);
    running = false;
};

await processTask;

// 清理資源
disposeServer.Dispose();
host.Dispose();

// ListeningEndpoint 會建立一對 Stream / ReverseStream，在同一個進程內模擬收送
```
---
## 進階主題

### Reactive 擴充（PinionCore.Remote.Reactive）

PinionCore.Remote.Reactive/Extensions.cs 提供以下常用擴充：

- ReturnVoid(this Action)：把 Action 包成 IObservable<Unit>
- RemoteValue(this Value<T>)：遠端回傳值轉 IObservable<T>
- PropertyChangeValue(this Property<T>)：屬性變更轉 IObservable<T>
- SupplyEvent/UnsupplyEvent(this INotifier<T>)：Notifier 事件轉 IObservable<T>

SampleTests 用 Rx 寫法串接：

1. 等待 Echo 介面供應：

    ```proxy.Agent.QueryNotifier<Echoable>().SupplyEvent()```
2. 呼叫遠端 Echo() 並用 RemoteValue() 取回：
```
    from e in ...
    from val in e.Echo().RemoteValue()
    select val;
```
這種寫法在需要組合多個連續遠端呼叫時非常適合。

### Gateway 模組

PinionCore.Remote.Gateway 提供：

- 多服務入口（Router）
- 群組化與負載平衡（LineAllocator）
- 版本共存（不同 IProtocol.VersionCode）
- 與 Chat1 範例整合的 Gateway 主控流程

詳細請參考 PinionCore.Remote.Gateway/README.md 以及 PinionCore.Consoles.Chat1.* 專案。

### 自訂連線（Custom Connection）

若內建 TCP/WebSocket 不符合需求，可自行實作：

- PinionCore.Network.IStreamable（收送 byte[]）
- PinionCore.Remote.Client.IConnectingEndpoint
- PinionCore.Remote.Server.IListeningEndpoint

用法與內建端點相同，只是底層換成你的協議或傳輸。

### 自訂序列化

若需要自訂序列化，應直接使用底層類別而非簡化包裝：

**伺服器端（使用 PinionCore.Remote.Soul.Service）**：
```csharp
var serializer = new YourSerializer();
var internalSerializer = new YourInternalSerializer();
var pool = PinionCore.Memorys.PoolProvider.Shared;

// 注意：這裡使用 Soul.Service（底層完整類別），不是 Server.Host（簡化包裝）
var service = new PinionCore.Remote.Soul.Service(entry, protocol, serializer, internalSerializer, pool);
```

**客戶端（使用 PinionCore.Remote.Ghost.Agent）**：
```csharp
var serializer = new YourSerializer();
var internalSerializer = new YourInternalSerializer();
var pool = PinionCore.Memorys.PoolProvider.Shared;

// 注意：這裡使用 Ghost.Agent（底層完整類別），不是 Client.Proxy（簡化包裝）
var agent = new PinionCore.Remote.Ghost.Agent(protocol, serializer, internalSerializer, pool);
```

**簡化包裝與完整類別的對應關係**：
- `Server.Host` 內部使用預設序列化的 `Soul.Service`
- `Client.Proxy` 內部使用預設序列化的 `Ghost.Agent`

需要序列化的型別可由 IProtocol.SerializeTypes 取得，或參考 PinionCore.Serialization/README.md。

---
## 範例與測試

建議從以下專案開始閱讀：

- PinionCore.Samples.Helloworld.Protocols：基本 Protocol 與 ProtocolCreator 實作
- PinionCore.Samples.Helloworld.Server：Entry、Greeter、Host 用法
- PinionCore.Samples.Helloworld.Client：Proxy、ConnectingEndpoint 與 QueryNotifier
- **PinionCore.Integration.Tests/SampleTests.cs**（重點推薦）：
    - **同時啟動 TCP / WebSocket / Standalone 三種端點並行測試**
    - 展示如何使用 Rx (SupplyEvent / RemoteValue) 處理遠端呼叫
    - **詳細的英文註解說明每個步驟**，包括為何需要背景處理迴圈
    - 驗證三種傳輸模式行為一致
- PinionCore.Remote.Gateway + PinionCore.Consoles.Chat1.*：Gateway 實際落地案例

---

## 結語

PinionCore Remote 的目標，是用「介面導向」的方式，把伺服器與客戶端之間的溝通，從繁瑣的封包格式與序列化細節中抽離出來。你只需要專注在 Domain 模型與狀態管理上，其餘連線、序列化、供應/退供與版本檢查等細節，都交
給框架處理。無論是遊戲、即時服務、工具後端，或是透過 Gateway 串起多個服務，只要你的需求是「在不同進程或機器之間像呼叫本地介面一樣互動」，這個框架都可以成為你的基礎。

如果你第一次接觸這個專案，建議從 PinionCore.Samples.HelloWorld.* 開始，照著「快速開始」章節建立 Protocol / Server / Client 三個專案，實際跑一次完整流程。接著再閱讀 PinionCore.Integration.Tests（特別是
SampleTests）與 Gateway 相關範例，會更清楚整體架構如何在真實場景下組合運作。當你需要更進階的能力，例如自訂傳輸層或序列化格式，可以回頭參考「進階主題」章節與對應程式碼檔案。

在使用過程中，如果你發現文件哪裡不清楚、範例有不足之處，或遇到實際需求無法覆蓋的情境，非常歡迎在 GitHub 開 Issue 討論，也歡迎提出 PR。無論是補充說明、修正文案、增加小型範例或整合測試，只要能讓下一個使用者更
容易上手，都是非常有價值的貢獻。希望 PinionCore Remote 能在你的專案裡，替你省下處理網路細節的時間，讓你把心力放在真正重要的遊戲與應用程式設計上。
