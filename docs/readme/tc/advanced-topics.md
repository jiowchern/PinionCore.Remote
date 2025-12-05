# 進階主題

[上一節：傳輸模式](transports.md) | [下一節：範例與結語](samples-and-tests.md)

## Reactive 擴充（PinionCore.Remote.Reactive）

`PinionCore.Remote.Reactive/Extensions.cs` 提供常用擴充：

- `ReturnVoid(this Action)`：`Action → IObservable<Unit>`
- `RemoteValue(this Value<T>)`：遠端回傳值轉 `IObservable<T>`
- `PropertyChangeValue(this Property<T>)`：屬性變更轉 `IObservable<T>`
- `SupplyEvent/UnsupplyEvent(this INotifier<T>)`：Notifier 事件轉 `IObservable<T>`

搭配 LINQ-to-Rx，可以很自然地組成遠端流程。

---

## Gateway 模組

`PinionCore.Remote.Gateway` 提供：

- 多服務入口（Router）
- 群組化與負載平衡（LineAllocator）
- 版本共存（不同 `IProtocol.VersionCode`）
- 與 `PinionCore.Consoles.Chat1.*` 整合的 Gateway 案例

詳細可參考 `PinionCore.Remote.Gateway/README.md` 與 Chat 範例程式。

---

## 自訂連線（Custom Connection）

若內建 TCP / WebSocket 不符合需求，可以自訂：

- `PinionCore.Network.IStreamable`（收送 byte[]）
- `PinionCore.Remote.Client.IConnectingEndpoint`
- `PinionCore.Remote.Server.IListeningEndpoint`

用法與內建端點相同，只是底層換成你自己的協議或傳輸。

---

## 自訂序列化

需要自訂序列化時，建議直接使用底層類別（而不是簡化包裝）：

伺服器端（使用 `Soul.Service`）：

```csharp
var serializer = new YourSerializer();
var internalSerializer = new YourInternalSerializer();
var pool = PinionCore.Memorys.PoolProvider.Shared;

var service = new PinionCore.Remote.Soul.Service(
    entry, protocol, serializer, internalSerializer, pool);
```

客戶端（使用 `Ghost.Agent`）：

```csharp
var serializer = new YourSerializer();
var internalSerializer = new YourInternalSerializer();
var pool = PinionCore.Memorys.PoolProvider.Shared;

var agent = new PinionCore.Remote.Ghost.Agent(
    protocol, serializer, internalSerializer, pool);
```

對應關係：

- `Server.Host`：封裝預設序列化的 `Soul.Service`
- `Client.Proxy`：封裝預設序列化的 `Ghost.Agent`

需要序列化的型別可由 `IProtocol.SerializeTypes` 取得，或參考 `PinionCore.Serialization/README.md`。
