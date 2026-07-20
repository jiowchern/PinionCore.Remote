# PinionCore.Remote.Standalone

單機（同進程）模式，提供兩種伺服器-客戶端模擬方式，適合開發階段除錯與測試。

## 兩種模式

### 1. ListeningEndpoint — 完整管線模擬

`ListeningEndpoint` 同時實作 `PinionCore.Remote.Server.IListeningEndpoint` 與 `PinionCore.Remote.Client.IConnectingEndpoint`，以記憶體 Stream 對接取代 Socket，**保留完整的序列化管線**（IProtocol、Serializer、封包、Ghost 代理都照常執行）。

```csharp
var protocol = ProtocolCreator.Create();
var entry = new Entry();
var host = new PinionCore.Remote.Server.Host(entry, protocol);
PinionCore.Remote.Soul.IService service = host;

var endpoint = new PinionCore.Remote.Standalone.ListeningEndpoint();
var (disposeServer, errors) = await service.ListenAsync(endpoint);

var agent = new PinionCore.Remote.Ghost.Agent(protocol);
PinionCore.Remote.Client.IConnectingEndpoint connectable = endpoint;
var stream = await connectable.ConnectAsync();
agent.Enable(stream);
```

用途：整合測試、驗證 Spirit 介面確實可遠端化（可序列化性、協議一致性），行為與 TCP/WebSocket 完全一致。

### 2. DirectStandalone — 直通模式（零序列化）

`DirectStandalone` 同時實作客戶端 `Ghost.IAgent` 與伺服器端 `ISessionBinder`，將 `Bind` 進來的 Soul 實例**不經序列化**直接供給到 `QueryNotifier<T>`。客戶端取得的物件就是伺服器端實例本身（共用參考）。

```csharp
// 不需要 IProtocol 與程式碼產生
var entry = new Entry();
var direct = new PinionCore.Remote.Standalone.DirectStandalone(entry);
PinionCore.Remote.Ghost.IAgent agent = direct;

agent.QueryNotifier<IGreeter>().Supply += greeter =>
{
    // greeter 就是伺服器端實例（同一參考），方法呼叫是直接的 .NET 呼叫
};

direct.Launch();            // 觸發 entry.OnSessionOpened(binder)

// 主迴圈
agent.HandlePackets();      // 驅動 IEntry.Update()
agent.HandleMessages();     // 觸發佇列中的 Supply/Unsupply

direct.Shutdown();          // 觸發 OnSessionClosed 並立即撤銷所有 Soul
```

用途：單元測試、Unity Editor 快速迭代、邏輯除錯——不需重新建置 Protocol 即可測試 Spirit 互動。

## 模式選擇

| 項目 | ListeningEndpoint | DirectStandalone |
|------|-------------------|------------------|
| 序列化管線 | 完整執行 | 完全繞過 |
| IProtocol / 程式碼產生 | 必要 | 不需要 |
| 物件語意 | 序列化複本 | 共用參考 |
| 每次呼叫開銷 | 序列化 + 封包 | 趨近於零 |
| 可序列化性驗證 | 會驗證 | 不驗證 |
| 適用 | 整合測試、上線前驗證 | 單元測試、快速迭代 |

## DirectStandalone 設計要點

- **時序一致性**：`Bind`/`Unbind` 造成的 `Supply`/`Unsupply` 排入佇列，於 `HandleMessages()` 時依序觸發，與網路模式的訊息時序一致；晚訂閱有補發語意（比照 `Depot<T>`）。
- **Shutdown 同步語意**：比照網路模式 `Disable`——立即處理佇列並撤銷所有仍綁定的 Soul，不需再 pump。
- **`Return` 語意**：比照遠端模式，`Return` 的物件不進 `QueryNotifier`（僅保留可 `Unbind` 的追蹤）；直通模式下方法回傳值本來就直接共享。
- **`IEntry.Update` 驅動**：由 `HandlePackets()` 代驅（對應網路模式由服務執行緒驅動），使用端不需另外驅動。
- **IAgent 相容**：`Enable(stream)` 映射到 `Launch()`（stream 參數無作用，可傳 null）、`Disable()` 映射到 `Shutdown()`；`Ping` 恆為 0，版本／方法錯誤事件永不觸發。

## 注意事項

DirectStandalone **不驗證可序列化性**——在此能運作的 Spirit 介面不代表可遠端化（傳遞不可序列化的型別、依賴共用參考的寫法都不會被攔截，切到網路模式才會失敗）。上線前仍應以 ListeningEndpoint 或 TCP 模式進行整合測試。

更多說明見 [docs/readme/tc/transports.md](../docs/readme/tc/transports.md)。
