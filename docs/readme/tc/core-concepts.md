# 核心概念詳解

[上一節：快速開始](quick-start.md) | [下一節：傳輸模式](transports.md)

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

`Notifier<T>` 包裝 `Depot<TypeObject>`，支援跨型別查詢與事件訂閱。

`INotifierQueryable` 介面（`PinionCore.Remote/INotifierQueryable.cs`）可呼叫：

```csharp
INotifier<T> QueryNotifier<T>();
```

`Ghost.User` 實作了 `INotifierQueryable`，所以客戶端可以透過 `QueryNotifier<T>` 取得任何介面的 Notifier。

---

## 串流方法（Streamable Method）

若介面方法定義如下：

```csharp
PinionCore.Remote.IAwaitableSource<int> StreamEcho(
    byte[] buffer,
    int offset,
    int count);
```

Source Generator 會將其視為「串流方法」：

- 傳送資料只包含 `buffer[offset..offset+count)`。
- 伺服器處理後的資料會原地寫回同一段區間。
- 回傳的 `IAwaitableSource<int>` 表示實際處理的位元組數。

檢查邏輯見：`PinionCore.Remote.Tools.Protocol.Sources/MethodPinionCoreRemoteStreamable.cs`
