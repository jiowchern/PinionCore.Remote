# 核心概念詳解

[上一節：快速開始](quick-start.md) | [下一節：傳輸模式](transports.md)

## Spirit / Soul / Ghost

框架的術語構成三位一體：

- **Spirit**——通訊介面：伺服器與客戶端共用的純 C# 介面，定義方法（`Value<T>`）、屬性（`Property<T>`）、事件、Notifier 與 `Spirit<T>`。
- **Soul**——Spirit 在伺服器端的化身：透過 `Bind<T>` 綁定到 Session 的實作（見 `PinionCore.Remote.Soul`）。
- **Ghost**——Spirit 在客戶端的化身：透過 `QueryNotifier<T>` 取得的即時代理（見 `PinionCore.Remote.Ghost`）。

伺服器綁定 Soul，客戶端取得 Ghost，兩端面對的都是同一個 Spirit。

---

## IEntry / ISessionBinder / ISoul

- **`IEntry`**：伺服器入口，負責 Session 開/關與更新。
- **`ISessionBinder`**：在 `OnSessionOpened` 傳入，用來 `Bind<T>` / `Unbind(ISoul)`。
- **`ISoul`**：代表一個已綁定到 Session 的實例，可用於之後解除綁定或查詢。

相關檔案：

- `PinionCore.Remote/IEntry.cs`
- `PinionCore.Remote/ISessionObserver.cs`
- `PinionCore.Remote/ISessionBinder.cs`
- `PinionCore.Remote/ISoul.cs`

`PinionCore.Remote.Soul.Service` 使用 `SessionEngine` 管理所有 Session，`PinionCore.Remote.Server.Host` 則包裝它以便建立服務。

---

## Value<T>

特性：

- 支援 `OnValue` 事件與 `await`。
- 只會設定一次值（一次性結果）。
- 支援隱含轉型：`return new HelloReply { ... };` 會自動包成 `Value<HelloReply>`。

實作位置：`PinionCore.Utility/Remote/Value.cs`

---

## Property<T>

可通知的狀態值：

- 設定 `Value` 時會觸發 DirtyEvent。
- 可透過 `PropertyObservable` 轉成 `IObservable<T>`（在 `PinionCore.Remote.Reactive/PropertyObservable.cs`）。
- 提供隱含轉型成 `T`，使用起來像一般屬性。

實作位置：`PinionCore.Remote/Property.cs`

---

## Notifier<T> 與 Depot<T>

`Depot<T>`（`PinionCore.Utility/Remote/Depot.cs`）是集合＋通知結合：

- `Items.Add(item)` → 觸發 `Supply`
- `Items.Remove(item)` → 觸發 `Unsupply`

`INotifier<out T>` 是 covariant：`Depot<World>` 可隱含轉型為 `INotifier<IWorld>`
（`World : IWorld`，繼承約束在編譯期檢查）。搭配 `ToNotifier<T>()` 擴充方法，
一個 Depot 可依實作的介面產生多個 `Notifier<T>`（建立一次並存欄位，不要在 property getter 內呼叫）：

```csharp
var depot = new PinionCore.Remote.Depot<World>();
var worldNotifier = depot.ToNotifier<IWorld>();   // Notifier<IWorld>
var viewNotifier = depot.ToNotifier<IView>();     // Notifier<IView>
```

`Notifier<T>` 包裝 `Depot<TypeObject>`，支援跨型別查詢與事件訂閱。

`INotifierQueryable` 介面（`PinionCore.Remote/INotifierQueryable.cs`）可呼叫：

```csharp
INotifier<T> QueryNotifier<T>();
```

`Ghost.User` 實作了 `INotifierQueryable`，所以客戶端可以透過 `QueryNotifier<T>` 取得任何介面的 Notifier。

---

## Spirit<T>

> 命名說明：**Spirit** 一詞在術語上指「通訊介面」本身；`Spirit<T>` 類別則是承載單一介面實體的容器——方法回傳 `Spirit<T>` 時，傳遞的正是一個 Spirit（介面）的化身。

由方法回傳、單次供給的遠端物件容器：

- 伺服器端以 `new Spirit<T>(instance)` 包裝實作回傳（`T` 必須是介面）。
- 客戶端收到的 `Spirit<T>` 透過 `Supply` 事件供給遠端代理（Ghost）。
- 伺服器端 `Dispose()` 後客戶端觸發 `Unsupply`，且此 Spirit 永不再供給。
- `Supply` / `Unsupply` 具補發語意：晚訂閱也能收到已發生的事件。
- 搭配 `PinionCore.Remote.Reactive` 的 `SupplyEvent()` / `UnsupplyEvent()` 可轉成 `IObservable<T>`。

```csharp
public interface ILobby
{
    PinionCore.Remote.Spirit<IRoom> EnterRoom(int roomId);
}
```

實作位置：`PinionCore.Utility/Remote/Spirit.cs`
測試範例：`PinionCore.Remote.Tools.Protocol.Sources.TestCommon.Tests/SpiritTests.cs`

---

## 串流方法（Streamable Method）

若介面方法定義如下：

```csharp
PinionCore.Remote.IAwaitableSource<int> StreamEcho(
    byte[] buffer,
    int offset,
    int count,
    System.Threading.CancellationToken token);
```

Source Generator 會將其視為「串流方法」。簽名必須完全符合——四個參數且最後一個是
`CancellationToken`——否則會被當成一般 RMI 處理：

- 傳送資料只包含 `buffer[offset..offset+count)`。
- 伺服器處理後的資料會原地寫回同一段區間。
- 回傳的 `IAwaitableSource<int>` 表示實際處理的位元組數。
- 取消 `token` 會中止進行中的串流作業。

檢查邏輯見：`PinionCore.Remote.Tools.Protocol.Sources/MethodPinionCoreRemoteStreamable.cs`
